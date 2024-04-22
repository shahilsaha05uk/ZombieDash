using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AdvancedSceneManager.Core.Actions;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using UnityEngine;
using static AdvancedSceneManager.SceneManager;
using scene = UnityEngine.SceneManagement.Scene;
using Scene = AdvancedSceneManager.Models.Scene;
using sceneManager = UnityEngine.SceneManagement.SceneManager;

namespace AdvancedSceneManager.Core
{

    /// <summary>Base class for <see cref="collection"/> and <see cref="standalone"/> classes. Contains shared functionality for scene management.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class SceneManagerBase : ScriptableObject, ISerializationCallbackReceiver
    {

        /// <summary>Occurs when a scene is opened in this scene manager.</summary>
        public event Action<OpenSceneInfo> sceneOpened;

        /// <summary>Occurs when a scene is closed in this scene manager.</summary>
        public event Action<OpenSceneInfo> sceneClosed;

        internal void RaiseSceneOpened(OpenSceneInfo scene) =>
            ActionUtility.Try(() => sceneOpened?.Invoke(scene));

        internal void RaiseSceneClosed(OpenSceneInfo scene) =>
            ActionUtility.Try(() => sceneClosed?.Invoke(scene));

        #region Open scene list

        /// <summary>Gets or creates <see cref="OpenSceneInfo"/> for an open scene.</summary>
        /// <param name="scene">The scene to get <see cref="OpenSceneInfo"/> for.</param>
        /// <param name="lazyCreate">Sets whatever ASM should create <see cref="OpenSceneInfo"/> if it does not exist (which it won't during Start(), Awake() and OnEnable() for example).</param>
        /// <remarks>Returns <see langword="null"/> if scene is not loaded, or had to be created, but <paramref name="lazyCreate"/> was <see langword="false"/>.</remarks>
        public OpenSceneInfo GetTrackedScene(scene? scene, bool lazyCreate = true)
        {

            if (!scene?.IsValid() ?? false)
                return null;

            var trackedScene = m_scenes.FirstOrDefault(s => s.unityScene == scene);
            if (trackedScene is null && lazyCreate && scene.HasValue)
            {

                var asmScene = SceneManager.assets.allScenes.Find(scene.Value.path);
                trackedScene = new OpenSceneInfo(asmScene, scene.Value, this);
                m_scenes.Add(trackedScene);

                if (asmScene)
                    asmScene.OnPropertyChanged();

            }

            return trackedScene;

        }

        /// <inheritdoc cref="GetTrackedScene(scene?, bool)"/>
        public OpenSceneInfo GetTrackedScene(Scene scene, bool lazyCreate = true) =>
            GetTrackedScene(sceneManager.GetSceneByPath(scene ? scene.path : string.Empty), lazyCreate);

        internal void Remove(OpenSceneInfo scene)
        {
            if (m_scenes.Remove(scene) && scene.scene)
                scene.scene.OnPropertyChanged();
        }

        internal void Clear() =>
            m_scenes.Clear();

        #region ISerializationCallbackReceiver

        [Serializable]
        internal class LiteScene
        {

            public LiteScene(scene scene, SceneCloseBehavior persistentOption)
            {
                this.scene = scene;
                this.persistentOption = persistentOption;
            }

            public scene scene;
            public SceneCloseBehavior persistentOption;

        }

        [SerializeField] private LiteScene[] _scenes = Array.Empty<LiteScene>();

        public void OnBeforeSerialize()
        {
            _scenes = m_scenes.Where(s => s.unityScene.HasValue).Select(s => new LiteScene(s.unityScene.Value, PersistentUtility.GetPersistentOption(s.unityScene.Value))).ToArray();
        }

        public void OnAfterDeserialize()
        { }

        public virtual void OnAfterDeserialize2() //Called from SceneManager.cs, since we are not allowed to call some apis in serialization methods
        {

            m_scenes.Clear();

            foreach (var scene in _scenes)
            {
                m_scenes.Add(new OpenSceneInfo(Scene.Find(scene.scene.path), scene.scene, this));
                PersistentUtility.Set(scene.scene, scene.persistentOption);
            }

        }

        #endregion

        List<OpenSceneInfo> m_scenes { get; } = new List<OpenSceneInfo>();

        /// <summary>The open scenes in this scene manager.</summary>
        public IEnumerable<OpenSceneInfo> openScenes => m_scenes;

        /// <summary>Finds last open instance of the specified scene.</summary>
        public OpenSceneInfo Find(Scene scene) =>
            openScenes.LastOrDefault(s => s.scene == scene);

        /// <summary>Finds the open instance of the specified scene.</summary>
        public OpenSceneInfo Find(scene scene) =>
            openScenes.Find(scene);

        /// <summary>Gets the last opened scene.</summary>
        public OpenSceneInfo GetLastScene() =>
            m_scenes.LastOrDefault();

        /// <summary>Gets if the scene is open.</summary>
        public bool IsOpen(Scene scene) =>
            Find(scene)?.isOpen ?? false;

        /// <summary>Gets if the scene is open.</summary>
        public bool IsOpen(scene scene) =>
            Find(scene)?.isOpen ?? false;

        #endregion
        #region Open, Activate

        /// <summary>Open the scene.</summary>
        public virtual SceneOperation<OpenSceneInfo> Open(Scene scene) =>
            SceneOperation.Add(this, @return: o => GetTrackedScene(o.FindLastAction<SceneLoadAction>()?.unityScene)).
            Open(scene);

        /// <summary>Opens the scenes.</summary>
        public virtual SceneOperation<OpenSceneInfo[]> OpenMultiple(params Scene[] scenes) =>
            SceneOperation.Add(this, @return: o => o.FindActions<SceneLoadAction>().Select(action => GetTrackedScene(action.unityScene)).OfType<OpenSceneInfo>().ToArray()).
            Open(scenes);

        /// <summary>Open the scene.</summary>
        public virtual SceneOperation OpenWithoutReturnValue(Scene scene) =>
            SceneOperation.Add(this).
            Open(scene);

        /// <summary>Reopens the scene.</summary>
        public SceneOperation<OpenSceneInfo> Reopen(OpenSceneInfo scene) =>
            SceneOperation.Add(this, @return: o => o.FindLastAction<SceneLoadAction>()?.GetTrackedScene()).
            Reopen(scene);

        #endregion
        #region Close

        /// <summary>Close the scene.</summary>
        public virtual SceneOperation Close(OpenSceneInfo scene)
        {

            if (!scene?.unityScene.HasValue ?? false)
                return SceneOperation.done;

            return SceneOperation.Add(this).
                Close(force: true, scene);

        }

        /// <summary>Close the scenes.</summary>
        public virtual SceneOperation CloseMultiple(params OpenSceneInfo[] scenes)
        {

            scenes = scenes.Where(s => s?.unityScene.HasValue ?? false).ToArray();

            if (!scenes.Any())
                return SceneOperation.done;

            return SceneOperation.Add(this).
                Close(scenes, force: true);

        }

        /// <summary>Close all open scenes in the list.</summary>
        internal virtual SceneOperation CloseAll()
        {

            if (!openScenes.Any())
                return SceneOperation.done;

            return SceneOperation.Add(this).
                Close(openScenes, force: true);

        }

        #endregion
        #region Toggle

        /// <summary>
        /// <para>Gets if this scene manager can open the specified scene.</para>
        /// <para><see cref="standalone"/> always returns true.</para>
        /// </summary>
        public virtual bool CanOpen(Scene scene) => true;

        /// <summary>Toggles the scene open or closed.</summary>
        /// <param name="enabled">If null, the scene will be toggled on or off depending on whatever the scene is open or not. Pass a value to ensure that the scene is either open or closed.</param>
        public SceneOperation Toggle(Scene scene, bool? enabled = null)
        {

            if (!CanOpen(scene) || !scene)
                return SceneOperation.done;

            var openSceneInfo = scene.GetOpenSceneInfo();
            var isOpen = openSceneInfo.isOpen;
            var isEnabled = enabled.GetValueOrDefault();

            if (enabled.HasValue)
            {
                if (isEnabled && !isOpen)
                    return OpenWithoutReturnValue(scene);
                else if (!isEnabled && isOpen)
                    return Close(openSceneInfo);
            }
            else
            {
                if (!isOpen)
                    return OpenWithoutReturnValue(scene);
                else if (isOpen)
                    return Close(openSceneInfo);
            }

            return SceneOperation.done;

        }

        /// <summary>Ensures that the scene is open.</summary>
        public SceneOperation EnsureOpen(Scene scene) =>
            Toggle(scene, true);

        #endregion
        #region Reinitialize

        internal void Reinitialize()
        {

            foreach (var scene in openScenes.Where(s => !s.scene).ToArray())
                Remove(scene);

            foreach (var scene in openScenes.Where(s => !s.isOpen).ToArray())
            {

                if (!scene?.scene || !(scene.unityScene = sceneManager.GetSceneByPath(scene.scene.path)).HasValue)
                    Remove(scene);

            }

        }

        #endregion

    }

}

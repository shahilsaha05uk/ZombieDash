using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Core.Actions;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using Lazy.Utility;
using UnityEditor;
using UnityEngine;
using static AdvancedSceneManager.SceneManager;
using Object = UnityEngine.Object;
using scene = UnityEngine.SceneManagement.Scene;
using Scene = AdvancedSceneManager.Models.Scene;
using sceneManager = UnityEngine.SceneManagement.SceneManager;

namespace AdvancedSceneManager.Core
{

    [AddComponentMenu("")]
    /// <summary>Helper script hosted in DontDestroyOnLoad.</summary>
    internal class UtilityManager : MonoBehaviour
    { }

    /// <summary>An utility scene manager that helps with actions that might relate to either <see cref="collection"/> or <see cref="standalone"/> managers.</summary>
    /// <remarks>Usage: <see cref="utility"/>.</remarks>
    public class UtilitySceneManager
    {

        /// <summary>Occurs when the last user scene closes.</summary>
        /// <remarks> 
        /// <para>This usually happens by mistake, and likely means that no user code would run, this is your chance to restore to a known state (return to main menu, for example), or crash to desktop.</para>
        /// <para>Returning to main menu can easily be done as such:<code>SceneManager.runtime.Restart()</code></para>
        /// </remarks>
        public Action onAllScenesClosed;

        /// <summary>Gets all currently open scenes.</summary>
        public IEnumerable<OpenSceneInfo> openScenes =>
            collection.openScenes.Concat(standalone.openScenes);

        static bool isSetup;

        internal static void Initialize()
        {

            if (isSetup)
                return;
            isSetup = true;

            RegisterCallbackHandlers();
            SetupQueue();

#if UNITY_EDITOR

            EditorApplication.playModeStateChanged += (state) =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode)
                {
                    SceneManager.utility.Reinitialize();
                    if (!runtime.wasStartedAsBuild && standalone.openScenes != null)
                        foreach (var scene in standalone.openScenes)
                            _ = CallbackUtility.DoSceneOpenCallbacks(scene).StartCoroutine();
                }
            };
#endif

        }

        internal void Reinitialize()
        {

            //This code is redundant when first starting, but when scripts recompile in playmode and execution continues,
            //they disappear, this fixes that (and there seem to be no issues first time either)
            if (!runtime.wasStartedAsBuild)
                foreach (var scene in SceneUtility.GetAllOpenUnityScenes())
                    if (scene.IsValid() && !DefaultSceneUtility.IsDefaultScene(scene))
                        _ = standalone.GetTrackedScene(scene);

        }

        /// <summary>Performs a callback on the scripts on all open scenes.</summary>
        public IEnumerator DoSceneCallback<T>(CallbackUtility.FluentInvokeAPI<T>.Callback action) =>
            CallbackUtility.Invoke<T>().WithCallback(action).OnAllOpenScenes();

        #region Queue

        /// <summary>Occurs when scene operation queue is empty, this means that ASM is not doing any scene operations.</summary>
        public event Action queueEmpty;

        /// <summary>Gets whatever ASM is busy with any scene operations.</summary>
        public bool isBusy => QueueUtility<SceneOperation>.isBusy;

        /// <summary>The currently running scene operations.</summary>
        public IEnumerable<SceneOperation> runningOperations =>
            QueueUtility<SceneOperation>.running;

        /// <summary>Gets the current scene operation queue.</summary>
        public IEnumerable<SceneOperation> queuedOperations =>
            QueueUtility<SceneOperation>.queue;

        /// <summary>Gets the current active operation in the queue.</summary>
        public SceneOperation currentOperation =>
            QueueUtility<SceneOperation>.queue.FirstOrDefault();

        static void SetupQueue()
        {

            QueueUtility<SceneOperation>.queueEmpty += () =>
            {
                //Move scenes that remained open when the parent collection closed to standalone
                CarryOverPersistentCollectionScenes();
                utility.queueEmpty?.Invoke();
            };

        }

        /// <summary>Move persistent scenes that remained in <see cref="collection"/> to <see cref="standalone"/>.</summary>
        static void CarryOverPersistentCollectionScenes()
        {
            var scenes = collection.openScenes.Where(s => !collection || !collection.current.scenes.Contains(s.scene)).ToArray();
            foreach (var scene in scenes)
            {
                collection.Remove(scene);
                if (scene.unityScene?.isLoaded ?? false)
                    _ = standalone.GetTrackedScene(scene.unityScene.Value);
            }
        }

        #endregion
        #region Scene Open / Close callbacks

        public delegate void ActiveSceneChangedHandler(OpenSceneInfo previousScene, OpenSceneInfo activeScene);

        /// <summary>Occurs when the active scene changes.</summary>
        public event ActiveSceneChangedHandler ActiveSceneChanged;

        /// <summary>Occurs when a scene is opened.</summary>
        public event Action<OpenSceneInfo, SceneManagerBase> sceneOpened;

        /// <summary>Occurs when a scene is closed.</summary>
        public event Action<OpenSceneInfo, SceneManagerBase> sceneClosed;

        /// <summary>Occurs when a loading screen is about to be opened.</summary>
        public event Action<LoadingScreen> LoadingScreenOpening;

        /// <summary>Occurs when a loading screen has opened.</summary>
        public event Action<LoadingScreen> LoadingScreenOpened;

        /// <summary>Occurs when a loading screen is about to close.</summary>
        public event Action<LoadingScreen> LoadingScreenClosing;

        /// <summary>Occurs when a loading screen has closed.</summary>
        public event Action<LoadingScreen> LoadingScreenClosed;

        internal void RaiseLoadingScreenOpening(LoadingScreen loadingScreen) =>
            LoadingScreenOpening?.Invoke(loadingScreen);

        internal void RaiseLoadingScreenOpened(LoadingScreen loadingScreen) =>
            LoadingScreenOpened?.Invoke(loadingScreen);

        internal void RaiseLoadingScreenClosing(LoadingScreen loadingScreen) =>
            LoadingScreenClosing?.Invoke(loadingScreen);

        internal void RaiseLoadingScreenClosed(LoadingScreen loadingScreen) =>
            LoadingScreenClosed?.Invoke(loadingScreen);

        static readonly Dictionary<IASMObject, List<(Action action, bool persistent)>> sceneOpenCallbacks = new Dictionary<IASMObject, List<(Action action, bool persistent)>>();
        static readonly Dictionary<IASMObject, List<(Action action, bool persistent)>> sceneCloseCallbacks = new Dictionary<IASMObject, List<(Action action, bool persistent)>>();

        /// <summary>Registers a callback for when a scene or collection has opened, or closed, the callback is removed once it has been called, unless persistent is true.</summary>
        public void RegisterCallback<T>(T scene, Action onOpen = null, Action onClose = null, bool persistent = false) where T : Object, IASMObject
        {
            if (scene)
            {
                if (onOpen != null)
                {
                    UnregisterCallback(scene, onOpen: onOpen);
                    sceneOpenCallbacks.Add(scene, (onOpen, persistent));
                }
                if (onClose != null)
                {
                    UnregisterCallback(scene, onClose: onClose);
                    sceneCloseCallbacks.Add(scene, (onClose, persistent));
                }
            }
        }

        /// <summary>Unregisters a callback.</summary>
        public void UnregisterCallback<T>(T scene, Action onOpen = null, Action onClose = null) where T : Object, IASMObject
        {
            if (scene)
            {
                _ = sceneOpenCallbacks.GetValue(scene)?.RemoveAll(o => o.action == onOpen);
                _ = sceneCloseCallbacks.GetValue(scene)?.RemoveAll(o => o.action == onClose);
            }
        }

        /// <summary>Register handlers for scene open / close callbacks.</summary>
        static void RegisterCallbackHandlers()
        {

            standalone.sceneOpened += s => OnSceneOpened(s, standalone);
            collection.sceneOpened += s => OnSceneOpened(s, collection);

            standalone.sceneClosed += s => OnSceneClosed(s, standalone);
            collection.sceneClosed += s => OnSceneClosed(s, collection);

            collection.opened += c => OnSceneOpened(c, collection);
            collection.closed += c => OnSceneOpened(c, collection);

            sceneManager.activeSceneChanged += SceneManager_activeSceneChanged;

            void OnSceneOpened(object scene, SceneManagerBase sceneManager)
            {

                IASMObject obj = null;
                if (scene is OpenSceneInfo info)
                    obj = info.scene;
                else if (scene is SceneCollection collection)
                    obj = collection;
                else
                    return;

                if (sceneOpenCallbacks.TryGetValue(obj, out var list))
                    foreach (var action in list.ToArray())
                    {
                        action.action?.Invoke();
                        if (!action.persistent)
                            _ = sceneOpenCallbacks.GetValue(obj).Remove(action);
                    }

                if (scene is OpenSceneInfo s)
                    utility.sceneOpened?.Invoke(s, sceneManager);

            }

            void OnSceneClosed(object scene, SceneManagerBase sceneManager)
            {

                if (scene == null)
                    return;

                IASMObject obj = null;
                if (scene is OpenSceneInfo info)
                    obj = info.scene;
                else if (scene is SceneCollection collection)
                    obj = collection;

                if (sceneCloseCallbacks.TryGetValue(obj, out var list))
                    foreach (var action in list.ToArray())
                    {
                        action.action?.Invoke();
                        if (!action.persistent)
                            _ = sceneCloseCallbacks.GetValue(obj).Remove(action);
                    }

                if (scene is OpenSceneInfo s)
                    utility.sceneClosed?.Invoke(s, sceneManager);

                if (SceneUtility.GetAllOpenUnityScenes().Count() == 1 && DefaultSceneUtility.isOpen)
                    SceneManager.utility.onAllScenesClosed?.Invoke();

            }

            void SceneManager_activeSceneChanged(scene oldScene, scene newScene)
            {
                DefaultSceneUtility.OnBeforeActiveSceneChanged(oldScene, newScene, out var cancel);
                if (!cancel)
                    utility.ActiveSceneChanged?.Invoke(oldScene.Scene(), newScene.Scene());
            }

        }

        #endregion
        #region Reopen

        /// <summary>Reopen a scene regardless of whatever it is associated with a collection, or is was opened as stand-alone.</summary>
        public SceneOperation<OpenSceneInfo> Reopen(OpenSceneInfo scene)
        {
            if (scene == null)
                return SceneOperation<OpenSceneInfo>.done;
            if (standalone.IsOpen(scene.scene))
                return standalone.Reopen(scene);
            if (collection.IsOpen(scene.scene))
                return collection.Reopen(scene);
            return SceneOperation<OpenSceneInfo>.done;
        }

        /// <summary>Opens the scene if not open, otherwise it will be reopened.</summary>
        public SceneOperation<OpenSceneInfo> OpenOrReopen(Scene scene, SceneCollection collection = null)
        {

            if (!scene)
                return SceneOperation<OpenSceneInfo>.done;

            return
                scene.isOpen
                ? scene.Reopen().WithCollection(collection)
                : scene.Open().WithCollection(collection);

        }

        #endregion
        #region Close

        /// <summary>Closes a scene regardless of whatever it is associated with a collection, or is was opened as stand-alone.</summary>
        public SceneOperation Close(OpenSceneInfo scene)
        {
            if (scene == null)
                return SceneOperation.done;
            if (standalone.IsOpen(scene.scene))
                return standalone.Close(scene);
            if (collection.IsOpen(scene.scene))
                return collection.Close(scene);
            return SceneOperation.done;
        }

        /// <summary>Closes all scenes.</summary>
        public SceneOperation CloseAll(bool closeLoadingScreens = false, bool closePersistent = false)
        {

            var scenes = openScenes.OfType<OpenSceneInfo>().
                Where(s => closeLoadingScreens || !LoadingScreenUtility.IsLoadingScreenOpen(s)).
                Where(s => closePersistent || PersistentUtility.GetPersistentOption(s.unityScene.Value) == SceneCloseBehavior.Close).
                ToArray();

            return SceneOperation.Add(collection.current ? (SceneManagerBase)collection : standalone).
                Close(force: true, scenes).
                WithCallback(Callback.BeforeLoadingScreenClose().Do(() => collection.SetNull()));
        }

        /// <summary>Closes all scenes.</summary>
        public SceneOperation CloseAll(Func<OpenSceneInfo, bool> predicate)
        {

            var scenes = openScenes.OfType<OpenSceneInfo>().
                Where(predicate).
                ToArray();

            return SceneOperation.Add(collection.current ? (SceneManagerBase)collection : standalone).
                Close(force: true, scenes).
                WithCallback(Callback.BeforeLoadingScreenClose().Do(() => collection.SetNull()));

        }

        #endregion
        #region Toggle

        /// <summary>Toggles the scene open or closed, if the scene is part of the current collection, then the scene will be toggled within the collection, otherwise, it will be toggled as a stand-alone scene.</summary>
        /// <param name="enabled">If null, the scene will be toggled on or off depending on whatever the scene is open or not. Pass a value to ensure that the scene either open or closed.</param>
        public SceneOperation Toggle(Scene scene, bool? enabled = null)
        {
            if (collection.current && collection.current.scenes.Any(s => s.path == scene.path))
                return collection.Toggle(scene, enabled);
            else
                return standalone.Toggle(scene, enabled);
        }

        /// <summary>Toggles the scene open or closed, if the scene is part of the current collection, then the scene will be toggled within the collection, otherwise, it will be toggled as a stand-alone scene.</summary>
        /// <param name="enabled">If null, the scene will be toggled on or off depending on whatever the scene is open or not. Pass a value to ensure that the scene either open or closed.</param>
        public SceneOperation Toggle(scene scene, bool? enabled = null)
        {

            if (!scene.IsValid())
                return SceneOperation.done;

            if (!(Scene.Find(scene.path) is Scene scene1))
                return SceneOperation.done;

            if (collection.current && collection.current.scenes.Any(s => s.path == scene.path))
                return collection.Toggle(scene1, enabled);
            else
                return standalone.Toggle(scene1, enabled);

        }

        #endregion
        #region IsOpen / FindOpenScene

        /// <inheritdoc cref="GetState(Scene, scene?, OpenSceneInfo)"/>
        public SceneState GetState(scene scene) => GetState(uScene: scene);

        /// <inheritdoc cref="GetState(Scene, scene?, OpenSceneInfo)"/>
        public SceneState GetState(OpenSceneInfo scene) => GetState(osi: scene);

        /// <inheritdoc cref="GetState(Scene, scene?, OpenSceneInfo)"/>
        public SceneState GetState(Scene scene) => GetState(scene: scene, null, null);

        /// <summary>Gets the current state of the scene.</summary>
        SceneState GetState(Scene scene = null, scene? uScene = default, OpenSceneInfo osi = null)
        {

            if (!scene) scene = GetScene(uScene, osi);
            if (!uScene.HasValue) uScene = GetScene(scene, osi);
            if (osi == null) osi = GetScene(scene, uScene);

            if (!scene && !uScene.HasValue && osi is null)
                return SceneState.Unknown;

            var isPreloaded = (standalone.preloadedScene?.isStillPreloaded ?? false) && standalone.preloadedScene.scene == osi;
            var isOpen = osi?.isOpen ?? false;
            var isQueued =
                QueueUtility<SceneOperation>.queue.Any(o => o.props.open.Any(s => s.scene == scene)) ||
                QueueUtility<SceneOperation>.running.Any(o => o.props.open.Any(s => s.scene == scene));

            var isOpening = SceneLoadAction.currentScene == scene;
            var isPreload = SceneLoadAction.currentScene == scene && SceneLoadAction.isCurrentPreload;

            if (isOpen) return SceneState.Open;
            else if (isPreloaded) return SceneState.Preloaded;
            else if (isPreload) return SceneState.Preloading;
            else if (isOpening) return SceneState.Opening;
            else if (isQueued) return SceneState.Queued;
            else return SceneState.NotOpen;

        }

        Scene GetScene(scene? uScene = default, OpenSceneInfo osi = null) =>
            osi == null
            ? FindOpenScene(uScene.Value).scene
            : osi.scene;

        scene? GetScene(Scene scene = null, OpenSceneInfo osi = null)
        {
            if (osi?.unityScene.HasValue ?? false)
                return osi.unityScene.Value;
            else if (scene)
                return scene.GetOpenSceneInfo()?.unityScene;
            else
                return null;
        }


        OpenSceneInfo GetScene(Scene scene = null, scene? uScene = default) =>
            FindOpenScene(scene) ?? (uScene.HasValue ? FindOpenScene(uScene.Value) : null);

        /// <summary>Finds the <see cref="OpenSceneInfo"/> of this <see cref="scene"/>.</summary>
        public OpenSceneInfo FindOpenScene(scene scene)
        {
            var trackedScene = collection.openScenes.Find(scene) ?? standalone.openScenes.Find(scene);
            //Debug.Log("Found: " + trackedScene?.ToString());
            return trackedScene;
        }

        /// <summary>Finds the open instance of this <see cref="Scene"/>, if it is open.</summary>
        public OpenSceneInfo FindOpenScene(Scene scene)
        {
            if (collection.current && collection.current.Contains(scene))
                return collection.GetTrackedScene(scene);
            else
                return standalone.GetTrackedScene(scene);
        }

        #endregion
        #region Active

        /// <summary>Sets a scene as the activate scene.</summary>
        public void SetActive(scene scene)
        {
            if (scene.isLoaded)
                _ = sceneManager.SetActiveScene(scene);
        }

        /// <inheritdoc cref="SetActive(scene)"/>
        public void SetActive(Scene scene) =>
            SetActive(scene.GetOpenSceneInfo().unityScene.Value);

        /// <summary>Gets the currently open scene.</summary>
        public OpenSceneInfo activeScene =>
            utility.FindOpenScene(sceneManager.GetActiveScene());

        #endregion
        #region DontDestroyOnLoad

        GameObject m_helper;
        GameObject helper
        {
            get
            {

                if (m_helper == null)
                {

                    if (Object.FindObjectOfType<UtilityManager>() is UtilityManager h && h)
                    {
                        m_helper = h.gameObject;
                        return m_helper;
                    }


                    m_helper = new GameObject("Advanced Scene Manager helper");
                    _ = m_helper.AddComponent<UtilityManager>();
                    Object.DontDestroyOnLoad(helper);

                }

                return m_helper;

            }
        }

        Scene m_dontDestroyOnLoadScene;
        Scene dontDestroyOnLoadScene
        {
            get
            {

                if (!m_dontDestroyOnLoadScene)
                {
                    m_dontDestroyOnLoadScene = ScriptableObject.CreateInstance<Scene>();
                    ((Object)m_dontDestroyOnLoadScene).name = "DontDestroyOnLoad";
                }

                return m_dontDestroyOnLoadScene;

            }
        }

        OpenSceneInfo m_dontDestroyOnLoad;
        /// <summary>Represents 'DontDestroyOnLoad' scene.</summary>
        public OpenSceneInfo dontDestroyOnLoad
        {
            get
            {

                if (!Application.isPlaying)
                    throw new InvalidOperationException("Cannot access DontDestroyOnLoad outside of play mode.");

                if (m_dontDestroyOnLoad == null)
                    m_dontDestroyOnLoad = new OpenSceneInfo(dontDestroyOnLoadScene, helper.scene, standalone);
                if (!m_dontDestroyOnLoad.scene)
                    m_dontDestroyOnLoad.scene = dontDestroyOnLoadScene;
                return m_dontDestroyOnLoad;

            }
        }

        /// <summary>Adds the component to the 'Advanced Scene Manager' gameobject in DontDestroyOnLoad.</summary>
        /// <returns><typeparamref name="T"/> if in playmode, otherwise throws <see cref="InvalidOperationException"/>.</returns>
        internal T AddToDontDestroyOnLoad<T>() where T : Component =>
            Application.isPlaying
                ? helper.AddComponent<T>()
                : throw new InvalidOperationException("Cannot access DontDestroyOnLoad outside of play mode.");

        #endregion
        #region Override scene load / unload

        /// <summary>Callback for loading a scene.</summary>
        public delegate IEnumerator SceneLoadOverride(SceneLoadOverrideArgs e);

        /// <summary>Callback for loading a scene.</summary>
        public delegate IEnumerator SceneUnloadOverride(SceneUnloadOverrideArgs e);

        internal SceneLoadOverride sceneLoadOverride;
        internal SceneUnloadOverride sceneUnloadOverride;

        /// <summary>Override the scene load logic.</summary>
        /// <remarks>Can be conditional. You are expected to also handle scene activation in this callback.</remarks>
        public void OverrideSceneLoad(SceneLoadOverride loadOverride, SceneUnloadOverride unloadOverride)
        {
            sceneLoadOverride = loadOverride;
            sceneUnloadOverride = unloadOverride;
        }

        #endregion

    }

}

namespace AdvancedSceneManager.Callbacks
{

    public abstract class SceneLoadUnloadOverrideArgs<T>
    {

        public SceneCollection collection { get; internal set; }
        internal Action<float> updateProgress { get; set; }
        internal bool isHandled { get; set; }

        internal T returnValue { get; set; }

        public void ReportProgress(float progress) =>
            updateProgress.Invoke(progress);

        /// <summary>Gets if this scene is a loading screen.</summary>
        public abstract bool isLoadingScreen { get; }

        /// <summary>Gets if this scene is a splash screen.</summary>
        public abstract bool isSplashScreen { get; }

    }

    public class SceneLoadOverrideArgs : SceneLoadUnloadOverrideArgs<scene>
    {

        public Scene scene { get; internal set; }
        public bool isPreload { get; internal set; }
        internal Func<IEnumerator> preloadCallback { get; set; }

        /// <summary>Notifies ASM that the load is done.</summary>
        /// <param name="scene">The opened scene.</param>
        /// <param name="handled">If <see langword="false"/>, then ASM will load scene like normal.</param>
        public void SetCompleted(scene scene)
        {
            returnValue = scene;
            isHandled = true;
        }

        /// <inheritdoc cref="SetCompleted(scene)"/>
        /// <param name="preloadCallback">Specifies a callback that will be called when it is time to activate preloaded scene.</param>
        public void SetCompleted(scene scene, Func<IEnumerator> preloadCallback)
        {
            this.preloadCallback = preloadCallback;
            SetCompleted(scene);
        }

        /// <summary>Checks if the scene is actually included in build.</summary>
        public bool CheckIsIncluded(bool logError = true)
        {

            if (scene.isIncluded)
                return true;
            else
            {
                if (logError)
                    Debug.LogError($"The scene ('{scene.path}') could not be opened because it is not added to build settings.");
                return false;
            }

        }

        /// <summary>Gets the <see cref="UnityEngine.SceneManagement.Scene"/> that was opened by this override.</summary>
        /// <remarks>Will return <see langword="default"/> if not found.</remarks>
        public scene GetOpenedScene()
        {

            scene? s;
            if (!string.IsNullOrWhiteSpace(scene.path))
                s = sceneManager.GetSceneByPath(scene.path);

            else if (!string.IsNullOrWhiteSpace(scene.name))
                s = sceneManager.GetSceneByName(scene.name);

            else
                s = sceneManager.GetSceneAt(sceneManager.sceneCount - 1);

            Debug.Assert(s?.IsValid() ?? false, "Could not find unity scene after loading it.");

            return s.Value;

        }

        public override bool isLoadingScreen => scene.isLoadingScreen;
        public override bool isSplashScreen => scene.isSplashScreen;

    }

    public class SceneUnloadOverrideArgs : SceneLoadUnloadOverrideArgs<object>
    {

        public Scene scene { get; internal set; }
        public scene unityScene { get; internal set; }

        /// <summary>Notifies ASM that the unload is done.</summary>
        /// <param name="handled">If <see langword="false"/>, then ASM will unload scene like normal.</param>
        public void SetCompleted() =>
            isHandled = true;

        public override bool isLoadingScreen => scene && scene.isLoadingScreen;
        public override bool isSplashScreen => scene && scene.isSplashScreen;

    }

}
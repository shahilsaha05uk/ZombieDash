using System;
using System.Linq;
using AdvancedSceneManager.Exceptions;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using UnityEngine;
using static AdvancedSceneManager.SceneManager;
using scene = UnityEngine.SceneManagement.Scene;
using Scene = AdvancedSceneManager.Models.Scene;

namespace AdvancedSceneManager.Core
{

    /// <summary>The manager for collection scenes.</summary>
    /// <remarks>Usage: <see cref="collection"/>.</remarks>
    public class CollectionManager : SceneManagerBase
    {

        public static implicit operator SceneCollection(CollectionManager manager) =>
            manager.current;

        public static implicit operator bool(CollectionManager manager) =>
            manager.current;

        /// <summary>Called when a collection is opened.</summary>
        public event Action<SceneCollection> opened;

        /// <summary>Called when a collection is closed.</summary>
        public event Action<SceneCollection> closed;

        [SerializeField] internal SceneCollection m_current;
        [SerializeField] internal SceneCollection m_previous;

        /// <summary>The currently open collection.</summary>
        public SceneCollection current => m_current;

        /// <summary>The previously open collection.</summary>
        public SceneCollection previous => m_previous;

        public override void OnAfterDeserialize2()
        {
            base.OnAfterDeserialize2();
            m_current = null;
            m_previous = null;
        }

        /// <summary>Sets <see cref="current"/> to null, make sure to only use this after manually closing <see cref="CollectionManager"/> scenes!</summary>
        internal void SetNull()
        {
            m_previous = m_current;
            m_current = null;
        }

        /// <summary>Sets the collection.</summary>
        internal void Set(SceneCollection collection, params OpenSceneInfo[] scenes)
        {

            m_previous = m_current;
            m_current = collection;
            Clear();

            foreach (var scene in scenes)
            {
                SceneManager.standalone.Remove(scene);
                if (scene.unityScene?.isLoaded ?? false)
                    _ = SceneManager.collection.GetTrackedScene(scene.unityScene.Value);
            }

        }

        #region ISceneOperationsManager<SceneCollection>

        /// <summary>Opens the collection.</summary>
        /// <param name="force">Open even if scene is tagged with DoNotOpen.</param>
        public SceneOperation Open(SceneCollection collection, bool ignoreLoadingScreen = false, bool force = false) =>
            OpenInternal(collection, ignoreLoadingScreen, force);

        /// <summary>Reopens the current collection.</summary>
        public SceneOperation Reopen(bool reopenPersistent = false) =>
            OpenInternal(current, reopenPersistent: reopenPersistent);

        internal SceneOperation OpenInternal(SceneCollection collection, bool ignoreLoadingScreen = false, bool forceOpen = false, bool ignoreQueue = false, bool reopenPersistent = false)
        {

            if (!collection)
                return SceneOperation.done;

            var operation = SceneOperation.Add(this, ignoreQueue).
                    WithCollection(collection, withCallbacks: true).
                    Close(utility.openScenes.Reverse(), force: false).
                    WithLoadingScreen(use: !ignoreLoadingScreen).
                    WithCallback(Callback.AfterLoadingScreenOpen().Do(() =>
                    {
                        if (current)
                            closed?.Invoke(current);
                        m_previous = current;
                        m_current = collection;
                        ActionUtility.Try(() => opened?.Invoke(collection));
                    }));

            //If already open, then we want to reopen it, so we'll need to call Reopen() method instead of Open()
            _ = collection != current
                ? operation.Open(collection.scenes, force: forceOpen)
                : operation.Reopen(collection.scenes.
                            Where(s => collection.Tag(s).openBehavior == SceneOpenBehavior.OpenNormally).
                            Where(s => !reopenPersistent || PersistentUtility.GetPersistentOption(s.GetOpenSceneInfo()?.unityScene ?? default) == SceneCloseBehavior.Close));

            return operation;

        }

        /// <summary>Closes the current collection.</summary>
        public SceneOperation Close() =>
            !standalone.openScenes.Any() && !current
            ? SceneOperation.done
            : SceneOperation.Add(this).
                WithCollection(current, withCallbacks: true).
                Close(utility.openScenes.Reverse(), force: false).
                Close(openScenes.Reverse().Where(s => current.Tag(s.scene).closeBehavior == SceneCloseBehavior.KeepOpenIfNextCollectionAlsoContainsScene), force: true).
                WithCallback(Callback.BeforeLoadingScreenClose().Do(() =>
                {
                    m_previous = current;
                    SetNull();
                    InvokeClosed(previous);
                }));

        internal void InvokeClosed(SceneCollection collection) =>
            ActionUtility.Try(() => closed?.Invoke(collection));

        internal void InvokeOpen(SceneCollection collection) =>
            ActionUtility.Try(() => opened?.Invoke(collection));

        /// <summary>Toggles the collection.</summary>
        /// <param name="enabled">If null, collection will be toggled on or off depending on whatever collection is open or not. Pass a value to ensure that collection either open or closed.</param>
        public SceneOperation Toggle(SceneCollection collection, bool? enabled = null)
        {

            var isOpen = IsOpen(collection);
            var isEnabled = enabled.GetValueOrDefault();

            if (enabled.HasValue)
            {
                if (isEnabled && !isOpen)
                    return Open(collection);
                else if (!isEnabled && isOpen)
                    return Close();
            }
            else
            {
                if (isOpen)
                    return Close();
                else
                    return Open(collection);
            }

            return SceneOperation.done;

        }

        /// <summary>Gets whatever the collection is currently open.</summary>
        public bool IsOpen(SceneCollection collection) =>
            current == collection;

        #endregion
        #region ISceneOperationsManager<Scene>

        /// <summary>Gets whatever the scene can be opened by the current collection.</summary>
        public override bool CanOpen(Scene scene) =>
            current && current.scenes.Contains(scene);

        /// <summary>Opens a scene.</summary>
        /// <remarks>Throws a <see cref="OpenSceneException"/> if the scene cannot be opened by the current collection.</remarks>
        public override SceneOperation<OpenSceneInfo> Open(Scene scene) =>
            CanOpen(scene)
            ? base.Open(scene).WithCollection(this)
            : throw new OpenSceneException(scene, current, "The scene is not part of the current open collection.");

        /// <summary>Opens the scenes.</summary>
        /// <remarks>Throws a <see cref="OpenSceneException"/> if a scene cannot be opened by the current collection.</remarks>
        public override SceneOperation<OpenSceneInfo[]> OpenMultiple(params Scene[] scenes)
        {

            foreach (var scene in scenes)
                if (!CanOpen(scene))
                    throw new OpenSceneException(scene, current, "The scene is not part of the current open collection.");

            return base.OpenMultiple(scenes).WithCollection(this);

        }

        /// <summary>Closes a scene.</summary>
        /// <remarks>Throws a <see cref="CloseSceneException"/> if the scene is not a part of the current collection.</remarks>
        public override SceneOperation Close(OpenSceneInfo scene)
        {
            if (!CanOpen(scene.scene))
                throw new CloseSceneException(scene.scene, scene.unityScene.Value, current, "The scene is not part of the current open collection.");
            else
                return base.Close(scene).WithCollection(this);
        }

        /// <summary>Closes the scenes.</summary>
        /// <remarks>Throws a <see cref="CloseSceneException"/> if a scene is not a part of the current collection.</remarks>
        public override SceneOperation CloseMultiple(params OpenSceneInfo[] scenes)
        {

            scenes = scenes.Where(s => s?.unityScene.HasValue ?? false).ToArray();
            foreach (var scene in scenes)
                if (!CanOpen(scene.scene))
                    throw new CloseSceneException(scene.scene, scene.unityScene.Value, current, "The scene is not part of the current open collection.");

            return base.CloseMultiple(scenes).WithCollection(this);

        }

        #endregion

    }

}

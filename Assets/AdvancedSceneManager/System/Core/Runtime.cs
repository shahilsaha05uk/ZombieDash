using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Models.Enums;
using AdvancedSceneManager.Utility;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AdvancedSceneManager.Core
{

    /// <summary>Manages runtime functionality for Advanced Scene Manager such as open scenes and collection.</summary>
    public class Runtime :
        Scene.IMethods_Target,
        SceneCollection.IMethods_Target
    {

#if UNITY_EDITOR

        [InitializeOnEnterPlayMode]
        static void OnEnterPlaymode() =>
            SceneManager.OnInitialized(SceneManager.runtime.Reset);

        [InitializeInEditorMethod]
        static void OnInitialize() =>
            SceneManager.OnInitialized(() =>
            {
                if (Application.isPlaying)
                    foreach (var scene in SceneUtility.GetAllOpenUnityScenes())
                        if (scene.ASMScene(out var s))
                            SceneManager.runtime.Track(scene, s);
            });

#endif

        public Runtime()
        {
            AddSceneLoader<RuntimeSceneLoader>();
            QueueUtility<SceneOperation>.queueFilled += () => startedWorking?.Invoke();
            QueueUtility<SceneOperation>.queueEmpty += () => stoppedWorking?.Invoke();
            sceneClosed += Runtime_sceneClosed;
        }

        void Runtime_sceneClosed(Scene scene)
        {
            var collections = openAdditiveCollections.Where(c => !c.scenes.Any(s => s && s.isOpen));
            foreach (var collection in collections.ToArray())
                Untrack(collection, isAdditive: true);
        }

        #region Properties

        internal void Reset()
        {
            UntrackScenes();
            UntrackPreload();
            UntrackCollections();
        }

        private readonly List<Scene> m_openScenes = new();
        private SceneCollection m_openCollection
        {
            get => SceneManager.settings.project.openCollection;
            set => SceneManager.settings.project.openCollection = value;
        }

        private Scene m_preloadedScene;
        private Func<IEnumerator> preloadCallback;

        /// <summary>Gets the scenes that are open.</summary>
        public IEnumerable<Scene> openScenes => m_openScenes.NonNull();

        /// <summary>Gets the collections that are opened as additive.</summary>
        public IEnumerable<SceneCollection> openAdditiveCollections => SceneManager.settings.project.openAdditiveCollections.NonNull().Distinct();

        /// <summary>Gets the collection that is currently open.</summary>
        public SceneCollection openCollection => m_openCollection;

        /// <summary>Gets the scene that is currently preloaded.</summary>
        public Scene preloadedScene
        {
            get
            {
                if (m_preloadedScene && !m_preloadedScene.isPreloaded)
                    UntrackPreload();
                return m_preloadedScene;
            }
        }

        #endregion
        #region Scene loaders

        internal List<SceneLoader> sceneLoaders = new();

        /// <summary>Gets a list of all added scene loaders that can be toggled scene by scene.</summary>
        public IEnumerable<SceneLoader> GetToggleableSceneLoaders() =>
            sceneLoaders.Where(l => !l.isGlobal && !string.IsNullOrWhiteSpace(l.sceneToggleText));

        /// <summary>Gets the loader for <paramref name="scene"/>.</summary>
        public SceneLoader GetLoaderForScene(Scene scene)
        {
            var loaders = sceneLoaders.Where(l => l.canBeActivated).ToArray();
            return loaders.FirstOrDefault(l => Match(l, scene)) ?? loaders.FirstOrDefault(l => l.isGlobal && l.CanOpen(scene));
        }

        bool Match(SceneLoader loader, Scene scene) =>
            loader.GetType().FullName == scene.sceneLoader && loader.CanOpen(scene);

        /// <summary>Adds a scene loader.</summary>
        public void AddSceneLoader<T>() where T : SceneLoader, new()
        {
            var key = SceneLoader.GetKey<T>();
            sceneLoaders.RemoveAll(l => l.Key == key);
            sceneLoaders.Add(new T());
        }

        /// <summary>Removes a scene loader.</summary>
        public void RemoveSceneLoader<T>() =>
            sceneLoaders.RemoveAll(l => l is T);

        #endregion
        #region Scene

        bool IsValid(Scene scene) => scene;
        bool IsClosed(Scene scene) => !openScenes.Contains(scene);
        bool IsOpen(Scene scene) => openScenes.Contains(scene);
        bool CanOpen(Scene scene, SceneCollection collection, bool openAllScenes) => openAllScenes || !collection.scenesThatShouldNotAutomaticallyOpen.Contains(scene);
        bool LoadingScreen(Scene scene) => LoadingScreenUtility.IsLoadingScreenOpen(scene);

        bool IsPersistent(Scene scene, SceneCollection closeCollection = null, SceneCollection nextCollection = null) =>
            (scene.isPersistent && !closeCollection)
            || (scene.keepOpenWhenNewCollectionWouldReopen && nextCollection && nextCollection.Contains(scene));

        bool NotPersistent(Scene scene, SceneCollection closeCollection = null, SceneCollection nextCollection = null) =>
            !IsPersistent(scene, closeCollection, nextCollection);

        bool NotPersistent(Scene scene, SceneCollection closeCollection = null) =>
            !IsPersistent(scene, closeCollection);

        bool NotLoadingScreen(Scene scene) =>
            !LoadingScreen(scene);

        #region Open

        public SceneOperation Open(Scene scene) =>
            Open(scenes: scene);

        /// <inheritdoc cref="Open(IEnumerable{Scene})"/>
        public SceneOperation Open(params Scene[] scenes) =>
            Open((IEnumerable<Scene>)scenes);

        /// <summary>Opens the scenes.</summary>
        /// <remarks>Open scenes will not be re-opened, please close it first.</remarks>
        public SceneOperation Open(IEnumerable<Scene> scenes)
        {

            if (SceneManager.runtime.currentOperation?.phase is Phase.OpenCallbacks)
            {
                //User is attempting to open a scene in a open callback, lets make that operation wait for this one
                var operation = SceneOperation.Start().Open(
                    scenes.
                    Where(IsValid).
                    Where(IsClosed));
                SceneManager.runtime.currentOperation.WaitFor(operation);
                return operation;
            }
            else
                return SceneOperation.Queue().Open(
                    scenes.
                    Where(IsValid).
                    Where(IsClosed));

        }

        public SceneOperation OpenWithLoadingScreen(Scene scene, Scene loadingScreen) =>
            Open(scene).With(loadingScreen);

        /// <summary>Opens a scene with a loading screen.</summary>
        public SceneOperation OpenWithLoadingScreen(IEnumerable<Scene> scene, Scene loadingScreen) =>
            Open(scene).With(loadingScreen);

        #endregion
        #region Close

        public SceneOperation Close(Scene scene) =>
            Close(scenes: scene);

        /// <inheritdoc cref="Close(IEnumerable{Scene})"/>
        public SceneOperation Close(params Scene[] scenes) =>
            Close((IEnumerable<Scene>)scenes);

        /// <summary>Closes the scenes.</summary>
        /// <remarks>Closes persistent scenes.</remarks>
        public SceneOperation Close(IEnumerable<Scene> scenes) =>
            SceneOperation.Queue().Close(
                scenes.
                Where(IsValid).
                Where(IsOpen));

        #endregion
        #region Preload

        SceneOperation LogAndReturn(string message)
        {
            Debug.LogError(message);
            return SceneOperation.done;
        }

        internal void TrackPreload(Scene scene, Func<IEnumerator> preloadCallback)
        {
            m_preloadedScene = scene;
            this.preloadCallback = () => Coroutine(preloadCallback);

            IEnumerator Coroutine(Func<IEnumerator> preloadCallback)
            {
                yield return preloadCallback();
                scenePreloadFinished?.Invoke(scene);
            }

            if (scene)
                scenePreloaded?.Invoke(scene);
        }

        internal void UntrackPreload() =>
            TrackPreload(null, null);

        public SceneOperation Preload(Scene scene, Action onPreloaded = null)
        {

            if (preloadedScene && !preloadedScene.isPreloaded)
                UntrackPreload();

            if (!scene || preloadedScene == scene)
                return SceneOperation.done;

            if (preloadedScene)
            {
                Debug.LogError("Only a single scene can be preloaded at a time.");
                return SceneOperation.done;
            }

            if (IsOpen(scene))
                return SceneOperation.done;

            return SceneOperation.Queue().Preload(scene).Callback(Callback.BeforeLoadingScreenClose().Do(onPreloaded));

        }

        public SceneOperation FinishPreload(Scene scene)
        {

            if (!scene || preloadedScene != scene)
                return LogAndReturn("Scene is not preloaded.");

            if (!scene.isPreloaded)
                return LogAndReturn("Scene is not preloaded.");

            return FinishPreload();

        }

        /// <summary>Finishes the preload of the currently preloaded scene.</summary>
        public SceneOperation FinishPreload()
        {

            if (!preloadedScene)
                return LogAndReturn("Scene is not preloaded.");

            return SceneOperation.Start().Callback(Callback.BeforeLoadingScreenClose().Do(preloadCallback));

        }

        /// <summary>Discards preload of the scene, if preloaded.</summary>
        public SceneOperation DiscardPreload(Scene scene)
        {

            if (!scene || preloadedScene != scene)
                return LogAndReturn("Scene is not preloaded.");

            if (!scene.isPreloaded)
                return LogAndReturn("Scene is not preloaded.");

            return DiscardPreload();

        }

        /// <summary>Discards the preload of the currently preloaded scene.</summary>
        public SceneOperation DiscardPreload()
        {

            //if (!preloadedScene)
            //    return LogAndReturn("Scene is not preloaded.");

            return SceneOperation.Start().Callback(Callback.AfterLoadingScreenOpen().Do(Coroutine));

            IEnumerator Coroutine()
            {
                yield return preloadCallback;
                yield return SceneOperation.Start().Close(preloadedScene);
                UntrackPreload();
            }

        }

        #endregion
        #region Toggle

        /// <inheritdoc cref="ToggleOpen(Scene, bool?)"/>
        public SceneOperation ToggleOpenState(Scene scene) =>
            IsOpen(scene)
            ? Close(scene)
            : Open(scene);

        /// <summary>Toggles the open state of this scene.</summary>
        public SceneOperation ToggleOpen(Scene scene, bool? openState = null)
        {

            if (!openState.HasValue)
                return ToggleOpenState(scene);
            else if (openState.Value && !IsOpen(scene))
                return Open(scene);
            else if (!openState.Value && IsOpen(scene))
                return Close(scene);

            return SceneOperation.done;

        }

        #endregion
        #region Active

        /// <summary>Gets the active scene.</summary>
        /// <remarks>Returns <see langword="null"/> if the active scene is not imported.</remarks>
        public Scene activeScene =>
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().ASMScene();

        /// <summary>Sets the scene as active.</summary>
        /// <remarks>No effect if not open.</remarks>
        public void SetActive(Scene scene)
        {

            if (!scene || !scene.isOpen)
                return;

            if (scene.internalScene.HasValue && scene.internalScene.Value.isLoaded)
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene.internalScene.Value);
            else
                Debug.LogError("Could not set active scene since internalScene not valid.");

        }

        #endregion

        #endregion
        #region Collection

        bool IsOpen(SceneCollection collection) =>
            collection && (openCollection == collection || openAdditiveCollections.Contains(collection));

        IEnumerable<Scene> ScenesToClose(SceneCollection closeCollection = null, SceneCollection nextCollection = null, SceneCollection additiveCloseCollection = null)
        {

            var list =
                (additiveCloseCollection ? additiveCloseCollection.scenes : openScenes.Where(s => !openAdditiveCollections.Any(c => c.Contains(s)))).
                Where(IsValid).
                Where(IsOpen).
                Where(NotLoadingScreen).
                Where(s => NotPersistent(s, closeCollection, nextCollection));

            if (SceneManager.settings.project.reverseUnloadOrderOnCollectionClose)
                list = list.Reverse();

            return list;

        }

        #region Open

        public SceneOperation Open(SceneCollection collection, bool openAll = false) =>
            collection
            ? Open(SceneOperation.Queue(), collection, openAll)
            : SceneOperation.done;

        /// <inheritdoc cref="Open(SceneCollection, bool)"/>
        internal SceneOperation Open(SceneOperation operation, SceneCollection collection, bool openAll = false) =>
            operation.With(collection, true).
            Callback(TrackCollectionCallback(collection)).
            Close(ScenesToClose(nextCollection: collection)).
            Open(collection.scenes.
                Where(IsValid).
                Where(IsClosed).
                Where(s => CanOpen(s, collection, openAll)));

        /// <summary>Opens the collection without closing existing scenes.</summary>
        /// <param name="collection">The collection to open.</param>
        /// <param name="openAll">Specifies whatever all scenes should open, regardless of open flag.</param>
        public SceneOperation OpenAdditive(SceneCollection collection, bool openAll = false)
        {

            if (!collection)
                return SceneOperation.done;

            if (m_openCollection == collection)
                throw new ArgumentException("Cannot open collection as additive if it is already open normally.");

            return SceneOperation.Queue().
                With(collection, collection.setActiveSceneWhenOpenedAsActive).
                Callback(TrackCollectionCallback(collection, true)).
                DisableLoadingScreen().
                Open(collection.scenes.
                    Where(IsValid).
                    Where(IsClosed).
                    Where(s => CanOpen(s, collection, openAll)));

        }

        #endregion
        #region Close

        public SceneOperation Close(SceneCollection collection) =>
            IsOpen(collection)
            ? Close(SceneOperation.Queue(), collection)
            : SceneOperation.done;

        /// <inheritdoc cref="Close(SceneCollection)"/>
        internal SceneOperation Close(SceneOperation operation, SceneCollection collection) =>
            collection.isOpenAdditive
            ? operation.
                With(collection).
                Callback(UntrackCollectionCallback(true)).
                Close(ScenesToClose(collection, additiveCloseCollection: collection))
            : operation.
                With(collection).
                Callback(UntrackCollectionCallback()).
                Close(ScenesToClose(collection));

        #endregion
        #region Toggle

        /// <inheritdoc cref="ToggleOpen(SceneCollection, bool?, bool)"/>
        public SceneOperation ToggleOpenState(SceneCollection collection, bool openAll = false) =>
            IsOpen(collection)
            ? Close(collection)
            : Open(collection, openAll);

        public SceneOperation ToggleOpen(SceneCollection collection, bool? openState = null, bool openAll = false)
        {

            if (!openState.HasValue)
                return ToggleOpenState(collection, openAll);
            else if (openState.Value && !IsOpen(collection))
                return Open(collection, openAll);
            else if (!openState.Value && IsOpen(collection))
                return Close(collection);

            return SceneOperation.done;

        }

        #endregion

        #endregion
        #region SceneState

        /// <summary>Gets the current state of the scene.</summary>
        public SceneState GetState(Scene scene)
        {

            if (!scene)
                return SceneState.Unknown;

            if (!scene.internalScene.HasValue)
                return SceneState.NotOpen;

            if (FallbackSceneUtility.IsFallbackScene(scene.internalScene.Value))
                throw new InvalidOperationException("Fallback scene is tracked by a Scene, this should not happen, something went wrong somewhere.");

            var isPreloaded = scene.internalScene.HasValue && !scene.internalScene.Value.isLoaded;
            var isOpen = openScenes.Contains(scene);
            var isQueued =
                QueueUtility<SceneOperation>.queue.Any(o => o.open?.Contains(scene) ?? false) ||
                QueueUtility<SceneOperation>.running.Any(o => o.open?.Contains(scene) ?? false);

            var isOpening = SceneOperation.currentLoadingScene == scene;
            var isPreloading = (SceneOperation.currentLoadingScene == scene && SceneOperation.isCurrentLoadingScenePreload);

            if (isPreloaded) return SceneState.Preloaded;
            else if (isPreloading) return SceneState.Preloading;
            else if (isOpen) return SceneState.Open;
            else if (isOpening) return SceneState.Opening;
            else if (isQueued) return SceneState.Queued;
            else return SceneState.NotOpen;

        }

        #endregion
        #region DontDestroyOnLoad

        [AddComponentMenu("")]
        /// <summary>Helper script hosted in DontDestroyOnLoad.</summary>
        internal class ASM : MonoBehaviour
        { }

        internal UnityEngine.SceneManagement.Scene dontDestroyOnLoadScene => helper ? helper.scene : default;
        bool hasDontDestroyOnLoadScene;

#if UNITY_EDITOR
        [InitializeOnEnterPlayMode]
        static void UnsetHasDontDestroyOnLoadScene() =>
            SceneManager.runtime.hasDontDestroyOnLoadScene = false;
#endif

        GameObject m_helper;
        GameObject helper
        {
            get
            {

                if (!Application.isPlaying)
                    return null;

                if (!m_helper && !hasDontDestroyOnLoadScene)
                {

                    var script = Object.FindFirstObjectByType<ASM>();
                    if (script)
                        m_helper = script.gameObject;
                    else
                    {
                        m_helper = new GameObject("ASM helper");
                        _ = m_helper.AddComponent<ASM>();
                        hasDontDestroyOnLoadScene = true;
                    }

                    Object.DontDestroyOnLoad(m_helper);

                }

                return m_helper;

            }
        }

        Scene m_dontDestroyOnLoadScene;

        /// <summary>Gets the dontDestroyOnLoad scene.</summary>
        /// <remarks>Returns <see langword="null"/> outside of play mode.</remarks>
        public Scene dontDestroyOnLoad
        {
            get
            {

                if (!Application.isPlaying)
                    return null;

                if (!m_dontDestroyOnLoadScene)
                {
                    m_dontDestroyOnLoadScene = ScriptableObject.CreateInstance<Scene>();
                    ((Object)m_dontDestroyOnLoadScene).name = "DontDestroyOnLoad";
                }

                if (m_dontDestroyOnLoadScene.internalScene?.handle != dontDestroyOnLoadScene.handle)
                    m_dontDestroyOnLoadScene.internalScene = dontDestroyOnLoadScene;

                return m_dontDestroyOnLoadScene;

            }
        }

        /// <inheritdoc cref="AddToDontDestroyOnLoad{T}(out T)"/>
        internal bool AddToDontDestroyOnLoad<T>() where T : Component =>
            AddToDontDestroyOnLoad<T>(out _);

        /// <summary>Adds the component to the 'Advanced Scene Manager' gameobject in DontDestroyOnLoad.</summary>
        /// <remarks>Returns <see langword="false"/> outside of play-mode.</remarks>
        internal bool AddToDontDestroyOnLoad<T>(out T component) where T : Component
        {

            component = null;

            if (helper && helper.gameObject)
            {
                component = helper.gameObject.AddComponent<T>();
                return true;
            }
            else
                Debug.LogError("Cannot access DontDestroyOnLoad outside of play mode.");

            return false;

        }

        /// <summary>Adds the component to a new gameobject in DontDestroyOnLoad.</summary>
        /// <remarks>Returns <see langword="false"/> outside of play-mode.</remarks>
        internal bool AddToDontDestroyOnLoad<T>(out T component, out GameObject obj) where T : Component
        {

            obj = null;
            component = null;
            if (Application.isPlaying)
            {
                obj = new GameObject(typeof(T).Name);
                Object.DontDestroyOnLoad(obj);
                component = obj.AddComponent<T>();
                return true;
            }
            else
                Debug.LogError("Cannot access DontDestroyOnLoad outside of play mode.");

            return false;

        }

        #endregion
        #region Events

        /// <summary>Occurs when a scene is opened.</summary>
        public event Action<Scene> sceneOpened;

        /// <summary>Occurs when a scene is closed.</summary>
        public event Action<Scene> sceneClosed;

        /// <summary>Occurs when a collection is opened.</summary>
        public event Action<SceneCollection> collectionOpened;

        /// <summary>Occurs when a collection is closed.</summary>
        public event Action<SceneCollection> collectionClosed;

        /// <summary>Occurs when a scene is preloaded.</summary>
        public event Action<Scene> scenePreloaded;

        /// <summary>Occurs when a previously preloaded scene is opened.</summary>
        public event Action<Scene> scenePreloadFinished;

        /// <summary>Occurs when the last user scene closes.</summary>
        /// <remarks> 
        /// <para>This usually happens by mistake, and likely means that no user code would run, this is your chance to restore to a known state (return to main menu, for example), or crash to desktop.</para>
        /// <para>Returning to main menu can be done like this:<code>SceneManager.app.Restart()</code></para>
        /// </remarks>
        public Action onAllScenesClosed;

        internal void OnAllScenesClosed() =>
            onAllScenesClosed?.Invoke();

        #endregion
        #region Tracking

        #region Scenes

        /// <summary>Tracks the specified scene as open.</summary>
        /// <remarks>Does not open scene.</remarks>
        public void Track(Scene scene, UnityEngine.SceneManagement.Scene unityScene)
        {

            if (!scene)
                return;

            if (!FallbackSceneUtility.IsFallbackScene(unityScene))
                scene.internalScene = unityScene;

            Track(scene);

        }

        /// <inheritdoc cref="Track(Scene, UnityEngine.SceneManagement.Scene)"/>
        public void Track(Scene scene)
        {

            if (!scene)
                return;

            if (!scene.internalScene.HasValue)
                FindAssociatedScene(scene);

            if (FallbackSceneUtility.IsFallbackScene(scene.internalScene ?? default))
            {
                scene.internalScene = null;
                return;
            }

            if (!m_openScenes.Contains(scene))
            {

                m_openScenes.Add(scene);
                scene.OnPropertyChanged(nameof(Scene.isOpen));
                sceneOpened?.Invoke(scene);

                LogUtility.LogTracked(scene);

            }

        }

        /// <summary>Untracks the specified scene as open.</summary>
        /// <remarks>Does not close scene.</remarks>
        public bool Untrack(Scene scene)
        {

            if (scene && m_openScenes.Remove(scene))
            {

                if (scene == preloadedScene)
                    UntrackPreload();

                scene.internalScene = null;

                scene.OnPropertyChanged(nameof(Scene.isOpen));
                sceneClosed?.Invoke(scene);
                LogUtility.LogUntracked(scene);

                return true;

            }

            return false;

        }

        /// <summary>Untracks all open scenes.</summary>
        /// <remarks>Does not close scenes.</remarks>
        public void UntrackScenes()
        {
            foreach (var scene in m_openScenes.ToArray())
                Untrack(scene);
            m_openScenes.Clear();
        }

        void FindAssociatedScene(Scene scene)
        {
            scene.internalScene = SceneUtility.GetAllOpenUnityScenes().FirstOrDefault(s => s.IsValid() && s.path == scene.path);
            if (!scene.internalScene.HasValue)
                throw new InvalidOperationException("Cannot track scene without a associated unity scene.");
        }

        #endregion
        #region Collections

        /// <summary>Tracks the collection as open.</summary>
        /// <remarks>Does not open collection.</remarks>
        public void Track(SceneCollection collection, bool isAdditive = false)
        {

            if (!collection)
                return;

            if (!isAdditive && collection != m_openCollection)
            {
                m_openCollection = collection;
                collection.OnPropertyChanged(nameof(collection.isOpenNonAdditive));
                collection.OnPropertyChanged(nameof(collection.isOpen));
                collectionOpened?.Invoke(collection);
                LogUtility.LogTracked(collection);
            }
            else if (isAdditive && !openAdditiveCollections.Contains(collection))
            {
                SceneManager.settings.project.AddAdditiveCollection(collection);
                collection.OnPropertyChanged(nameof(collection.isOpenAdditive));
                collection.OnPropertyChanged(nameof(collection.isOpen));
                LogUtility.LogTracked(collection, true);
            }

        }

        /// <summary>Untracks the collection.</summary>
        /// <remarks>Does not close the collection.</remarks>
        public void Untrack(SceneCollection collection, bool isAdditive = false)
        {

            if (!collection)
                return;

            if (!isAdditive && collection == openCollection)
            {

                m_openCollection = null;

                collection.OnPropertyChanged(nameof(collection.isOpenNonAdditive));
                collection.OnPropertyChanged(nameof(collection.isOpen));
                collectionClosed?.Invoke(collection);
                LogUtility.LogUntracked(collection);

                //Untrack all additive collections
                //openAdditiveCollections.ToArray().ForEach(c => Untrack(c, true));

            }
            else if (isAdditive && openAdditiveCollections.Contains(collection))
            {
                SceneManager.settings.project.RemoveAdditiveCollection(collection);
                collection.OnPropertyChanged(nameof(collection.isOpenAdditive));
                collection.OnPropertyChanged(nameof(collection.isOpen));
                LogUtility.LogUntracked(collection, true);
            }

        }

        /// <summary>Untracks all collections.</summary>
        /// <remarks>Does not close collections.</remarks>
        public void UntrackCollections()
        {
            Untrack(openCollection);
            openAdditiveCollections.ForEach(c => Untrack(c, true));
        }

        Callback TrackCollectionCallback(SceneCollection collection, bool isAdditive = false) =>
            Callback.Before(Phase.LoadScenes).Do(() =>
            {
                Untrack(openCollection, isAdditive);
                if (!isAdditive) Untrack(openCollection, true); //Make sure additive collection is removed when it is opened property
                Track(collection, isAdditive);
            });

        Callback UntrackCollectionCallback(bool isAdditive = false) =>
            Callback.BeforeLoadingScreenClose().Do(() =>
            {
                Untrack(openCollection, isAdditive);
            });

        #endregion

        /// <summary>Gets whatever this scene is tracked as open.</summary>
        public bool IsTracked(Scene scene) =>
            scene && scene.internalScene.HasValue &&
            (scene.isDontDestroyOnLoad ||
            FallbackSceneUtility.IsFallbackScene(scene.internalScene ?? default) ||
            openScenes.Any(s => s.id == scene.id));

        /// <summary>Gets whatever this collection is tracked as open.</summary>
        public bool IsTracked(SceneCollection collection) =>
            openCollection == collection || openAdditiveCollections.Contains(collection);

        #endregion
        #region Queue

        /// <summary>Occurs when ASM has started working and is running scene operations.</summary>
        public event Action startedWorking;

        /// <summary>Occurs when ASM has finished working and no scene operations are running.</summary>
        public event Action stoppedWorking;

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

        #endregion

        /// <summary>Closes all scenes and collections.</summary>
        public SceneOperation CloseAll(bool exceptLoadingScreens = true, bool exceptUnimported = true, params Scene[] except)
        {

            var scenes = openScenes;
            if (exceptLoadingScreens)
                scenes = scenes.Where(s => !s.isLoadingScreen && !except.Contains(s));

            if (SceneManager.settings.project.reverseUnloadOrderOnCollectionClose)
                scenes = scenes.Reverse();

            var operation = Close(scenes).Callback(UntrackCollectionCallback()).Callback(Callback.BeforeLoadingScreenClose().Do(UntrackPreload));

            if (!exceptUnimported)
                operation.Callback(Callback.After(Phase.UnloadScenes).Do(CloseUnimportedScenes));

            return operation;

            IEnumerator CloseUnimportedScenes()
            {

                var scenes = SceneUtility.GetAllOpenUnityScenes().
                    Where(s => !s.ASMScene()).
                    Where(s => !FallbackSceneUtility.IsFallbackScene(s)).
                    ToArray();

                foreach (var scene in scenes)
                    yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);

            }

        }

    }

}

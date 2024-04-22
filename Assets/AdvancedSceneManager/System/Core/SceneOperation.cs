using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Models.Enums;
using AdvancedSceneManager.Utility;
using Lazy.Utility;
using UnityEditor;
using UnityEngine;
using Scene = AdvancedSceneManager.Models.Scene;
using When = AdvancedSceneManager.Core.Callback.When;

namespace AdvancedSceneManager.Core
{

    /// <summary>A scene operation is a queueable operation that can open or close scenes. See also: <see cref="SceneAction"/>.</summary>
    public class SceneOperation : CustomYieldInstruction, IQueueable
    {

        #region Scene operation

        #region Constructor / queue

        /// <summary>Gets a <see cref="SceneOperation"/> that has already completed.</summary>
        public static SceneOperation done { get; } = new SceneOperation() { hasRun = true };

        static SceneOperation() =>
            QueueUtility<SceneOperation>.queueEmpty += ResetThreadPriority;

        /// <summary>Queues a new scene operation.</summary>
        public static SceneOperation Queue() =>
            QueueUtility<SceneOperation>.Queue(new());

        /// <summary>Starts a new scene operation, ignoring queue.</summary>
        public static SceneOperation Start() =>
            QueueUtility<SceneOperation>.Queue(new(), true);

        GlobalCoroutine operationCoroutine;

        void IQueueable.OnTurn(Action onComplete) =>
             operationCoroutine = Run()?.StartCoroutine(description: description ?? "SceneOperation", onComplete: () =>
             {
                 coroutine?.Stop();
                 onComplete.Invoke();
                 ClearProgress();
             });

        void IQueueable.OnCancel() =>
            Cancel();

        bool IQueueable.CanQueue()
        {

            if (!Profile.current)
                throw new InvalidOperationException("Cannot queue a scene operation with no active profile.");

            return SceneManager.runtime.preloadedScene
                ? throw new InvalidOperationException("Cannot queue a scene operation when a scene is preloaded. Please finish preload using SceneManager.management.FinishPreload() / .DiscardPreload(). Scene helper can also be used.")
                : true;

        }

        static void ResetThreadPriority()
        {
            if (Profile.current.enableChangingBackgroundLoadingPriority)
                Application.backgroundLoadingPriority = Profile.current.backgroundLoadingPriority;
        }

        /// <summary>Specifies description for coroutine.</summary>
        public string description { get; protected set; }

        /// <summary>Specifies description for coroutine.</summary>
        public SceneOperation WithFriendlyText(string text) =>
            Set(() =>
            {
                description = text;
                if (operationCoroutine is not null)
                    operationCoroutine.description = description;
            });

        #endregion
        #region Duplicate check

        bool IsDuplicate() =>
            SceneManager.settings.project.checkForDuplicateSceneOperations &&
            QueueUtility<SceneOperation>.running.Concat(QueueUtility<SceneOperation>.queue).Any(o => o != this && IsDuplicate(this, o));

        static bool IsDuplicate(SceneOperation left, SceneOperation right)
        {

            if (left.isLoadingScreen || right.isLoadingScreen)
                return false;

            if (left.open.Count() + left.close.Count() == 0 ||
                right.open.Count() + right.close.Count() == 0)
                return false;

            if (left.open.SequenceEqual(right.open) && left.close.SequenceEqual(right.close))
                return true;

            return false;

        }

        #endregion
        #region Cancel

        /// <summary>Cancel this operation.</summary>
        /// <remarks>Note that the operation might not be cancelled immediately, if user defined callbacks are currently running.</remarks>
        public void Cancel()
        {
            wasCancelled = true;
            coroutine?.Stop();
        }

        #endregion

        #region Fields

        SceneCollection m_collection;
        bool m_setActiveCollectionScene;

        Scene m_focus;

        //Lists
        IEnumerable<Scene> m_open = Enumerable.Empty<Scene>();
        IEnumerable<Scene> m_close = Enumerable.Empty<Scene>();
        IEnumerable<Callback> m_callbacks = Enumerable.Empty<Callback>();

        Scene m_preload;

        //Loading
        Scene m_loadingScene;
        Action<LoadingScreen> m_loadingScreenCallback;
        bool m_useLoadingScene = true;
        ThreadPriority? m_loadingPriority;

        //Options
        bool? m_unloadUnusedAssets;

        Progress<float> m_customProgress;

        #endregion
        #region Properties

        /// <summary>Specifies the collection that is being opened or closed.</summary>
        public SceneCollection collection => m_collection;

        /// <summary>Specifies whatever active scene should be set when possible.</summary>
        public bool setActiveCollectionScene => m_setActiveCollectionScene;

        /// <summary>Sets focus to the specified scene. Overrides selected scene in collections.</summary>
        /// <remarks>No effect if preloading.</remarks>
        public Scene focus => m_focus;


        //Lists

        /// <summary>Gets the scenes specified to open.</summary>
        /// <remarks>List will change depending on when its called (i.e. only closed scenes can be opened).</remarks>
        public IEnumerable<Scene> open => m_open;

        /// <summary>Gets the scenes specified to close.</summary>
        /// <remarks>List will change depending on when its called (i.e. only open scenes can be closed).</remarks>
        public IEnumerable<Scene> close => m_close;

        /// <summary>Gets the user defined callbacks.</summary>
        public IEnumerable<Callback> callbacks => m_callbacks;

        /// <summary>Gets the scene specified to preload.</summary>
        public Scene preload => m_preload;

        //Loading

        /// <summary>Gets the specified loading screen.</summary>
        public Scene loadingScene => m_loadingScene;

        /// <summary>Gets the specified loading screen callback.</summary>
        public Action<LoadingScreen> loadingScreenCallback => m_loadingScreenCallback;

        /// <summary>Gets whatever a loading screen should be used.</summary>
        public bool useLoadingScene => m_useLoadingScene;

        /// <summary>Gets the specified <see cref="ThreadPriority"/> to be used.</summary>
        public ThreadPriority? loadingPriority => m_loadingPriority;

        //Options

        /// <summary>Gets whatever <see cref="Resources.UnloadUnusedAssets"/> should be called at the end (before loading screen).</summary>
        public bool? unloadUnusedAssets => m_unloadUnusedAssets;

        /// <summary>Gets the scenes that was closed during this operation.</summary>
        public IEnumerable<Scene> closedScenes => m_closedScenes;

        /// <summary>Gets the scenes that was opened during this operation.</summary>
        public IEnumerable<Scene> openedScenes => m_openedScenes;

        readonly List<Scene> m_closedScenes = new();
        readonly List<Scene> m_openedScenes = new();

        /// <summary>Specifies whatever this scene operation was started by ASM to open a loading screen.</summary>
        public bool isLoadingScreen { get; internal set; }

        /// <summary>Inherited from <see cref="CustomYieldInstruction"/>. Tells unity whatever the operation is done or not.</summary>
        /// <summary>Inherited from <see cref="CustomYieldInstruction"/>. Tells unity whatever the operation is done or not.</summary>
        public override bool keepWaiting
        {
            get
            {

                if (hasRun)
                    return false;

                if (this == done)
                    return false;

                if (!isFrozen)
                    return true;

                return operationCoroutine?.keepWaiting ?? false;

            }
        }

        /// <summary>The phase the this scene operation is currently in.</summary>
        public Phase phase { get; private set; }

        /// <summary>Gets if this scene operation is cancelled.</summary>
        public bool wasCancelled { get; private set; }

        /// <summary>Gets custom progress, if there is any. Will be counted as part of <see cref="progress"/>.</summary>
        public Progress<float> customProgress { get; private set; }

        #endregion
        #region Fluent api

        //Collection

        /// <summary>Specifies an associated collection.</summary>
        public SceneOperation With(SceneCollection collection, bool setActiveScene = false) =>
            Set(() => { m_collection = collection; m_setActiveCollectionScene = setActiveScene; });

        /// <inheritdoc cref="Runtime.Open(SceneOperation, SceneCollection, bool)"/>
        public SceneOperation Open(SceneCollection collection, bool openAll = false) =>
            SceneManager.runtime.Open(this, collection, openAll);

        /// <inheritdoc cref="Runtime.Close(SceneOperation, SceneCollection)"/>
        public SceneOperation Close(SceneCollection collection) =>
            SceneManager.runtime.Close(this, collection);

        /// <summary>Sets focus to the specified scene. Overrides selected scene in collections.</summary>
        /// <remarks>No effect if preloading.</remarks>
        public SceneOperation Focus(Scene scene) =>
            Set(() => m_focus = scene);

        //Lists

        /// <summary>Specifies the scenes to open.</summary>
        /// <remarks>Can be called multiple times to add more scenes.</remarks>
        public SceneOperation Open(params Scene[] scenes) => Open(scenes.AsEnumerable());
        public SceneOperation PrependOpen(params Scene[] scenes) => PrependOpen(scenes.AsEnumerable());

        /// <summary>Specifies the scenes to close.</summary>
        /// <remarks>Can be called multiple times to add more scenes.</remarks>
        public SceneOperation Close(params Scene[] scenes) => Close(scenes.AsEnumerable());

        /// <summary>Specifies user callbacks.</summary>
        public SceneOperation Callback(params Callback[] callbacks) => Callback(callbacks.AsEnumerable());

        /// <summary>Specifies a scene to preload.</summary>
        /// <remarks>A scene specified to preload cannot also be added to open or close lists.</remarks>
        public SceneOperation Preload(Scene scene) => Set(() => m_preload = scene);

        /// <inheritdoc cref="Open(Scene[])"/>
        public SceneOperation Open(IEnumerable<Scene> scenes) => Set(() => m_open = m_open.Concat(scenes));
        public SceneOperation PrependOpen(IEnumerable<Scene> scenes) => Set(() => m_open = scenes.Concat(m_open));

        /// <inheritdoc cref="Close(Scene[])"/>
        public SceneOperation Close(IEnumerable<Scene> scenes) => Set(() => m_close = m_close.Concat(scenes));

        /// <inheritdoc cref="Callback(Core.Callback[])"/>
        public SceneOperation Callback(IEnumerable<Callback> callbacks) => Set(() => m_callbacks = m_callbacks.Concat(callbacks));


        //Loading

        /// <summary>Specifies loading screen to use.</summary>
        /// <remarks>Has no effect if <see cref="useLoadingScene"/> is <see langword="false"/>.</remarks>
        public SceneOperation With(Scene loadingScene) => Set(() => m_loadingScene = loadingScene);

        /// <summary>Specifies a callback when loading screen is opened, before <see cref="LoadingScreen.OnOpen"/> is called.</summary>
        public SceneOperation With(Action<LoadingScreen> loadingScreenCallback) => Set(() => m_loadingScreenCallback = loadingScreenCallback);

        /// <summary>Specifies whatever loading screen should be used.</summary>
        public SceneOperation DisableLoadingScreen(bool useLoadingScene = false) => Set(() => m_useLoadingScene = useLoadingScene);

        /// <summary>Specifies whatever loading screen should be used.</summary>
        public SceneOperation EnableLoadingScreen(bool useLoadingScene = true) => Set(() => m_useLoadingScene = useLoadingScene);

        /// <summary>Specifies the <see cref="ThreadPriority"/> to use.</summary>
        public SceneOperation With(ThreadPriority loadingPriority) => Set(() => m_loadingPriority = loadingPriority);

        //Options

        /// <summary>Specifies whatever <see cref="Resources.UnloadUnusedAssets"/> should be called at the end (before loading screen).</summary>
        public SceneOperation UnloadUsedAssets() => Set(() => m_unloadUnusedAssets = true);

        /// <summary>Specifies custom progress that will be counted as part of <see cref="progress"/>.</summary>
        public SceneOperation With(Progress<float> customProgress) => Set(() => m_customProgress = customProgress);

        //Convenience

        /// <summary>Closes all scenes prior to opening any scenes.</summary>
        public void CloseAll(params Scene[] except) =>
           Close(SceneManager.openScenes.Where(s => !except.Contains(s)));

        /// <summary>Closes all non-persistent scenes prior to opening any scenes.</summary>
        public void CloseAllNonPersistent(params Scene[] except) =>
           Close(SceneManager.openScenes.Where(s => !s.isPersistent && !except.Contains(s)));

        bool isFrozen;
        SceneOperation Set(Action action)
        {

            if (isFrozen)
                throw new InvalidOperationException("Cannot change properties when frozen.");

            action.Invoke();
            return this;

        }

        void Freeze() => isFrozen = true;

        #endregion

        #endregion
        #region Run

        bool hasRun;
        IEnumerator Run()
        {

            //Lets wait a bit so that users can change properties, we freeze after beforeStart callback
            yield return null;

            if (preload && (this.open.Contains(preload) || this.close.Contains(preload)))
            {
                Debug.LogError("A scene cannot be both preloaded and opened/closed in a single scene operation.");
                yield break;
            }

            if (IsDuplicate())
            {
                Debug.LogWarning("A duplicate scene operation was detected, it has been halted. This behavior can be changed in settings.");
                yield break;
            }

            beforeStart?.Invoke(this);
            if (wasCancelled)
                yield break;

            //Freeze properties so that they cannot be changed once started 
            Freeze();

            LogUtility.LogStart(this);

            FallbackSceneUtility.EnsureOpen();

            SetThreadPriority();

            SetupProgress();
            yield return ShowLoadingScreen();

            //Close scenes
            var close = this.close.NonNull().Distinct().ToArray();
            yield return DoCloseCallbacks(close);
            yield return DoActions(Phase.UnloadScenes, s => UnloadScene(s), close);

            //Open scenes
            var open = this.open.NonNull().Distinct().ToArray();
            yield return DoActions(Phase.LoadScenes, s => LoadScene(s, false), open);
            yield return DoActions(Phase.LoadScenes, s => LoadScene(s, true), preload);

            //Do before open callbacks
            if (!SceneManager.runtime.preloadedScene)
                yield return UnloadUnusedAssets();

            yield return DoOpenCallbacks(open);
            foreach (var operation in waitFor.ToArray())
                yield return operation;

            yield return HideLoadingScreen();
            UnsetupProgress();

            ResetThreadPriority();

            LogUtility.LogEnd(this);

            if (!SceneManager.openScenes.Any() && !QueueUtility<SceneOperation>.isBusy)
                SceneManager.runtime.OnAllScenesClosed();

            yield return null;
            hasRun = true;

        }

        #region Run actions

        IEnumerator DoActions(Phase phase, Func<Scene, IEnumerator> createAction, params Scene[] scenes)
        {

            yield return DoCoroutine(() => DoPhaseCallbacks(phase, When.Before));

            foreach (var scene in scenes)
            {

                if (wasCancelled)
                    yield break;

                yield return DoCoroutine(() => SceneCallbacks(scene, When.Before));
                yield return DoCoroutine(() => createAction.Invoke(scene), description: $"SceneOperation.Run({phase}:{(scene ? scene.name : "")}");
                yield return DoCoroutine(() => SceneCallbacks(scene, When.After));

            }

            yield return DoCoroutine(() => DoPhaseCallbacks(phase, When.After));

        }

        GlobalCoroutine coroutine;
        IEnumerator DoCoroutine(Func<IEnumerator> func, string description = "")
        {

            try
            {
                coroutine = func.Invoke().StartCoroutine(description: description);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            yield return coroutine;
            coroutine = null;

        }

        #endregion
        #region Scene load

        internal static Scene currentLoadingScene { get; private set; }
        internal static bool isCurrentLoadingScenePreload { get; private set; }

        IEnumerator LoadScene(Scene scene, bool isPreload)
        {

            if (!scene)
                yield break;

            if (scene.isOpen)
                yield break;

            currentLoadingScene = scene;
            isCurrentLoadingScenePreload = isPreload;

            var e = new SceneLoadArgs()
            {
                scene = scene,
                collection = collection,
                isPreload = isPreload,
            };

            yield return RunSceneLoader(e);
            if (Validate(e))
            {

                SceneManager.runtime.Track(scene);
                if (e.isPreload && e.preloadCallback is not null)
                    SceneManager.runtime.TrackPreload(scene, e.preloadCallback);

                scene.openedBy = collection;
                m_openedScenes.Add(scene);
                SetActiveScene();

            }

            currentLoadingScene = null;
            isCurrentLoadingScenePreload = false;

        }

        IEnumerator RunSceneLoader(SceneLoadArgs e)
        {

            var loader = e.scene.GetEffectiveSceneLoader();
            LogUtility.LogLoaded(loader, e);

            SetupProgress(e);
            yield return loader?.LoadScene(e.scene, e);
            OnDone(e);

        }

        bool Validate(SceneLoadArgs e)
        {

            if (e.isHandled)
            {

                if (e.scene.internalScene?.IsValid() ?? false)
                    return true;
                else if (e.isError)
                    Debug.LogError(e.errorMessage);
                else if (!e.noSceneWasLoaded)
                {
                    Debug.LogError($"Could not open scene due to unknown error:" + e.scene.path);
                }

                e.scene.internalScene = null;

            }
            else
                Debug.LogError("Could not find a scene loader to load the scene:\n" + e.scene.path);

            return false;

        }

        #endregion
        #region Scene unload

        IEnumerator UnloadScene(Scene scene)
        {

            if (!scene || !scene.internalScene.HasValue || !scene.internalScene.Value.IsValid())
                yield break;

            var e = new SceneUnloadArgs()
            {
                scene = scene,
                collection = collection
            };

            SetActiveScene(scene);
            yield return RunSceneLoader(e);
            if (Validate(e))
            {
                m_closedScenes.Add(scene);
                _ = SceneManager.runtime.Untrack(scene);
            }

        }

        IEnumerator RunSceneLoader(SceneUnloadArgs e)
        {

            var loader = e.scene.GetEffectiveSceneLoader();
            LogUtility.LogUnloaded(loader, e);

            SetupProgress(e);
            yield return loader?.UnloadScene(e.scene, e);
            OnDone(e);

        }

        bool Validate(SceneUnloadArgs e)
        {

            if (e.isHandled)
            {

                if (e.scene.internalScene?.isLoaded ?? false)
                {
                    Debug.LogError("Could not unload scene due too unknown error:\n" + e.scene.path);
                    return false;
                }

                return true;

            }
            else if (e.isError)
            {
                Debug.LogError(e.errorMessage);
                e.scene.internalScene = null;
            }
            else
                Debug.LogError("Could not find a scene loader to load the scene:\n" + e.scene.path);

            return false;
        }

        #endregion
        #region Callbacks

        readonly List<SceneOperation> waitFor = new();
        public void WaitFor(SceneOperation operation) =>
            waitFor.Add(operation);

        #region Extensibility

        public delegate void OnBeforeStart(SceneOperation operation);

        /// <summary>Occurs before operation has started working.</summary>
        public static event OnBeforeStart beforeStart;

        protected static readonly List<Callback> _extCallbacks = new();

        /// <summary>Adds the callback to every scene operation.</summary>
        public static void AddCallback(Callback callback)
        {
            _ = _extCallbacks.Remove(callback);
            _extCallbacks.Add(callback);
        }

        /// <summary>Removes a callback that was added to every scene operation.</summary>
        public static void RemoveCallback(Callback callback) =>
            _ = _extCallbacks.Remove(callback);

        #endregion

        bool doCallbacks => !wasCancelled && Application.isPlaying;

        IEnumerator DoCloseCallbacks(IEnumerable<Scene> scenes)
        {

            if (wasCancelled)
                yield break;

            yield return DoPhaseCallbacks(Phase.CloseCallbacks, When.Before);
            if (doCallbacks)
                yield return CallbackUtility.DoCollectionCloseCallbacks(SceneManager.openCollection);

            foreach (var scene in scenes)
            {
                yield return SceneCallbacks(scene, When.Before);
                if (doCallbacks)
                    yield return CallbackUtility.DoSceneCloseCallbacks(scene);
                yield return SceneCallbacks(scene, When.After);
            }

            yield return DoPhaseCallbacks(Phase.CloseCallbacks, When.After);

        }

        IEnumerator DoOpenCallbacks(IEnumerable<Scene> scenes)
        {

            if (wasCancelled)
                yield break;

            yield return DoPhaseCallbacks(Phase.OpenCallbacks, When.Before);

            foreach (var scene in scenes)
            {
                yield return SceneCallbacks(scene, When.Before);
                if (doCallbacks)
                    yield return CallbackUtility.DoSceneOpenCallbacks(scene);
                yield return SceneCallbacks(scene, When.After);
            }

            if (doCallbacks)
                yield return CallbackUtility.DoCollectionOpenCallbacks(collection);

            yield return DoPhaseCallbacks(Phase.OpenCallbacks, When.After);

        }

        readonly List<(Phase phase, When when)> callbacksRun = new();

        IEnumerator DoPhaseCallbacks(Phase phase, When when)
        {

            if (wasCancelled)
                yield break;

            var tuple = (phase, when);
            if (!callbacksRun.Contains(tuple))
            {
                callbacksRun.Add(tuple);
                this.phase = phase;
                yield return CallCallbacks(when, phase);
            }

        }

        IEnumerator SceneCallbacks(Scene scene, When when)
        {
            if (scene)
                yield return CallCallbacks(when, phase, scene);
        }

        IEnumerator LoadingScreenCallback(When when)
        {
            yield return CallCallbacks(when);
        }

        IEnumerator CallCallbacks(When when, Phase? phase = null, Scene scene = null)
        {
            yield return callbacks.Run(this, scene, phase, when);
            yield return _extCallbacks.Run(this, scene, phase, when);
        }

        #endregion
        #region Loading screen

        /// <summary>Gets the loading screen that was opened by this operation.</summary>
        public LoadingScreen openedLoadingScreen { get; private set; }

        IEnumerator ShowLoadingScreen()
        {

            if (useLoadingScene)
            {
                var async = LoadingScreenUtility.OpenLoadingScreen(this, Callback);
                yield return async;
                openedLoadingScreen = async.value;
            }

            yield return LoadingScreenCallback(When.Before);

            void Callback(LoadingScreen loadingScreen) =>
                loadingScreenCallback?.Invoke(loadingScreen);

        }

        IEnumerator HideLoadingScreen()
        {

            yield return LoadingScreenCallback(When.After);

            if (openedLoadingScreen)
            {
                yield return LoadingScreenUtility.CloseLoadingScreen(openedLoadingScreen);
                openedLoadingScreen = null;
            }

        }

        #endregion
        #region Active scene

        /// <summary>Attempts to set active scene.</summary>
        /// <param name="except">Specifies a scene that should not be activated.</param>
        void SetActiveScene(Scene except = null)
        {

            if (preload)
                return;

            var scene = focus;

            if (!scene)
            {
                if (setActiveCollectionScene && collection && collection.activeScene && collection.activeScene != except)
                    scene = collection.activeScene;
                else if (!SceneManager.runtime.activeScene || FallbackSceneUtility.IsFallbackScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene()))
                    SceneManager.runtime.SetActive(SceneManager.openScenes.Except(except).LastOrDefault());
            }

            SceneManager.runtime.SetActive(scene);

        }

        #endregion
        #region Unload unused assets

        IEnumerator UnloadUnusedAssets()
        {
            if (unloadUnusedAssets ?? false ||
                (collection && collection.unloadUnusedAssets) ||
                (!collection && Profile.current.unloadUnusedAssetsForStandalone))
                yield return Resources.UnloadUnusedAssets();
        }

        #endregion
        #region Thread priority

        void SetThreadPriority() =>
            SetThreadPriority(collection);

        internal SceneOperation SetThreadPriority(SceneCollection collection, bool ignoreQueueCheck = false)
        {

            //Set loading thread priority, if queued.
            //This property is global, and race conditions will occur if we allow non-queued operations to also set this

            if (!Profile.current || !Profile.current.enableChangingBackgroundLoadingPriority)
                return this;

            if (!QueueUtility<SceneOperation>.IsQueued(this) && !ignoreQueueCheck)
                return this;

            Application.backgroundLoadingPriority = GetPriority();

            return this;

            ThreadPriority GetPriority()
            {

                if (loadingPriority.HasValue)
                    return loadingPriority.Value;

                if (!collection)
                    return Profile.current.backgroundLoadingPriority;
                else
                {

                    if (collection.loadingPriority != CollectionLoadingThreadPriority.Auto)
                        return (ThreadPriority)collection.loadingPriority;
                    else
                    {

                        return LoadingScreenUtility.isAnyLoadingScreenOpen
                            ? ThreadPriority.Normal
                            : ThreadPriority.Low;

                    }

                }

            }

        }

        #endregion
        #region Progress

        /// <summary>Gets the current progress.</summary>
        public float progress => GetProgress();

        Dictionary<Scene, float> sceneProgress = new();
        float customProgressValue;

        int progressID;
        static readonly List<int> progressIds = new();

        float GetProgress()
        {

            var count = sceneProgress.Count;
            var sum = sceneProgress.Values.Sum();
            if (customProgress is not null)
            {
                count += 1;
                sum += customProgressValue;
            }

            return sceneProgress is not null
            ? sum / count
            : 0;

        }

        void SetupProgress()
        {

#if UNITY_EDITOR
            progressID = Progress.Start("Running operation", "Initializing...", Progress.Options.Sticky);
            progressIds.Add(progressID);
#endif

            sceneProgress = new();

            if (preload)
                sceneProgress.Set(preload, 0);

            foreach (var scene in open.ToArray())
                sceneProgress.Set(scene, 0);

            foreach (var scene in close.ToArray())
                sceneProgress.Set(scene, 0);

            if (customProgress is not null)
                customProgress.ProgressChanged += CustomProgress_ProgressChanged;

        }

        void CustomProgress_ProgressChanged(object sender, float value) =>
            customProgressValue = value;

        void UnsetupProgress()
        {

#if UNITY_EDITOR
            Progress.Remove(progressID);
#endif

            foreach (var scene in sceneProgress.Keys.ToArray())
                sceneProgress[scene] = 1;
            reportProgress?.Invoke(progress);

            if (customProgress is not null)
                customProgress.ProgressChanged -= CustomProgress_ProgressChanged;

        }

        void SetupProgress(SceneLoaderArgsBase e)
        {

            e.updateProgress = (progress) => OnProgress(e.scene, progress);

#if UNITY_EDITOR

            var description = "unloading: ";
            if (e is SceneLoadArgs e1)
                description = e1.isPreload ? "preloading: " : "loading: ";

            description += e.scene.name;
            Progress.Report(progressID, progress, description);

#endif

        }

        void OnProgress(Scene scene, float progress)
        {

            if (openedLoadingScreen)
                openedLoadingScreen.OnProgressChanged(progress);
            sceneProgress[scene] = progress;
            reportProgress?.Invoke(this.progress);

        }

        void OnDone(SceneLoaderArgsBase e) =>
            OnProgress(e.scene, 1);

        Action<float> reportProgress;

        /// <summary>Specifies a callback for when progress changes.</summary>
        /// <remarks>Only one callback can be registered, previous one will be replaced by <paramref name="progress"/>.</remarks>
        public SceneOperation ReportProgress(Action<float> progress) =>
            Set(() => reportProgress = progress);

        static void ClearProgress()
        {
#if UNITY_EDITOR
            if (!SceneManager.runtime.isBusy)
                foreach (var id in progressIds.ToArray())
                {
                    if (Progress.Exists(id))
                        Progress.Remove(id);
                    progressIds.Remove(id);
                }
#endif
        }

        #endregion

        #endregion

    }

}

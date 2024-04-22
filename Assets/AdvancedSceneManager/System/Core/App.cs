using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using Lazy.Utility;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Runtime.CompilerServices;
using AdvancedSceneManager.Models.Internal;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using AdvancedSceneManager.Editor.Utility;
#endif

namespace AdvancedSceneManager.Core
{

    /// <summary>Manages startup and quit.</summary>
    /// <remarks>Usage: <see cref="SceneManager.app"/>.</remarks>
    public sealed class App
    {

        #region Initialize

        [RuntimeInitializeOnLoadMethod]
        [InitializeInEditorMethod]
        static void OnLoad()
        {

            SceneManager.app.isStartupFinished = false;

            SceneManager.OnInitialized(() =>
            {

                if (!Application.isPlaying)
                    InitializeEditor();

                RunStartupProcess();

            });

        }

        static void RunStartupProcess()
        {

#if UNITY_EDITOR
            if (!SceneManager.settings.user.runStartupProcess)
            {
                foreach (var scene in SceneUtility.GetAllOpenUnityScenes())
                    if (SceneManager.assets.scenes.TryFind(scene.path, out var s))
                        SceneManager.runtime.Track(s, scene);
                return;
            }
#endif

            if (Application.isPlaying)
            {
                SceneManager.app.Reset();
                SceneManager.app.StartInternal();
            }

        }

        #region Editor initialization

        static void InitializeEditor()
        {

#if UNITY_EDITOR

            SetProfile();

            if (!Application.isBatchMode)
            {
                Install();
                CallbackUtility.Initialize();
                BuildUtility.Initialize();
            }

#endif

        }

#if UNITY_EDITOR

        static void SetProfile()
        {

            Profile.SetProfile(GetProfile(), updateBuildSettings: !Application.isBatchMode);

            static Profile GetProfile()
            {
                if (Application.isBatchMode) return Profile.buildProfile;
                else if (Profile.forceProfile) return Profile.forceProfile;
                else if (SceneManager.settings.user.activeProfile) return SceneManager.settings.user.activeProfile;
                else return Profile.defaultProfile;
            }

        }

#endif

        #endregion

        #endregion
        #region Install

#if UNITY_EDITOR

        const string pragma = "ADVANCED_SCENE_MANAGER";

        static void Install()
        {
            CheckExistingScenes();
            SetPragma(true);
        }

        internal static void SetPragma(bool enabled)
        {

            ScriptingDefineUtility.Set(pragma, enabled);

            if (SceneManager.settings.project.isFirstStart)
            {

                SceneManager.settings.project.isFirstStart = false;
                SceneManager.settings.project.Save();

                UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation(UnityEditor.Compilation.RequestScriptCompilationOptions.CleanBuildCache);

            }

        }

        static void CheckExistingScenes()
        {
            var fixedScenes = Assets.scenes.Where(s => s.CheckIfSpecialScene()).ToArray();
            foreach (var scene in fixedScenes)
                scene.Save();
        }

#endif

        #endregion
        #region Uninstall

#if UNITY_EDITOR

        internal static event Action onUninstall;

        internal static void Uninstall()
        {
            onUninstall?.Invoke();
            SetPragma(false);
        }

#endif

        #endregion
        #region Properties

        /// <summary>Gets whatever we're currently in build mode.</summary>
        /// <remarks>This is <see langword="true"/> when in build or when play button in scene manager window is pressed.</remarks>
        public bool isBuildMode
        {
#if UNITY_EDITOR
            get => SceneManager.settings.user.isBuildMode;
            private set
            {
                SceneManager.settings.user.isBuildMode = value;
                SceneManager.settings.user.Save();
            }
#else
            get => true;
#endif
        }

        /// <summary>Gets if startup process is finished.</summary>
        public bool isStartupFinished { get; private set; }

        /// <summary>An object that persists start properties across domain reload, which is needed when configurable enter play mode is set to reload domain on enter play mode.</summary>
        [Serializable]
        public class Props
        {

            /// <summary>Gets the default <see cref="Props"/>.</summary>
            /// <remarks>Cannot be called during <see cref="Object"/> constructor.</remarks>
            public static Props defaultProps { get; } = new();

            /// <summary>Creates a new props.</summary>
            public Props()
            { }

            /// <summary>Creates a new props, from the specified props, copying its values.</summary>
            public Props(Props props)
            {
                displaySplashScreen = props.displaySplashScreen;
                forceOpenAllScenesOnCollection = props.forceOpenAllScenesOnCollection;
                fadeColor = props.fadeColor;
                fadeOutDuration = props.fadeOutDuration;
                fadeInDuration = props.fadeInDuration;
                openCollection = props.openCollection;
                m_runStartupProcessWhenPlayingCollection = props.m_runStartupProcessWhenPlayingCollection;
                softSkipSplashScreen = props.softSkipSplashScreen;
            }

            /// <summary>Specifies whatever splash screen should open, but be skipped.</summary>
            /// <remarks>Used by ASMSplashScreen.</remarks>
            [NonSerialized] internal bool softSkipSplashScreen = false;

            /// <summary>Specifies whatever the splash screen should be played.</summary>
            public bool? displaySplashScreen = null;

            /// <summary>Specifies whatever all scenes on <see cref="openCollection"/> should be opened.</summary>
            public bool forceOpenAllScenesOnCollection = false;

            /// <summary>The color for the fade out.</summary>
            /// <remarks>Unity splash screen color will be used if <see langword="null"/>.</remarks>
            public Color? fadeColor;

            /// <summary>Specifies the duration for the fade out animation.</summary>
            public float fadeInDuration = 1f;

            /// <summary>Specifies the duration for the fade in animation.</summary>
            /// <remarks>This would normally be 0 during first startup, then on restart it would be > 0.</remarks>
            public float fadeOutDuration = 1f;

            [SerializeField] private bool? m_runStartupProcessWhenPlayingCollection;

            /// <summary>Specifies whatever startup process should run before <see cref="openCollection"/> is opened.</summary>
            public bool runStartupProcessWhenPlayingCollection
            {
#if UNITY_EDITOR
                get => m_runStartupProcessWhenPlayingCollection ?? SceneManager.settings.user.startupProcessOnCollectionPlay;
#else
                get => m_runStartupProcessWhenPlayingCollection ?? false;
#endif
                set => m_runStartupProcessWhenPlayingCollection = value;
            }

            /// <summary>Gets if startup process should run.</summary>
            public bool runStartupProcess =>
                openCollection
                ? runStartupProcessWhenPlayingCollection
                : true;

            /// <summary>Specifies a collection to be opened after startup process is done.</summary>
            public SceneCollection openCollection;

            /// <summary>Gets the effective fade animation color, uses <see cref="fadeColor"/> if specified, but falls back to <see cref="ProjectSettings.buildUnitySplashScreenColor"/>.</summary>
            public Color effectiveFadeColor => fadeColor ?? SceneManager.settings.project.buildUnitySplashScreenColor;

        }

        #endregion
        #region No profile warning

        void CheckProfile()
        {

#if !UNITY_EDITOR

            if (!Application.isPlaying)
                return;

            if (!SceneManager.settings.project)
                NoProfileWarning.Show("Could not find ASM settings!");
            else if (!SceneManager.settings.project.buildProfile)
                NoProfileWarning.Show("Could not find build profile!");

#endif

        }

        class NoProfileWarning : MonoBehaviour
        {

            static string text;
            public static void Show(string text)
            {
                Debug.LogError(text);
                NoProfileWarning.text = text;
                if (!Profile.current)
                    _ = SceneManager.runtime.AddToDontDestroyOnLoad<NoProfileWarning>();
            }

            void Start()
            {
                DontDestroyOnLoad(gameObject);
                Update();
            }

            void Update()
            {
                if (Profile.current)
                    Destroy(gameObject);
            }

            GUIContent content;
            GUIStyle style;
            void OnGUI()
            {

                content ??= new GUIContent(text);
                style ??= new GUIStyle(GUI.skin.label) { fontSize = 22 };

                var size = style.CalcSize(content);
                GUI.Label(new Rect((Screen.width / 2) - (size.x / 2), (Screen.height / 2) - (size.y / 2), size.x, size.y), content, style);

            }

        }

        #endregion
        #region Internal start

        void StartInternal()
        {

            CheckProfile();
            SetLoadingPriority();
            UnsetBuildModeOnEditMode();

            if (!Application.isPlaying)
                return;

            FallbackSceneUtility.EnsureOpen();

            if (isBuildMode)
                Start(SceneManager.settings.project.m_startProps);

        }

        void SetLoadingPriority()
        {

            if (SceneManager.profile && SceneManager.profile.enableChangingBackgroundLoadingPriority)
                Application.backgroundLoadingPriority = SceneManager.profile.backgroundLoadingPriority;

        }

        void UnsetBuildModeOnEditMode()
        {
#if UNITY_EDITOR

            EditorApplication.playModeStateChanged -= EditorApplication_playModeStateChanged;
            EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;

            void EditorApplication_playModeStateChanged(PlayModeStateChange state)
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                    isBuildMode = false;
            }

#endif
        }

        #endregion
        #region Start / Restart

        /// <inheritdoc cref="RestartInternal()"/>
        public void Restart(Props props = null) =>
            Start(props);

        /// <inheritdoc cref="RestartInternal()"/>
        public IEnumerator RestartAsync(Props props = null) =>
            StartAsync(props);

        /// <inheritdoc cref="RestartInternal()"/>
        public void Start(Props props = null)
        {

#if COROUTINES || !UNITY_EDITOR
            StartAsync(props).StartCoroutine(description: "ASM Startup");
#else

            if (Application.isPlaying)
                StartAsync(props).StartCoroutine(description: "ASM Startup");
            else
            {
                BeforeStart(ref props);
                props.fadeOutDuration = 0;
                EditorApplication.EnterPlaymode();
            }

#endif

        }

        /// <inheritdoc cref="RestartInternal()"/>
        public IEnumerator StartAsync(Props props = null)
        {

            BeforeStart(ref props);
            if (Application.isPlaying)
                yield return RestartInternal();
#if UNITY_EDITOR
            else
            {

                props.fadeOutDuration = 0;
                PrepareEnterPlayMode(out var cancel);
                if (!cancel)
                    EditorApplication.EnterPlaymode();

            }
#endif

        }

#if UNITY_EDITOR

        void PrepareEnterPlayMode(out bool cancel)
        {

            cancel = false;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                cancel = true;
                return;
            }

            var scenes = EditorSceneManager.GetSceneManagerSetup().Where(s => AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path)).ToArray();
            if (scenes.Length > 0)
                SceneManager.settings.user.sceneSetup = scenes;
            else
                SceneManager.settings.user.sceneSetup = null;
            SceneManager.settings.user.Save();

            FallbackSceneUtility.EnsureOpen();
            foreach (var scene in SceneUtility.GetAllOpenUnityScenes().Where(s => !FallbackSceneUtility.IsFallbackScene(s)))
                EditorSceneManager.CloseScene(scene, true);

        }

        [InitializeOnLoadMethod]
        static void OnPlayModeChanged() =>
            SceneManager.OnInitialized(() => EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged);

        static void EditorApplication_playModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {

                if (SceneManager.settings.user.sceneSetup == null)
                    return;

                var setup = SceneManager.settings.user.sceneSetup.OfType<SceneSetup>().Where(s => AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path)).ToArray();
                if (setup?.Length == 0 || !setup.Any(s => s.isActive))
                    return;

                EditorSceneManager.RestoreSceneManagerSetup(setup);

                SceneManager.settings.user.sceneSetup = null;
                SceneManager.settings.user.Save();

            }
        }

#endif

        void BeforeStart(ref Props props)
        {

            props ??= SceneManager.settings.project.m_startProps;

#if UNITY_EDITOR
            SceneManager.settings.user.runStartupProcess = true;
            isBuildMode = true;
#endif

#if !UNITY_EDITOR
            props.fadeOutDuration = 0;
#endif

            SceneManager.settings.project.m_startProps = props;
            SceneManager.settings.project.Save();

        }

        GlobalCoroutine coroutine;

        /// <summary>Starts ASM startup process.</summary>
        /// <remarks>There is no difference between Restart() and Start() methods.</remarks>
        IEnumerator RestartInternal()
        {
            coroutine?.Stop();
            coroutine = DoStartupProcess(SceneManager.settings.project.m_startProps).StartCoroutine(description: "ASM startup", onComplete: UnsetupProgress);
            yield return coroutine;
        }

        internal void CancelStartup() =>
            coroutine?.Stop();

        #endregion
        #region Startup process

        #region Progress

        //Fade
        //close all scenes
        //splash screen
        //startup loading screen
        //collections
        //scenes
        //collection
        //startup loading screen

        //Async
        //AsyncOperation
        //Scene operation

        readonly Dictionary<string, float> progress = new()
        {
            //{ nameof(FadeOut), 0f },
            { nameof(CloseAllScenes), 0f },
            { nameof(PlaySplashScreen), 0f },
            //{ nameof(FadeIn), 0f },
            { nameof(ShowStartupLoadingScreen), 0f },
            { nameof(OpenCollections), 0f },
            { nameof(OpenScenes), 0f },
            { nameof(OpenCollection), 0f },
            { nameof(HideStartupLoadingScreen), 0f },
        };

        void OnProgress(float value, [CallerMemberName] string name = "")
        {

            progress[name] = Mathf.Clamp01(value);

            if (loadingScreen)
                loadingScreen.OnProgressChanged(value);

#if UNITY_EDITOR
            Progress.Report(progressID, progress.Values.Sum() / progress.Count);
#endif

        }

        void OnDone([CallerMemberName] string name = "") =>
            OnProgress(1, name);

#if UNITY_EDITOR
        static int progressID;
#endif
        void SetupProgress()
        {
            UnsetupProgress();
#if UNITY_EDITOR
            progressID = Progress.Start("ASM Startup");
#endif
        }

        void UnsetupProgress()
        {

            foreach (var item in progress.Keys.ToArray())
                progress[item] = 0;

#if UNITY_EDITOR
            if (Progress.Exists(progressID))
                Progress.Remove(progressID);
            progressID = -1;
#endif

        }

        #endregion

        public bool IsRestart { get; private set; }

        /// <summary>Occurs before restart process has begun, but has been initiated.</summary>
        public event Action beforeRestart;

        /// <summary>Occurs after restart has been completed.</summary>
        public event Action afterRestart;

        LoadingScreen loadingScreen;

        internal Props currentProps;
        IEnumerator DoStartupProcess(Props props)
        {

            //Fixes issue where first scene cannot be opened when user are not using configurable enter play mode
            yield return null;

            currentProps = props;
            IsRestart = isStartupFinished;
            isStartupFinished = false;

#if UNITY_EDITOR

            LogUtility.LogStartupBegin();
            if (!SceneManager.profile)
            {
                Debug.LogError("No profile set.");
                yield break;
            }

#endif

            QueueUtility<SceneOperation>.StopAll();
            beforeRestart?.Invoke();

            SetupProgress();

            CreateCamera(props);
            yield return CloseAllScenes(props);
            yield return PlaySplashScreen(props);
            yield return ShowStartupLoadingScreen(props);
            DestroyCamera(props);

            yield return OpenScenes(props);
            yield return OpenCollections(props);
            yield return OpenCollection(props);
            yield return OpenScenes(props);

            yield return HideStartupLoadingScreen(props);
            UnsetupProgress();

            if (!SceneManager.openScenes.Any())
                Debug.LogError("No scenes opened during startup.");

#if UNITY_EDITOR
            SceneManager.settings.user.runStartupProcess = false;
#endif

            isStartupFinished = true;
            SceneManager.settings.project.m_startProps.displaySplashScreen = null;
            SceneManager.settings.project.Save();

            afterRestart?.Invoke();
            LogUtility.LogStartupEnd();

            currentProps = null;

        }

        GameObject camera;
        void CreateCamera(Props props)
        {
            if (Profile.current.createCameraDuringStartup && !Object.FindFirstObjectByType<Camera>())
                if (SceneManager.runtime.AddToDontDestroyOnLoad<Camera>(out var c, out camera))
                {
                    c.backgroundColor = props.effectiveFadeColor;
                    c.clearFlags = CameraClearFlags.SolidColor;
                }
        }

        IEnumerator CloseAllScenes(Props _)
        {

            SceneManager.runtime.Reset();

            var scenes = SceneUtility.GetAllOpenUnityScenes().
                Where(s => !FallbackSceneUtility.IsFallbackScene(s)).
                Where(s => !Profile.current.startupScene || Profile.current.startupScene.name != s.name).
                Where(s => s.IsValid()).
                ToArray();

            var progress = scenes.ToDictionary(s => s, s => 0f);

            if (scenes.Length > 0)
                foreach (var scene in scenes)
                {

                    FallbackSceneUtility.EnsureOpen();
                    yield return null;

                    if (scene.IsValid() && !FallbackSceneUtility.IsFallbackScene(scene))
                    {

#if UNITY_EDITOR
                        if (SceneImportUtility.StringExtensions.IsTestScene(scene.path))
                            continue;
#endif

                        yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene).WithProgress(value =>
                        {
                            progress[scene] = value;
                            OnProgress(Mathf.Clamp01(progress.Values.Sum() / scenes.Length));
                        });

                    }

                }

            OnDone();

        }

        IEnumerator PlaySplashScreen(Props props)
        {

            if (props.runStartupProcess && (props.displaySplashScreen ?? !IsRestart) && Profile.current && Profile.current.splashScreen)
            {

                var async = LoadingScreenUtility.OpenLoadingScreen<SplashScreen>(Profile.current.splashScreen);
                yield return async;

                if (async.value)
                {
                    _ = UnityEngine.SceneManagement.SceneManager.SetActiveScene(async.value.gameObject.scene);
                    yield return LoadingScreenUtility.CloseLoadingScreen(async.value, (f) => OnProgress(f), false);
                    yield return ShowStartupLoadingScreen(props);
                    if (async.value && async.value.ASMScene(out var scene))
                        yield return LoadingScreenUtility.CloseLoadingScreenScene(scene);
                }

            }

            OnDone();

        }

        IEnumerator ShowStartupLoadingScreen(Props props)
        {

            if (loadingScreen)
                yield break;

            var async = LoadingScreenUtility.OpenLoadingScreen(
                Profile.current.startupLoadingScreen,
                callbackBeforeBegin: l =>
                {
                    if (l is IFadeLoadingScreen f)
                    {
                        f.color = props.effectiveFadeColor;
                        f.fadeDuration = 0;
                    }
                },
                progress: f => OnProgress(f));

            yield return async;
            loadingScreen = async.value;

            OnDone();

        }

        void DestroyCamera(Props _)
        {
            if (camera)
                Object.Destroy(camera);
        }

        IEnumerator OpenCollections(Props props)
        {

            if (props.runStartupProcess)
            {

                var collections = Profile.current.startupCollections.ToArray();
                var progress = collections.ToDictionary(c => c, c => 0f);

                if (collections.Length > 0)
                    foreach (var collection in collections)
                        yield return collection.Open().DisableLoadingScreen().ReportProgress(f =>
                        {
                            progress[collection] = f;
                            OnProgress(progress.Values.Sum() / progress.Count);
                        });

            }

            OnDone();

        }

        IEnumerator OpenScenes(Props props)
        {

            var scenes = Profile.current.startupScenes;
            var progress = scenes.ToDictionary(c => c, c => 0f);

            foreach (var scene in scenes)
                yield return scene.Open().ReportProgress(f => progress[scene] = f);

            OnDone();

        }

        IEnumerator OpenCollection(Props props)
        {

            var collection = props.openCollection;
            if (collection)
                yield return collection.Open().DisableLoadingScreen().ReportProgress(f => OnProgress(f));

            OnDone();

        }

        IEnumerator HideStartupLoadingScreen(Props _)
        {
            yield return LoadingScreenUtility.CloseLoadingScreen(loadingScreen, f => OnProgress(f));
            OnDone();
        }

        #endregion
        #region Quit

        #region Callbacks

        readonly List<IEnumerator> callbacks = new();

        /// <summary>Register a callback to be called before quit.</summary>
        public void RegisterQuitCallback(IEnumerator coroutine) => callbacks.Add(coroutine);

        /// <summary>Unregister a callback that was to be called before quit.</summary>
        public void UnregisterQuitCallback(IEnumerator coroutine) => callbacks.Remove(coroutine);

        IEnumerator CallSceneCloseCallbacks()
        {
            yield return CallbackUtility.Invoke<ISceneClose>().OnAllOpenScenes();
        }

        IEnumerator CallCollectionCloseCallbacks()
        {
            if (SceneManager.openCollection)
                yield return CallbackUtility.Invoke<ICollectionClose>().WithParam(SceneManager.openCollection).OnAllOpenScenes();
        }

        #endregion

        internal void Reset()
        {
            isQuitting = false;
            cancelQuit = false;
        }

        /// <summary>Gets whatever ASM is currently in the process of quitting.</summary>
        public bool isQuitting { get; private set; }

        bool cancelQuit;

        /// <summary>Cancels a quit in progress.</summary>
        /// <remarks>Only usable during a <see cref="RegisterQuitCallback(IEnumerator)"/> or while <see cref="isQuitting"/> is true.</remarks>
        public void CancelQuit()
        {
            if (isQuitting)
                cancelQuit = true;
        }

        /// <summary>Quits the game, and calls quitCallbacks, optionally with a fade animation.</summary>
        /// <param name="fade">Specifies whatever screen should fade out.</param>
        /// <param name="fadeColor">Defaults to <see cref="ProjectSettings.buildUnitySplashScreenColor"/>.</param>
        /// <param name="fadeDuration">Specifies the duration of the fade out.</param>
        public void Quit(bool fade = true, Color? fadeColor = null, float fadeDuration = 1)
        {

            Coroutine().StartCoroutine();
            IEnumerator Coroutine()
            {

                QueueUtility<SceneOperation>.StopAll();

                isQuitting = true;
                cancelQuit = false;

                var wait = new List<IEnumerator>();

                var async = LoadingScreenUtility.FadeOut(fadeDuration, fadeColor);
                yield return async;
                wait.Add(new WaitForSecondsRealtime(0.5f));

                wait.AddRange(callbacks);
                wait.Add(CallCollectionCloseCallbacks());
                wait.Add(CallSceneCloseCallbacks());

                yield return wait.WaitAll(isCancelled: () => cancelQuit);

                if (cancelQuit)
                {
                    cancelQuit = false;
                    isQuitting = false;
                    if (async?.value)
                        yield return LoadingScreenUtility.CloseLoadingScreen(async.value);
                    yield break;
                }

#if UNITY_EDITOR
                EditorApplication.ExitPlaymode();
#else
                Application.Quit();
#endif

            }

        }

        #endregion

#if UNITY_EDITOR
        internal bool isInstalled =>
            AssetDatabase.FindAssets("t:asmdef").
            Select(AssetDatabase.GUIDToAssetPath).
            Any(path => path.EndsWith("AdvancedSceneManager.asmdef"));
#endif

    }

    #region Uninstall

#if UNITY_EDITOR

    class UninstalledChecker : AssetModificationProcessor
    {

        static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions _)
        {

            if (path.Contains("/AdvancedSceneManager/"))
            {

                if (AssetDatabase.IsValidFolder(path))
                {
                    var assemblyPaths = AssetDatabase.FindAssets("t:asmdef", new[] { path }).Select(AssetDatabase.GUIDToAssetPath);
                    assemblyPaths.ForEach(CheckPath);
                }
                else
                    CheckPath(path);

            }

            static void CheckPath(string path)
            {
                if (path.EndsWith("AdvancedSceneManager/AdvancedSceneManager.asmdef"))
                    App.Uninstall();
            }

            return AssetDeleteResult.DidNotDelete;

        }

    }

#endif

    #endregion

}

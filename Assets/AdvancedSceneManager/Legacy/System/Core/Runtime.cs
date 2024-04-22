#pragma warning disable IDE0051 // Remove unused private members

using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using AdvancedSceneManager.Core.Actions;

using static AdvancedSceneManager.SceneManager;
using System.Linq;
using AdvancedSceneManager.Utility;
using System;
using AdvancedSceneManager.Models;
using Lazy.Utility;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace AdvancedSceneManager.Core
{

    /// <summary>Manages the start and quit processes of the game.</summary>
    public class Runtime
    {

        /// <summary>Gets whatever ASM is done with startup process.</summary>
        public bool isInitialized { get; internal set; }

        /// <summary>Occurs before startup process is started, or when <see cref="Restart"/> is called.</summary>
        public event Action beforeStart;

        /// <summary>Occurs after startup process is done, or when <see cref="Restart"/> is called.</summary>
        public event Action afterStart;

        #region InitializeOnLoad

        internal static void Initialize()
        {

            SceneManager.runtime.wasStartedAsBuild = false;
            SetProfile();
            SceneManager.Initialize();

            if (profile && profile.enableChangingBackgroundLoadingPriority)
                Application.backgroundLoadingPriority = profile.backgroundLoadingPriority;

#if UNITY_EDITOR

            EditorApplication.playModeStateChanged -= EditorApplication_playModeStateChanged;
            EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;

            void EditorApplication_playModeStateChanged(PlayModeStateChange state)
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                {
                    runtime.isBuildMode = false;
                    RestoreSceneSetup();
                }
            }

#endif

            standalone.OnLoad();
            if (!Application.isPlaying)
                return;

            DefaultSceneUtility.EnsureOpen();

            if (runtime.isBuildMode)
                runtime.DoStart(SceneManager.settings.project.m_startProps);
            else
                runtime.isInitialized = true;

        }

        #endregion
        #region Start

        [Serializable]
        internal struct StartProps
        {

            /// <summary>Gets the default <see cref="StartProps"/>.</summary>
            /// <remarks>Cannot be called during <see cref="UnityEngine.Object"/> constructor.</remarks>
            public static StartProps GetDefault() =>
                new StartProps()
                {
                    skipSplashScreen = false,
                    ignoreDoNotOpen = false,
                    fadeColor = SceneManager.settings.project && SceneManager.settings.project ? SceneManager.settings.project.buildUnitySplashScreenColor : Color.black,
                    initialFadeDuration = 0,
                    beforeSplashScreenFadeDuration = 1f,
                    m_overrideOpenCollection = ""
                };

            public bool skipSplashScreen;
            public bool ignoreDoNotOpen;
            public Color fadeColor;
            public float initialFadeDuration;
            public float beforeSplashScreenFadeDuration;

#pragma warning disable CS0414
            [SerializeField] private string m_overrideOpenCollection;
#pragma warning restore CS0414

            public SceneCollection overrideOpenCollection
            {
                get
                {
#if UNITY_EDITOR
                    return AssetDatabase.LoadAssetAtPath<SceneCollection>(m_overrideOpenCollection);
#else
                    return null;
#endif
                }
                set
                {
#if UNITY_EDITOR
                    m_overrideOpenCollection = AssetDatabase.GetAssetPath(value);
#else
                    return;
#endif
                }
            }

        }

        /// <summary>Gets whatever we're currently in build mode.</summary>
        /// <remarks>This is true when in build or when play button in scene manager window is pressed.</remarks>
        public bool isBuildMode
        {
#if UNITY_EDITOR
            get => SceneManager.settings.local.isBuildMode;
            private set
            {
                settings.local.isBuildMode = value;
                settings.local.Save();
            }
#else
            get => true;
#endif
        }

        /// <summary>Gets if game was started as a build.</summary>
        public bool wasStartedAsBuild { get; private set; }

        /// <summary>Starts startup sequence.</summary>
        /// <param name="quickStart">Skips splash screen if <see langword="true"/>.</param>
        /// <param name="collection">Opens the collection after all other collections and scenes flagged to open has.</param>
        /// <remarks>Enters playmode if in editor.</remarks>
        public void Start(SceneCollection collection = null, bool ignoreDoNotOpen = false, bool playSplashScreen = true) =>
            Start(skipSplashScreen: !playSplashScreen, SceneManager.settings.project.buildUnitySplashScreenColor, 0, 1f, overrideOpenCollection: collection, ignoreDoNotOpen);

        /// <summary>Restarts game and plays startup sequence again.</summary>
        /// <remarks>Enters playmode if in editor.</remarks>
        public void Restart(bool playSplashScreen = false)
        {
            var props = SceneManager.settings.project.m_startProps;
            Start(skipSplashScreen: !playSplashScreen, props.fadeColor, props.initialFadeDuration, props.beforeSplashScreenFadeDuration, overrideOpenCollection: props.overrideOpenCollection, props.ignoreDoNotOpen);
        }

        void Start(bool skipSplashScreen, Color fadeColor, float initialFadeDuration, float beforeSplashScreenFadeDuration, SceneCollection overrideOpenCollection, bool ignoreDoNotOpen)
        {

            SceneManager.settings.project.m_startProps = new StartProps()
            {
                skipSplashScreen = skipSplashScreen,
                fadeColor = fadeColor,
                initialFadeDuration = initialFadeDuration,
                beforeSplashScreenFadeDuration = beforeSplashScreenFadeDuration,
                overrideOpenCollection = overrideOpenCollection,
                ignoreDoNotOpen = ignoreDoNotOpen
            };
            SceneManager.settings.project.Save();

            if (Application.isPlaying)
                DoStart(SceneManager.settings.project.m_startProps, force: true);
            else
            {

#if UNITY_EDITOR

                Coroutine().StartCoroutine();
                return;

                IEnumerator Coroutine()
                {

                    //Prevents a long delay in-between edit and play mode, that is otherwise avoided by just not pressing play button during, or directly after recompile
                    while (EditorApplication.isCompiling)
                        yield return null;

                    if (settings.local.saveActionWhenUsingASMPlayButton == ASMSettings.Local.SaveAction.Save)
                        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                    else if (settings.local.saveActionWhenUsingASMPlayButton == ASMSettings.Local.SaveAction.Prompt && !UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        yield break;

                    isBuildMode = true;

                    yield return SaveSceneSetup();

                    EditorApplication.EnterPlaymode();
                    wasStartedAsBuild = true;

                }

#endif
            }

        }

        //DoStart() is called from OnLoad()
        void DoStart(StartProps props, bool force = false)
        {

            wasStartedAsBuild = true;

#if UNITY_EDITOR


            if (!profile)
            {
                Debug.LogError("No profile set!");
                return;
            }

            if (!force && !SceneUtility.isStartupScene)
                return;

#endif

            var skipSplashScreen = props.skipSplashScreen || !Profile.current || !Profile.current.splashScreen;

            QueueUtility<SceneOperation>.StopAll();
            SceneManager.collection.Clear();
            SceneManager.standalone.Clear();

            ActionUtility.Try(() => beforeStart?.Invoke());
            _ = SceneOperation.Add(null).
               WithAction(new StartupAction(skipSplashScreen: skipSplashScreen, props.fadeColor, props.initialFadeDuration, props.beforeSplashScreenFadeDuration, props.overrideOpenCollection, props.ignoreDoNotOpen)).
               WithCallback(Callback.BeforeLoadingScreenClose().Do(() =>
               {
                   runtime.isInitialized = true;
                   ActionUtility.Try(() => afterStart?.Invoke());
               }));

        }

        static void SetProfile()
        {

#if !UNITY_EDITOR

            if (!Profile.current && Application.isPlaying)
            {
                Debug.LogError("No build profile set!");
                NoProfileWarning.Show();
            }

#endif

        }

        #region No profile warning

        class NoProfileWarning : MonoBehaviour
        {

            public static void Show()
            {
                if (Profile.current)
                    return;
                _ = utility.AddToDontDestroyOnLoad<NoProfileWarning>();
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

                if (content == null)
                    content = new GUIContent("No active profile");

                if (style == null)
                    style = new GUIStyle(GUI.skin.label) { fontSize = 22 };

                var size = style.CalcSize(content);
                GUI.Label(new Rect((Screen.width / 2) - (size.x / 2), (Screen.height / 2) - (size.y / 2), size.x, size.y), content, style);

            }

        }

        #endregion

        #endregion
        #region Scene setup

#if UNITY_EDITOR

        [Serializable]
        internal class Setup
        {

            public Setup(params SceneSetup[] scenes) =>
                this.scenes = scenes;

            public SceneSetup[] scenes;

        }

        static Setup sceneSetup
        {
            get => settings.local.sceneSetup;
            set
            {
                settings.local.sceneSetup = value;
                settings.local.Save();
            }
        }

        static IEnumerator SaveSceneSetup()
        {

            if (EditorSceneManager.GetSceneManagerSetup().Any())
                sceneSetup = new Setup(EditorSceneManager.GetSceneManagerSetup());

            _ = EditorSceneManager.OpenScene(DefaultSceneUtility.GetStartupScene(), OpenSceneMode.Single);

            yield return new WaitForSeconds(0.1f);

        }

        static void RestoreSceneSetup()
        {

            var setup = sceneSetup;

            if (setup?.scenes != null)
                setup.scenes = setup.scenes.Where(s => AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path)).ToArray();

            if ((setup?.scenes?.Any() ?? false))
                EditorSceneManager.RestoreSceneManagerSetup(setup.scenes);

            sceneSetup = null;

        }

#endif

        #endregion
        #region Quit

        internal readonly List<IEnumerator> quitCallbacks = new List<IEnumerator>();

        /// <summary>Register a callback to be called before quit.</summary>
        public void RegisterQuitCallback(IEnumerator courutine) =>
            quitCallbacks.Add(courutine);

        /// <summary>Unregister a callback that was to be called before quit.</summary>
        public void UnregisterQuitCallback(IEnumerator courutine) =>
            quitCallbacks.Remove(courutine);

        /// <inheritdoc cref="QuitAction.CancelQuit"/>
        public void CancelQuit() =>
            QuitAction.CancelQuit();

        /// <inheritdoc cref="QuitAction.isQuitting"/>
        public bool isQuitting =>
            QuitAction.isQuitting;

        /// <summary>Quits the game, and calls quitCallbacks, optionally with a fade animation.</summary>
        public void Quit(bool fade = true, Color? fadeColor = null, float fadeDuration = 1) =>
            SceneOperation.Add(null).
                WithAction(new QuitAction(fade, fadeColor, fadeDuration));

        #endregion

    }

}

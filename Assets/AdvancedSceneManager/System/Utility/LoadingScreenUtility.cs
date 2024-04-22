using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Core;
using Lazy.Utility;
using UnityEngine;
using static AdvancedSceneManager.SceneManager;
using scene = UnityEngine.SceneManagement.Scene;
using Scene = AdvancedSceneManager.Models.Scene;

namespace AdvancedSceneManager.Utility
{

    /// <summary>Used to pass arguments from <see cref="LoadingScreenUtility.FadeIn(LoadingScreen, float, Color?)"/></summary>
    public interface IFadeLoadingScreen
    {
        /// <summary>Specifies the fade duration.</summary>
        float fadeDuration { get; set; }
        /// <summary>Specifies the color of the fade.</summary>
        Color color { get; set; }
    }

    /// <summary>Manager for loading screens.</summary>
    public static class LoadingScreenUtility
    {

        #region Methods

        /// <summary>Gets if this scene is a loading screen.</summary>
        public static bool IsLoadingScreenOpen(Scene scene) =>
            m_loadingScreens.Any(l => scene && l && l.gameObject && (scene.path == l.gameObject.scene.path));

        /// <summary>Gets if any loading screens are open.</summary>
        public static bool isAnyLoadingScreenOpen =>
            loadingScreens.Where(l => l && l.gameObject).Any();

        static Scene GetLoadingScreen(SceneOperation operation)
        {
            if (operation?.loadingScene)
                return operation.loadingScene;
            else if (operation?.collection && operation.collection.effectiveLoadingScreen)
                return operation.collection.effectiveLoadingScreen;
            else
                return null;
        }

        public static Async<LoadingScreen> OpenLoadingScreen(SceneOperation operation, Action<LoadingScreen> callbackBeforeBegin = null) =>
            OpenLoadingScreen(GetLoadingScreen(operation), operation, callbackBeforeBegin);

        public static Async<LoadingScreen> OpenLoadingScreen(Scene loadingScene, SceneOperation operation = null, Action<LoadingScreen> callbackBeforeBegin = null, Action<float> progress = null) =>
            OpenLoadingScreen<LoadingScreen>(loadingScene, operation, callbackBeforeBegin, progress);

        public static Async<T> OpenLoadingScreen<T>(SceneOperation operation, Action<T> callbackBeforeBegin = null, Action<float> progress = null) where T : LoadingScreenBase =>
            OpenLoadingScreen(GetLoadingScreen(operation), operation, callbackBeforeBegin, progress);

        /// <summary>Shows a loading screen.</summary>
        public static Async<T> OpenLoadingScreen<T>(Scene loadingScene, SceneOperation operation = null, Action<T> callbackBeforeBegin = null, Action<float> progress = null) where T : LoadingScreenBase
        {

            if (!loadingScene)
                return Async<T>.complete;

            T value = default;
            return new(Coroutine().StartCoroutine(description: $"OpenLoadingScreen: {loadingScene.name}"), () => value);

            IEnumerator Coroutine()
            {

                var loadingScreenOperation = SceneOperation.Start().Open(loadingScene).ReportProgress(progress);
                loadingScreenOperation.isLoadingScreen = true;

                yield return loadingScreenOperation;

                if (!loadingScene.internalScene.HasValue || !loadingScene.internalScene.Value.IsValid())
                    yield return OnError($"Loaded scene was not valid.");
                else if (!loadingScene.FindObject<T>(out var loadingScreen))
                    yield return OnError($"No {typeof(T).Name} script could be found in '{loadingScene.name}.'");
                else
                {

                    if (loadingScreen is LoadingScreen l && l)
                        l.operation = operation;

                    Add(loadingScreen);
                    callbackBeforeBegin?.Invoke(loadingScreen);
                    yield return loadingScreen.OnOpen();
                    value = loadingScreen;

                }

                IEnumerator OnError(string message)
                {
                    Debug.LogError(message);
                    yield return SceneOperation.Start().Close(loadingScene);
                }

            }

        }

        /// <inheritdoc cref="CloseLoadingScreen(LoadingScreenBase, Action{float})"/>
        public static IEnumerator CloseLoadingScreen(Scene scene) =>
            CloseLoadingScreen(loadingScreens.LastOrDefault(l => scene.Equals(l.ASMScene())));

        /// <summary>Hide the loading screen.</summary>
        /// <param name="loadingScreen">The loading screen to hide.</param>
        /// <param name="progress">The callback to receive progress.</param>
        /// <param name="closeScene">Specifies whatever the scene should be closed afterwards. Use <see cref="CloseLoadingScreenScene(Scene, Action{float})"/> if <see langword="false"/>.</param>
        public static IEnumerator CloseLoadingScreen(LoadingScreenBase loadingScreen, Action<float> progress = null, bool closeScene = true)
        {

            if (!loadingScreen)
                yield break;

            yield return loadingScreen.OnClose();
            Remove(loadingScreen);

            if (closeScene && loadingScreen.ASMScene(out var scene))
                yield return CloseLoadingScreenScene(scene, progress);

        }

        /// <summary>Close the scene that contained a loading screen.</summary>
        public static IEnumerator CloseLoadingScreenScene(Scene scene, Action<float> progress = null)
        {
            var operation = SceneOperation.Start().Close(scene).ReportProgress(progress);
            operation.isLoadingScreen = true;
            yield return operation;
        }

        /// <summary>Hide all loading screens.</summary>
        public static IEnumerator CloseAll()
        {
            foreach (var loadingScreen in loadingScreens.ToArray())
                yield return CloseLoadingScreen(loadingScreen);
        }

        #endregion
        #region DoAction utility

        #region Fade

        /// <summary>Finds the default fade loading screen. Will be null if not included in build.</summary>
        public static Scene fade => SceneManager.assets.defaults.fadeScreen;

        /// <summary>Fades out the screen.</summary>
        public static Async<LoadingScreen> FadeOut(float duration = 1, Color? color = null, Action<float> progress = null) =>
            OpenLoadingScreen<LoadingScreen>(fade, null, callbackBeforeBegin: l => SetFadeProps(l, duration, color), progress);

        /// <summary>Fades in the screen.</summary>
        public static IEnumerator FadeIn(LoadingScreenBase loadingScreen, float duration = 1, Color? color = null, Action<float> progress = null)
        {
            SetFadeProps(loadingScreen, duration, color);
            return CloseLoadingScreen(loadingScreen, progress);
        }

        static void SetFadeProps(LoadingScreenBase loadingScreen, float duration, Color? color)
        {
            if (loadingScreen is IFadeLoadingScreen fade)
            {
                fade.fadeDuration = duration;
                fade.color = color ?? Color.black;
            }
        }

        #endregion

        /// <inheritdoc cref="DoAction(Scene, Func{IEnumerator}, Action{LoadingScreenBase})"/>
        public static IEnumerator DoAction(Scene scene, Action action, Action<LoadingScreenBase> loadingScreenCallback = null) =>
            DoAction(scene, coroutine: RunAction(action), loadingScreenCallback);

        /// <summary>Opens loading screen, performs action and hides loading screen again.</summary>
        /// <param name="scene">The loading screen scene.</param>
        /// <param name="coroutine">To coroutine to execute.</param>
        /// <param name="loadingScreenCallback">The callback to perform when loading script is loaded, but before ASM has called <see cref="LoadingScreenBase.OnOpen()"/>.</param>
        public static IEnumerator DoAction(Scene scene, Func<IEnumerator> coroutine, Action<LoadingScreenBase> loadingScreenCallback = null)
        {

            if (scene)
                yield return
                    SceneOperation.Start().
                    With(scene).
                    With(loadingScreenCallback).
                    Callback(coroutine);

        }

        static Func<IEnumerator> RunAction(Action action)
        {
            return () => Run();
            IEnumerator Run()
            {
                action?.Invoke();
                yield break;
            }
        }

        #endregion
        #region List over open loading screens

        /// <summary>Gets the current default loading screen.</summary>
        public static Scene defaultLoadingScreen =>
            profile ? profile.loadingScreen : null;

        static readonly List<LoadingScreenBase> m_loadingScreens = new();

        /// <summary>The currently open loading screens.</summary>
        public static IEnumerable<LoadingScreenBase> loadingScreens => m_loadingScreens;

        static void Add(LoadingScreenBase loadingScreen)
        {

            if (!loadingScreen.ASMScene(out var scene))
                return;

            m_loadingScreens.Add(loadingScreen);
            loadingScreen.canvas.PutOnTop();
            loadingScreen.onDestroy += Remove;

        }

        static void Remove(LoadingScreenBase loadingScreen)
        {

            if (!loadingScreen.ASMScene(out var scene))
                return;

            CanvasSortOrderUtility.Remove(loadingScreen.canvas);
            _ = m_loadingScreens.Remove(loadingScreen);
            _ = m_loadingScreens.RemoveAll(l => !l);

        }

        #endregion

#if UNITY_EDITOR
        internal static DateTime? lastRefresh;
        internal static void RefreshSpecialScenes()
        {
            if (lastRefresh.HasValue && (DateTime.Now - lastRefresh.Value).TotalSeconds < 1)
                return;
            lastRefresh = DateTime.Now;
            foreach (var scene in SceneManager.assets.scenes.Where(s => s.CheckIfSpecialScene()))
                scene.Save();
        }
#endif

        /// <summary>Returns a coroutine that returns when <see cref="AsyncOperation.isDone"/> becomes <see langword="true"/>. <paramref name="onProgress"/> will be called every frame with <see cref="AsyncOperation.progress"/>.</summary>
        public static IEnumerator WithProgress(this AsyncOperation asyncOperation, Action<float> onProgress)
        {

            if (asyncOperation == null)
                yield break;

            while (!IsDone())
            {
                onProgress(asyncOperation.progress);
                yield return null;
            }

            yield return null;

            bool IsDone() =>
                (asyncOperation.isDone || Mathf.Approximately(asyncOperation.progress, 1f)) ||
                (!asyncOperation.allowSceneActivation && Mathf.Approximately(asyncOperation.progress, 0.9f));

        }

        /// <summary>Sets <see cref="AsyncOperation.allowSceneActivation"/> to <see langword="false"/>.</summary>
        public static AsyncOperation Preload(this AsyncOperation asyncOperation, out Func<IEnumerator> activateCallback)
        {

            asyncOperation.allowSceneActivation = false;
            activateCallback = Activate;

            return asyncOperation;

            IEnumerator Activate()
            {
                asyncOperation.allowSceneActivation = true;
                yield return asyncOperation;
            }

        }

    }

}

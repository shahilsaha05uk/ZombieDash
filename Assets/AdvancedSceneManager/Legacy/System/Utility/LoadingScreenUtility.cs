using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Core;
using AdvancedSceneManager.Core.Actions;
using AdvancedSceneManager.Exceptions;
using AdvancedSceneManager.Models;
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
        public static bool IsLoadingScreenOpen(scene scene) =>
            m_loadingScreens.Any(l => l && scene == l.gameObject.scene);

        /// <summary>Gets if this scene is a loading screen.</summary>
        public static bool IsLoadingScreenOpen(Scene scene) =>
            m_loadingScreens.Any(l => scene && l && l.gameObject && (scene.path == l.gameObject.scene.path));

        /// <summary>Gets if this scene is a loading screen.</summary>
        public static bool IsLoadingScreenOpen(OpenSceneInfo scene) =>
            m_loadingScreens.Any(l => l && l.gameObject && (scene?.path == l.gameObject.scene.path));

        /// <summary>Gets if any loading screens are open.</summary>
        public static bool IsAnyLoadingScreenOpen =>
            loadingScreens.Where(l => l).Any();

        /// <summary>Shows the loading screen associated with this collection.</summary>
        public static SceneOperation<LoadingScreenBase> OpenLoadingScreen(SceneCollection collection, Action<LoadingScreenBase> callbackBeforeBegin = null)
        {

            if (!collection)
                return SceneOperation<LoadingScreenBase>.done;
            else if (collection.loadingScreenUsage == LoadingScreenUsage.DoNotUse)
                return SceneOperation<LoadingScreenBase>.done;
            else if (FindLoadingScreen(collection) is Scene scene)
                return OpenLoadingScreen(scene, callbackBeforeBegin);

            return SceneOperation<LoadingScreenBase>.done;

        }

        /// <summary>Shows a loading screen.</summary>
        /// <param name="typeName">Overrides the name of the type in the missing script message.</param>
        public static SceneOperation<LoadingScreenBase> OpenLoadingScreen(Scene scene, Action<LoadingScreenBase> callbackBeforeBegin = null, string typeName = null) =>
            OpenLoadingScreen<LoadingScreenBase>(scene, callbackBeforeBegin, typeName);

        /// <summary>Shows a loading screen.</summary>
        /// <param name="typeName">Overrides the name of the type in the missing script message.</param>
        public static SceneOperation<T> OpenLoadingScreen<T>(Scene scene, Action<T> callbackBeforeBegin = null, string typeName = null) where T : LoadingScreenBase
        {

            if (!scene)
                return SceneOperation<T>.done;

            var action = new OpenAndRunCallbackAction<T>(scene, (l) =>
            {
                callbackBeforeBegin?.Invoke(l);
                Add(l);
                return l.OnOpen();
            },
            isLoadingScreen: true,
            onMissingCallback: () => Debug.LogError($"No {typeName ?? typeof(T).Name} script could be found in '{scene.name}.'"));

            return SceneOperation.Add(standalone, @return: o => action.callback, ignoreQueue: true).
                WithAction(action).
                WithFriendlyText("OpenLoadingScreen<" + typeof(T).Name + ">").FlagAsLoadingScreen();

        }

        /// <inheritdoc cref="CloseLoadingScreen(LoadingScreen)"/>
        public static SceneOperation CloseLoadingScreen(Scene scene) =>
            CloseLoadingScreen(loadingScreens.LastOrDefault(l => scene.Equals(l.Scene())));

        /// <summary>Hide the loading screen.</summary>
        public static SceneOperation CloseLoadingScreen(LoadingScreenBase loadingScreen)
        {

            if (!loadingScreen)
                return SceneOperation.done;

            Remove(loadingScreen);
            var action = new RunCallbackAndCloseAction<LoadingScreenBase>(loadingScreen, (l) => l.OnClose(), isLoadingScreen: true);
            return SceneOperation.Add(standalone, ignoreQueue: true).
                WithAction(action).
                WithFriendlyText("CloseLoadingScreen<" + loadingScreen.GetType().Name + ">");

        }

        /// <summary>Hide all loading screens.</summary>
        public static SceneOperation CloseAll()
        {

            if (!m_loadingScreens.Any())
                return SceneOperation.done;

            var actions = m_loadingScreens.Select(loadingScreen => new RunCallbackAndCloseAction<LoadingScreenBase>(loadingScreen, (l) => l.OnClose(), isLoadingScreen: true));

            return SceneOperation.Add(standalone, ignoreQueue: true).
                WithAction(actions.ToArray()).
                WithCallback(Callback.BeforeLoadingScreenClose().Do(m_loadingScreens.Clear));

        }

        /// <summary>Find the loading screen that is associated with this collection.</summary>
        public static Scene FindLoadingScreen(SceneCollection collection)
        {

            if (!collection)
                return null;

            if (collection.loadingScreenUsage == LoadingScreenUsage.Override && collection.loadingScreen)
                return collection.loadingScreen;
            else if (collection.loadingScreenUsage != LoadingScreenUsage.DoNotUse && Profile.current && Profile.current.loadingScreen)
                return Profile.current.loadingScreen;

            return null;

        }

        #endregion
        #region DoAction utility

        #region Fade

        const string path = "Assets/AdvancedSceneManager/Defaults/Loading Screen/Fade/Fade Loading Screen.unity";

        /// <summary>Finds the default fade loading screen. Will be null if not included in build.</summary>
        public static Scene fade =>
            assets.allScenes.FirstOrDefault(s => s && (s.path?.EndsWith(path) ?? false));

        /// <summary>Fades out the screen.</summary>
        public static SceneOperation<LoadingScreen> FadeOut(float duration = 1, Color? color = null) =>
            OpenLoadingScreen<LoadingScreen>(fade, callbackBeforeBegin: l => SetFadeProps(l, duration, color));

        /// <summary>Fades in the screen.</summary>
        public static SceneOperation FadeIn(LoadingScreen loadingScreen, float duration = 1, Color? color = null)
        {
            SetFadeProps(loadingScreen, duration, color);
            return CloseLoadingScreen(loadingScreen);
        }

        static void SetFadeProps(LoadingScreen loadingScreen, float duration, Color? color)
        {
            if (loadingScreen is IFadeLoadingScreen fade)
            {
                fade.fadeDuration = duration;
                fade.color = color ?? Color.black;
            }
        }

        #endregion

        /// <inheritdoc cref="DoAction(Scene, Func{IEnumerator}, Action{LoadingScreen})"/>
        public static SceneOperation DoAction(Scene scene, Action action, Action<LoadingScreen> loadingScreenCallback = null) =>
            DoAction(scene, coroutine: RunAction(action), loadingScreenCallback);

        /// <summary>Opens loading screen, performs action and hides loading screen again.</summary>
        /// <param name="scene">The loading screen scene.</param>
        /// <param name="action">The action to perform.</param>
        /// <param name="loadingScreenCallback">The callback to perform when loading script is loaded, but before ASM has called <see cref="LoadingScreen.OnOpen(SceneOperation)"/>.</param>
        /// <remarks>Throws <see cref="OpenSceneException"/> if <paramref name="scene"/> is null.</remarks>
        public static SceneOperation DoAction(Scene scene, Func<IEnumerator> coroutine, Action<LoadingScreen> loadingScreenCallback = null)
        {

            if (!scene)
                throw new OpenSceneException(scene, message: "Scene was null");

            var operation = SceneOperation.Add(standalone, ignoreQueue: true).
                WithLoadingScreen(scene).
                WithLoadingScreenCallback(loadingScreenCallback).
                WithCallback(coroutine);

            return operation;

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

        static readonly List<LoadingScreenBase> m_loadingScreens = new List<LoadingScreenBase>();

        /// <summary>The currently open loading screens.</summary>
        public static IEnumerable<LoadingScreenBase> loadingScreens => m_loadingScreens;

        static void Add(LoadingScreenBase loadingScreen)
        {

            if (!loadingScreen || !(loadingScreen.Scene()?.unityScene.HasValue ?? false))
                return;

            PersistentUtility.Set(loadingScreen.Scene().unityScene.Value, SceneCloseBehavior.KeepOpenAlways);
            m_loadingScreens.Add(loadingScreen);
            loadingScreen.canvas.PutOnTop();
            loadingScreen.onDestroy += Remove;

        }

        static void Remove(LoadingScreenBase loadingScreen)
        {

            var scene = loadingScreen ? loadingScreen.Scene() : null;
            if (scene?.unityScene.HasValue ?? fade)
                return;

            PersistentUtility.Unset(scene.unityScene.Value);
            _ = m_loadingScreens.Remove(loadingScreen);
            _ = m_loadingScreens.RemoveAll(l => !l);
            CanvasSortOrderUtility.Remove(loadingScreen.canvas);

        }

        #endregion

        /// <summary>Returns a coroutine that returns when <see cref="AsyncOperation.isDone"/> becomes <see langword="true"/>. <paramref name="onProgress"/> will be called every frame with <see cref="AsyncOperation.progress"/>.</summary>
        public static IEnumerator WithProgress(this AsyncOperation asyncOperation, Action<float> onProgress)
        {

            if (asyncOperation == null)
                yield break;

            while (!(asyncOperation.isDone || (!asyncOperation.allowSceneActivation && asyncOperation.progress < 0.9f)))
            {
                onProgress(asyncOperation.progress);
                yield return null;
            }

            yield return null;

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

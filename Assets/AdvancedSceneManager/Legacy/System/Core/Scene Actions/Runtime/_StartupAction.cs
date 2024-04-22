using System;
using System.Collections;
using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AdvancedSceneManager.Core.Actions
{

    /// <summary>
    /// Performs startup sequence, see <see cref="Runtime.Start"/>:
    /// <para><see cref="FadeOut(float, Color?)(float)"/>.</para>
    /// <para><see cref="CloseAllUnityScenesAction"/>.</para>
    /// <para><see cref="PlaySplashScreenAction"/> (+ fade in).</para>
    /// <para><see cref="ShowStartupLoadingScreen"/>.</para>
    /// <para><see cref="OpenStartupCollections"/>.</para>
    /// <para><see cref="HideStartupLoadingScreen"/>.</para>
    /// </summary>
    public class StartupAction : AggregateAction
    {

        ///<inheritdoc cref="StartupAction"/>
        /// <param name="fadeColor">Defaults to unity splash screen color.</param>
        public StartupAction(bool skipSplashScreen = false, Color? fadeColor = null, float initialFadeDuration = 0, float beforeSplashScreenFadeDuration = 0.5f, SceneCollection collection = null, bool forceOpenAllScenes = false) :
            base(

                new CallbackAction(CreateCamera),
                skipSplashScreen ? null : new CallbackAction(() => FadeOut(initialFadeDuration, fadeColor)),
                new CloseAllUnityScenesAction(),
                skipSplashScreen ? null : new PlaySplashScreenAction(() => FadeIn(beforeSplashScreenFadeDuration)),

                new CallbackAction(ShowStartupLoadingScreen),
                new CallbackAction(DestroyCamera),
                new OpenStartupCollections(collection, forceOpenAllScenes),
                new CallbackAction(HideStartupLoadingScreen)

                )
        { }

        static SceneOperation<LoadingScreen> fade;
        static SceneOperation<LoadingScreen> loadingScreen;

        static IEnumerator FadeOut(float duration, Color? fadeColor)
        {
            if (Profile.current && LoadingScreenUtility.fade && LoadingScreenUtility.fade.isIncluded)
                yield return fade = LoadingScreenUtility.FadeOut(duration, color: fadeColor ?? SceneManager.settings.project.buildUnitySplashScreenColor);
        }

        static IEnumerator FadeIn(float duration) =>
            LoadingScreenUtility.FadeIn(fade?.value, duration);

        static IEnumerator ShowStartupLoadingScreen()
        {
            if (Profile.current)
            {
                yield return loadingScreen =
                    LoadingScreenUtility.OpenLoadingScreen<LoadingScreen>(
                        Profile.current.startupLoadingScreen,
                        callbackBeforeBegin: l => l.operation = SceneManager.utility.currentOperation);
            }
        }

        static IEnumerator HideStartupLoadingScreen() =>
            LoadingScreenUtility.CloseLoadingScreen(loadingScreen?.value);

        static Camera camera;
        static void CreateCamera()
        {
            if (!Profile.current.createCameraDuringStartup)
                return;
            if (!camera)
            {
                camera = SceneManager.utility.AddToDontDestroyOnLoad<Camera>();
                camera.backgroundColor = SceneManager.settings.project.buildUnitySplashScreenColor;
                camera.clearFlags = CameraClearFlags.SolidColor;
            }
        }

        static void DestroyCamera()
        {
            if (camera)
            {
                var t = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity​Engine.​RenderPipelines.Universal.Runtime", throwOnError: false);
                if (t != null && camera.gameObject.GetComponent(t) is Component c && c)
                    Object.Destroy(c);
                Object.Destroy(camera);
            }
        }

    }

}

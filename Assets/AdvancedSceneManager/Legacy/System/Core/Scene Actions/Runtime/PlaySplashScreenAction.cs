using System;
using System.Collections;
using System.Linq;
using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;

namespace AdvancedSceneManager.Core.Actions
{

    /// <summary>Plays splash screen.</summary>
    public class PlaySplashScreenAction : SceneAction
    {

        public override bool reportsProgress => false;

        public PlaySplashScreenAction(Func<IEnumerator> hideInitialLoadingScreen) =>
            this.hideInitialLoadingScreen = hideInitialLoadingScreen;

        readonly Func<IEnumerator> hideInitialLoadingScreen;

        public override IEnumerator DoAction(SceneManagerBase _sceneManager)
        {

            if (!Profile.current)
                yield break;

            if (Profile.current.splashScreen)
            {

                SceneOperation<SplashScreen> async;
                yield return async =
                    LoadingScreenUtility.OpenLoadingScreen<SplashScreen>(Profile.current.splashScreen, typeName: "SplashScreen").
                    SetParent(SceneManager.utility.currentOperation);

                yield return hideInitialLoadingScreen?.Invoke();

                if (async.value)
                {
                    _ = UnityEngine.SceneManagement.SceneManager.SetActiveScene(async.value.gameObject.scene);
                    yield return LoadingScreenUtility.CloseLoadingScreen(async.value).SetParent(SceneManager.utility.currentOperation);
                }
                else if (async.openedScenes.FirstOrDefault() is OpenSceneInfo scene)
                    yield return scene.scene.Close();

            }

        }

    }

}

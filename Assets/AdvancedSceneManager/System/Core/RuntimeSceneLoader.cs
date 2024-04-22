using System.Collections;
using AdvancedSceneManager.Utility;
using UnityEngine.SceneManagement;
using Scene = AdvancedSceneManager.Models.Scene;
using sceneManager = UnityEngine.SceneManagement.SceneManager;

namespace AdvancedSceneManager.Core
{

    class RuntimeSceneLoader : SceneLoader
    {

        public override bool CanOpen(Scene scene) =>
            scene.isIncluded;

        public override IEnumerator LoadScene(Scene scene, SceneLoadArgs e)
        {

            if (e.isPreload)
            {

                yield return sceneManager.LoadSceneAsync(scene.path, LoadSceneMode.Additive).Preload(out var activateCallback).WithProgress(e.ReportProgress);

                var openedScene = e.GetOpenedScene();
                while (!openedScene.IsValid())
                {
                    openedScene = e.GetOpenedScene();
                    yield return null;
                }

                e.SetCompleted(openedScene, activateCallback);

            }
            else
            {

                yield return sceneManager.LoadSceneAsync(scene.path, LoadSceneMode.Additive).WithProgress(e.ReportProgress);

                var openedScene = e.GetOpenedScene();
                while (!openedScene.IsValid())
                {
                    openedScene = e.GetOpenedScene();
                    yield return null;
                }

                e.SetCompleted(openedScene);
                yield return null;

            }

        }

        public override IEnumerator UnloadScene(Scene scene, SceneUnloadArgs e)
        {
            yield return sceneManager.UnloadSceneAsync(scene.internalScene.Value).WithProgress(e.ReportProgress);
            e.SetCompleted();
        }

    }

}

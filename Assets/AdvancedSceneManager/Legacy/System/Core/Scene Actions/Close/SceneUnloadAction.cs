using System.Collections;
using System.Linq;
using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using sceneManager = UnityEngine.SceneManagement.SceneManager;

namespace AdvancedSceneManager.Core.Actions
{

    public sealed class SceneUnloadAction : SceneAction
    {

        LazyOpenScene targetScene;
        public SceneUnloadAction(LazyOpenScene scene, SceneCollection collection = null)
        {
            targetScene = scene;
            this.collection = collection;
            if (!targetScene)
                Done();
        }

        public override IEnumerator DoAction(SceneManagerBase _sceneManager)
        {

            var targetScene = (OpenSceneInfo)this.targetScene;

            if (targetScene == null || !targetScene.unityScene.HasValue || !targetScene.unityScene.Value.IsValid())
                yield break;

            var e = new SceneUnloadOverrideArgs()
            {
                scene = targetScene.scene,
                unityScene = targetScene.unityScene.Value,
                collection = collection,
                updateProgress = OnProgress
            };

            if (SceneManager.utility.sceneUnloadOverride != null)
                yield return SceneManager.utility.sceneUnloadOverride?.Invoke(e);

            if (!e.isHandled)
                yield return UnloadSceneAsync(e);

            if (targetScene != null)
            {
                UnsetPersistentFlag(targetScene);
                Remove(targetScene, SceneManager.standalone);
                Remove(targetScene, SceneManager.collection);
            }

            Done(unityScene);

        }

        static IEnumerator UnloadSceneAsync(SceneUnloadOverrideArgs e)
        {
            yield return sceneManager.UnloadSceneAsync(e.unityScene).WithProgress(e.ReportProgress);
            e.SetCompleted();
        }

        public void UnsetPersistentFlag(OpenSceneInfo scene) =>
            PersistentUtility.Unset(scene.unityScene.Value);

        public void Remove(OpenSceneInfo scene, SceneManagerBase sceneManager)
        {
            if (sceneManager.openScenes.Contains(scene))
            {
                sceneManager.Remove(scene);
                sceneManager.RaiseSceneClosed(scene);
                scene.OnSceneClosed();
            }
        }

    }

}

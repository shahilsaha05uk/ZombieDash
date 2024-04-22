using System.Collections;
using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Models;

namespace AdvancedSceneManager.Core.Actions
{

    /// <summary>Calls all <see cref="ISceneOpen.OnSceneOpen"/> callbacks in scene.</summary>
    public sealed class SceneOpenCallbackAction : SceneAction
    {

        LazyOpenScene targetScene;
        public SceneOpenCallbackAction(LazyOpenScene scene, SceneCollection collection = null)
        {
            targetScene = scene;
            this.collection = collection;
        }

        public override IEnumerator DoAction(SceneManagerBase _sceneManager)
        {
            if (targetScene.ToOpenSceneInfo() is OpenSceneInfo scene)
                yield return CallbackUtility.DoSceneOpenCallbacks(scene);
            Done();
        }

    }

}

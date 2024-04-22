using System.Collections;
using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Models;

namespace AdvancedSceneManager.Core.Actions
{

    /// <summary>Calls all <see cref="ISceneClose.OnSceneClose"/> callbacks in scene.</summary>
    public sealed class SceneCloseCallbackAction : SceneAction
    {

        LazyOpenScene targetScene;
        /// <summary>Creates a new instance of <see cref="SceneCloseCallbackAction"/>.</summary>
        public SceneCloseCallbackAction(LazyOpenScene scene, SceneCollection collection = null)
        {
            targetScene = scene;
            this.collection = collection;
        }

        /// <inheritdoc/>
        public override IEnumerator DoAction(SceneManagerBase _sceneManager)
        {

            if (targetScene)
                yield return CallbackUtility.DoSceneCloseCallbacks(targetScene);
            Done();

        }

    }

}

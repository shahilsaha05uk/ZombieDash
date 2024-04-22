using System;
using System.Collections;
using System.Linq;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;

namespace AdvancedSceneManager.Core.Actions
{

    /// <summary>Closes all scenes, except <see cref="DefaultSceneUtility"/>, regardless of whatever they are tracked or not. This is used in <see cref="StartupAction"/>, where we cannot be sure scenes are tracked yet.</summary>
    public class CloseAllUnityScenesAction : SceneAction
    {

        public override bool reportsProgress => false;

        public CloseAllUnityScenesAction()
        { }

        public CloseAllUnityScenesAction(Func<UnityEngine.SceneManagement.Scene> ignore) =>
            this.ignore = ignore;

        readonly Func<UnityEngine.SceneManagement.Scene> ignore;

        public override IEnumerator DoAction(SceneManagerBase _sceneManager)
        {

            var ignore = this.ignore?.Invoke();

            var scenes = SceneUtility.GetAllOpenUnityScenes().
                Where(s => !ignore.HasValue || scene.path != ignore.Value.path).
                Where(s => !DefaultSceneUtility.IsDefaultScene(s)).
                Where(s => !Profile.current.startupScene || Profile.current.startupScene.path != s.path).
                Where(s => s.IsValid()).
                ToArray();

            foreach (var scene in scenes)
            {
                DefaultSceneUtility.EnsureOpen();
                if (scene.IsValid())
                    yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene).WithProgress(OnProgress);
            }

            SceneManager.collection.SetNull();
            SceneManager.collection.Clear();
            SceneManager.standalone.Clear();

        }

    }

}

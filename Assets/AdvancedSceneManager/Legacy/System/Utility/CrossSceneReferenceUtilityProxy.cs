using System;
using UnityEngine.SceneManagement;

namespace AdvancedSceneManager.Utility
{

    internal static class CrossSceneReferenceUtilityProxy
    {

        //Used to call plugin.asm.cross-scene-references from plugin.asm.locking

        public static event Action<Scene> clearScene;
        public static void ClearScene(Scene scene) =>
            clearScene?.Invoke(scene);

    }

}
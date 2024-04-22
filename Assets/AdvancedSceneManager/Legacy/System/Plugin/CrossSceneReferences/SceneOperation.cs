#if ASM_PLUGIN_CROSS_SCENE_REFERENCES

using AdvancedSceneManager.Core;

namespace AdvancedSceneManager.Plugin.Cross_Scene_References
{

    static class SceneOperation
    {

        internal static void Initialize() =>
            Core.SceneOperation.AddCallback(sceneOperationCallback);

        static readonly Callback sceneOperationCallback = Callback.Before(Phase.OpenCallbacks).Do(CrossSceneReferenceUtility.ResolveAllScenes);

    }

}
#endif
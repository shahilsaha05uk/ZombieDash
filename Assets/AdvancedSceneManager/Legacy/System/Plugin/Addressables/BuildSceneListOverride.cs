#if ASM_PLUGIN_ADDRESSABLES && UNITY_EDITOR

using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Utility;
using UnityEditor;

namespace AdvancedSceneManager.Plugin.Addressables.Editor
{

    [InitializeOnLoad]
    static class BuildSceneListOverride
    {

        static BuildSceneListOverride() =>
            EditorApplication.delayCall += () =>
                BuildUtility.Extensibility.Add(isIncluded: Override);

        static void Override(BuildUtility.Extensibility.CallbackEventArgs e)
        {
            if (SceneManager.assets.allScenes.TryFind(e.scene, out var scene) && scene.IsAddressable())
                e.SetValue(false);
        }

        internal static void ResetBuildListBeforeDisable() =>
            BuildUtility.Extensibility.Remove(isIncluded: Override);

    }

}
#endif

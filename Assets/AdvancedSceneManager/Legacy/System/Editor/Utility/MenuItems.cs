using UnityEditor;

namespace AdvancedSceneManager.Editor.Utility
{

    static class MenuItems
    {

        [MenuItem("Tools/Advanced Scene Manager/Window/Scene overview", priority = 41)]
        static void SceneOverview() => SceneOverviewWindow.Open();

        [MenuItem("Tools/Advanced Scene Manager/Window/Callback analyzer", priority = 53)]
        static void CallbackUtility() => Callbacks.CallbackUtility.Open();

    }

}

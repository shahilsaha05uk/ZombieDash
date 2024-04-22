#if UNITY_EDITOR

using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdvancedSceneManager.Editor.Utility
{

    static class DrawCollectionOnScenesInHierarchy
    {

        static bool isInitialized;
        internal static void Initialize()
        {
            if (isInitialized)
                return;
            isInitialized = true;
            HierarchyGUIUtility.AddSceneGUI(OnGUI);
        }

        static bool OnGUI(Scene scene)
        {

            if (!SceneManager.settings.local.displayCollectionTitleOnScenesInHierarchy)
                return false;

            if (!Application.isPlaying)
                return false;

            if (!SceneManager.collection.current)
                return false;

            if (!(SceneManager.collection.current.scenes.FirstOrDefault(s1 => s1 && s1.path == scene.path) is Models.Scene s && s))
                return false;

            GUILayout.Label(SceneManager.collection.current.title, HierarchyGUIUtility.defaultStyle, GUILayout.ExpandWidth(false));
            return true;

        }

    }

}
#endif

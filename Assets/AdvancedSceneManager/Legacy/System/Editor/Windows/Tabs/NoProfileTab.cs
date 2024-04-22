using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.Window
{

    static class NoProfileTab
    {

        public static void OnEnable(VisualElement element)
        {

            if (Profile.current)
            {
                SceneManagerWindow.RestoreTab();
                SceneManagerWindow.Reload();
            }
            else
            {
                element.Clear();
                element.Add(SettingsTab.CurrentProfileField(() => OnEnable(element)).SetStyle(e => e.style.SetMargin(22)));
            }

        }

    }

}

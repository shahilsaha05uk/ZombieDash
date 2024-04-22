using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    partial class SceneManagerWindow
    {

        class NetworkPage : SettingsPage
        {

            public override string Header => "Network";

            public override void OnCreateGUI(VisualElement element)
            {
                element.BindToSettings();
                element.Q("toggle-sync-indicator").BindToUserSettings();
            }

        }

    }

}

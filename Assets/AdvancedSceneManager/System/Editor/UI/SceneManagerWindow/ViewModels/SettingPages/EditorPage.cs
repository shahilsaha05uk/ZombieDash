using AdvancedSceneManager.Editor.Utility;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    partial class SceneManagerWindow
    {

        class EditorPage : SettingsPage
        {

            public override string Header => "Editor";

            public override void OnCreateGUI(VisualElement container)
            {
                container.BindToUserSettings();
                SetupProfiles();
                SetupLegacyCheck();
                SetupLocking();
                SetupToolbarButton();
            }

            void SetupProfiles()
            {

                var profileForce = element.Q<ObjectField>("profile-force");
                var profileDefault = element.Q<ObjectField>("profile-default");

                profileForce.RegisterValueChangedCallback(e => profileDefault.SetEnabled(!profileForce.value));

                element.Q("group-profiles").BindToSettings();
                element.Q("group-profiles").Query<PropertyField>().ForEach(e => e.SetEnabled(true));

            }

            void SetupLegacyCheck()
            {
                element.Q("toggle-legacy-mode-check").BindToSettings();
            }

            void SetupLocking()
            {
                element.Q("group-locking").BindToSettings();
                element.Q<Toggle>("toggle-scene-lock").RegisterValueChangedCallback(e => HierarchyGUIUtility.Repaint());
            }

            void SetupToolbarButton()
            {

                var groupInstalled = element.Q("group-toolbar").Q("group-installed");
                var groupNotInstalled = element.Q("group-toolbar").Q("group-not-installed");

#if TOOLBAR_EXTENDER

                groupInstalled.SetVisible(true);
                groupNotInstalled.SetVisible(false);

                Setup(element.Q("slider-toolbar-button-offset"));
                Setup(element.Q("slider-toolbar-button-count"));

                void Setup(VisualElement element)
                {
                    element.SetVisible(true);
                    element.Q("unity-drag-container").RegisterCallback<PointerMoveEvent>(e =>
                    {
                        if (e.pressedButtons == 1)
                            Utility.ToolbarButton.Repaint();
                    });
                }

#endif

            }

        }

    }

}

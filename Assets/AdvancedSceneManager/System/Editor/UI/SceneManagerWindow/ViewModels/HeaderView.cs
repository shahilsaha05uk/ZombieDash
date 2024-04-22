using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    partial class SceneManagerWindow
    {

        class HeaderView : ViewModel
        {

            public override void OnCreateGUI(VisualElement element)
            {

                element.Q<Button>("button-overview").clicked += window.popups.Open<OverviewPopup>;
                element.Q<Button>("button-menu").clicked += window.popups.Open<MenuPopup>;

                SetupPlayButton(element);
                SetupSettingsButton(element);

            }

            void SetupPlayButton(VisualElement element)
            {

                var button = element.Q<Button>("button-play");
                button.clickable.activators.Add(new() { button = MouseButton.LeftMouse, modifiers = UnityEngine.EventModifiers.Shift });
                element.Q<Button>("button-play").clickable.clickedWithEventInfo += (e) =>
                {
                    if (e is PointerUpEvent ev)
                        SceneManager.app.Start(new() { forceOpenAllScenesOnCollection = ev.shiftKey || ev.commandKey });
#if UNITY_2021 || UNITY_2022
                    if (e is MouseUpEvent ev1)
                        SceneManager.app.Start(new() { forceOpenAllScenesOnCollection = ev1.shiftKey || ev1.commandKey });
#endif
                };

                window.BindEnabledToProfile(button);

            }

            void SetupSettingsButton(VisualElement element)
            {

                var button = element.Q<Button>("button-settings");
                button.clicked += window.popups.Open<SettingsPopup>;
                window.BindEnabledToProfile(button);

            }

            public override void ApplyAppearanceSettings(VisualElement element)
            {
                element.Q<Button>("button-search").SetVisible(SceneManager.settings.user.displaySearchButton);
                element.Q<Button>("button-overview").SetVisible(SceneManager.settings.user.displayOverviewButton);
            }
        }

    }

}

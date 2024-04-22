using System.Linq;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Models.Internal;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    partial class SceneManagerWindow
    {

        class StartupPage : SettingsPage
        {

            public override string Header => "Startup";

            public override void OnCreateGUI(VisualElement element)
            {

                element.Bind(Profile.serializedObject);

                element.Q<DropdownField>("dropdown-splash-scene").
                    SetupSceneDropdown(
                    getScenes: () => Assets.scenes.Where(s => s.isSplashScreen),
                    getValue: () => SceneManager.settings.profile.splashScreen,
                    setValue: (s) =>
                    {
                        if (Profile.current)
                        {
                            Profile.current.splashScreen = s;
                            Profile.current.Save();
                        }
                    });

                element.Q<DropdownField>("dropdown-loading-scene").
                    SetupSceneDropdown(
                    getScenes: () => Assets.scenes.Where(s => s.isLoadingScreen),
                    getValue: () => SceneManager.settings.profile.startupLoadingScreen,
                    setValue: (s) =>
                    {
                        SceneManager.settings.profile.startupLoadingScreen = s;
                        SceneManager.settings.profile.Save();
                    });

            }

        }

    }

}

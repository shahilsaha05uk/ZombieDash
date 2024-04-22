using System.Linq;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models.Internal;
using AdvancedSceneManager.Utility.CrossSceneReferences;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    partial class SceneManagerWindow
    {

        class SceneLoadingPage : SettingsPage
        {

            public override string Header => "Scene loading";

            public override void OnCreateGUI(VisualElement element)
            {

                element.Q("section-profile").BindToProfile();
                element.Q("section-project-settings").BindToSettings();
                element.Q<Toggle>("toggle-enable-cross-scene-references").RegisterValueChangedCallback(e => CrossSceneReferenceUtility.Initialize());

                element.Q<DropdownField>("dropdown-loading-scene").
                    SetupSceneDropdown(
                    getScenes: () => Assets.scenes.Where(s => s.isLoadingScreen),
                    getValue: () => SceneManager.settings.profile.loadingScreen,
                    setValue: (s) =>
                    {
                        SceneManager.settings.profile.loadingScreen = s;
                        SceneManager.settings.profile.Save();
                    });

            }

        }

    }

}

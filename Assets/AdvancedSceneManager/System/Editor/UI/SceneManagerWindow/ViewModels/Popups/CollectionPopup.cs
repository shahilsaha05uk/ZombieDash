using System.Linq;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Models.Enums;
using AdvancedSceneManager.Models.Internal;
using AdvancedSceneManager.Utility;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    partial class SceneManagerWindow
    {

        [SerializeField] private SceneCollection m_collectionPopup_collection;

        class CollectionPopup : ViewModel, IPopup
        {

            SceneCollection collection;

            public async void OnOpen(VisualElement element, object parameter)
            {

                if (!IsParameterValid(parameter, out collection))
                {
                    await window.popups.Close();
                    return;
                }

                element.Bind(new(collection));

                SetupSceneLoaderToggles();
                SetupLoadingOptions();
                SetupStartupOptions();
                SetupBinding();
                SetupLock();
                SetupActiveScene();

            }

            #region Active scene

            void SetupActiveScene() =>
                element.Q<DropdownField>("dropdown-active-scene").
                    SetupSceneDropdown(
                    getScenes: () => collection.scenes,
                    getValue: () => collection.activeScene,
                    setValue: (s) =>
                    {
                        collection.activeScene = s;
                        collection.Save();
                    });

            #endregion
            #region Popups

            bool IsParameterValid(object parameter, out SceneCollection collection)
            {

                parameter ??= window.m_collectionPopup_collection;
                collection = parameter as SceneCollection;

                if (!collection)
                    return false;

                window.m_collectionPopup_collection = collection;
                return true;

            }

            void IPopup.OnClose(VisualElement element) =>
                window.m_collectionPopup_collection = null;

            #endregion
            #region Lock

            void SetupLock()
            {

                var lockButton = element.Q<Button>("button-lock");
                var unlockButton = element.Q<Button>("button-unlock");
                lockButton.clicked += () => collection.Lock(prompt: true);
                unlockButton.clicked += () => collection.Unlock(prompt: true);

                BindingHelper lockBinding = null;
                BindingHelper unlockBinding = null;

                ReloadButtons();
                element.SetupLockBindings(collection);

                void ReloadButtons()
                {

                    lockBinding?.Unbind();
                    unlockBinding?.Unbind();
                    lockButton.SetVisible(false);
                    unlockButton.SetVisible(false);

                    if (!SceneManager.settings.project.allowCollectionLocking)
                        return;

                    lockBinding = lockButton.BindVisibility(collection, nameof(collection.isLocked), true);
                    unlockBinding = unlockButton.BindVisibility(collection, nameof(collection.isLocked));

                }

            }

            #endregion
            #region Scene loader toggles

            void SetupSceneLoaderToggles()
            {

                var list = element.Q("group-scene-loader-toggles");

                Reload();

                void Reload()
                {

                    list.Clear();

                    foreach (var loader in SceneManager.runtime.GetToggleableSceneLoaders().ToArray())
                    {

                        var isCheck = collection.scenes.NonNull().All(s => s.sceneLoader == loader.Key);
                        var isMixedValue = !isCheck && collection.scenes.NonNull().Any(s => s.sceneLoader == loader.Key);

                        var button = new Toggle();
                        button.showMixedValue = isMixedValue;
                        button.label = loader.sceneToggleText;
                        button.SetValueWithoutNotify(isCheck);
                        button.RegisterValueChangedCallback(e =>
                        {
                            foreach (var scene in collection.scenes.NonNull())
                            {
                                if (e.newValue)
                                    scene.sceneLoader = loader.Key;
                                else
                                    scene.ClearSceneLoader();
                                scene.Save();
                            }
                            Reload();
                        });

                        list.Add(button);

                    }

                }

            }

            #endregion
            #region Loading options

            void SetupLoadingOptions()
            {

                var dropdown = element.Q<DropdownField>("dropdown-loading-scene");
                dropdown.
                    SetupSceneDropdown(
                    getScenes: () => Assets.scenes.Where(s => s.isLoadingScreen),
                    getValue: () => collection.loadingScreen,
                    setValue: (s) =>
                    {
                        collection.loadingScreen = s;
                        collection.Save();
                    });

                dropdown.SetEnabled(collection.loadingScreenUsage is LoadingScreenUsage.Override);
                _ = element.Q<EnumField>("enum-loading-screen").
                    RegisterValueChangedCallback(e =>
                        dropdown.SetEnabled(e.newValue is LoadingScreenUsage.Override));

            }

            #endregion
            #region Startup options

            void SetupStartupOptions()
            {

                var group = element.Q<RadioButtonGroup>("radio-group-startup");
                group.RegisterValueChangedCallback(e => collection.OnPropertyChanged(nameof(collection.startupOption)));
            }

            #endregion
            #region Binding

            void SetupBinding()
            {
                var section = element.Q<TemplateContainer>("SceneBinding");
#if ENABLE_INPUT_SYSTEM && INPUTSYSTEM
                SceneBindingUtility.SetupBindingField(section, collection);
#else
                section.SetEnabled(false);
#endif
            }

            #endregion

        }

    }

}

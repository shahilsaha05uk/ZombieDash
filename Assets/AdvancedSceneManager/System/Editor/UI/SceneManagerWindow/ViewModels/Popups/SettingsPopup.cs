using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AdvancedSceneManager.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    partial class SceneManagerWindow
    {

        [SerializeField] private VisualTreeAsset mainSettingsPage;
        [SerializeField] private VisualTreeAsset startupPage;
        [SerializeField] private VisualTreeAsset sceneLoadingPage;
        [SerializeField] private VisualTreeAsset assetsPage;
        [SerializeField] private VisualTreeAsset networkPage;
        [SerializeField] private VisualTreeAsset windowPage;

        [SerializeField] private string m_activeSettingsPage;

        class SettingsHelper
        {

            public void Open() =>
                window.popups.Open<SettingsPopup>();

            public void Open<T>() where T : SettingsPage, new() => Open<T>(typeof(T));

            void Open<T>(object param = null) where T : SettingsPage, new()
            {
                if (window.popups.activeView?.model is SettingsPopup settings)
                    settings.Open<T>(param);
                else
                    window.popups.Open<SettingsPopup>(param);
            }

        }

        SettingsHelper settings { get; } = new();

        abstract class SettingsPage : ViewModel
        {
            public SettingsPopup settingsPopup => SettingsPopup.instance;
            public abstract string Header { get; }
        }

        class SettingsPopup : ViewManager<SettingsPage>, IPopup
        {

            public static SettingsPopup instance { get; private set; }

            protected override VisualElement GetParent() =>
                rootVisualElement.Q("setting-page-host");

            protected override Dictionary<Type, VisualTreeAsset> GetTemplates() =>
                new()
                {
                    { typeof(StartupPage), window.startupPage },
                    { typeof(SceneLoadingPage), window.sceneLoadingPage },
                    { typeof(AssetsPage), window.assetsPage },
                    { typeof(EditorPage), window.windowPage },
                    { typeof(NetworkPage), window.networkPage },
                };

            void GoToMain() =>
                container.style.left = new StyleLength(new Length(0, LengthUnit.Percent));

            void GoToPage() =>
                container.style.left = new StyleLength(new Length(-100, LengthUnit.Percent));

            public override void OnSizeChanged()
            {

                base.OnSizeChanged();

                if (Type.GetType(window.m_activeSettingsPage ?? "", false) is Type type)
                    _ = TryOpen(type);

            }

            VisualElement container;
            public override void OnCreateGUI(VisualElement element, object param)
            {

                instance = this;
                base.OnCreateGUI(element);

                container = rootVisualElement.Q("popup-settings");

                container.Q<Button>("button-startup").clicked += Open<StartupPage>;
                container.Q<Button>("button-scene-loading").clicked += Open<SceneLoadingPage>;
                container.Q<Button>("button-assets").clicked += Open<AssetsPage>;
                container.Q<Button>("button-editor").clicked += Open<EditorPage>;

                var netcodeButton = container.Q<Button>("button-network");
                netcodeButton.SetVisible(false);

#if NETCODE
                netcodeButton.clicked += Open<NetworkPage>;
                netcodeButton.SetVisible(true);
#endif

                container.Q<Button>("button-back").clicked += async () => await Close();

                TryOpenPage(param);

            }

            void TryOpenPage(object param)
            {

                var didOpenPage =
                    (param is Type type && TryOpen(type)) ||
                    (Type.GetType(window.m_activeSettingsPage ?? "", false) is Type savedType && TryOpen(savedType));

                if (!didOpenPage)
                    GoToMain();

            }

            protected override async Task OnOpen(SettingsPage model, VisualElement element, object parameter = null)
            {

                await base.OnOpen(model, element);

                container.Q<Label>("label-page-header").text = model.Header;
                rootVisualElement.Q("popup-overlay").Q<ScrollView>().verticalScroller.value = 0;

                if (model is not null)
                    GoToPage();

            }

            protected override async Task AnimateAndRemove(SettingsPage model, VisualElement element, bool hasNewView)
            {

                if (!hasNewView)
                    GoToMain();

                await Task.Delay(100);
                element?.RemoveFromHierarchy();
                await base.AnimateAndRemove(model, element, hasNewView);

            }

            public override async void OnRemoved()
            {

                window.m_activeSettingsPage = null;

                await Task.Delay(250);

                if (Profile.current)
                    Profile.current.Save();
                ASMSettings.instance.Save();
                ASMUserSettings.instance.Save();

            }

            public override void OnDisable() =>
                window.m_activeSettingsPage = activeView?.model?.GetType()?.FullName;

        }

    }

}

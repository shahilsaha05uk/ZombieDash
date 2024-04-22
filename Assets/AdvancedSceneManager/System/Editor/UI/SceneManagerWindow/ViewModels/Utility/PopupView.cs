using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    partial class SceneManagerWindow
    {

        [SerializeField] private VisualTreeAsset overviewPopup;
        [SerializeField] private VisualTreeAsset settingsPopup;
        [SerializeField] private VisualTreeAsset menuPopup;
        [SerializeField] private VisualTreeAsset importScenePopup;
        [SerializeField] private VisualTreeAsset collectionPopup;
        [SerializeField] private VisualTreeAsset dynamicCollectionPopup;
        [SerializeField] private VisualTreeAsset scenePopup;

        [SerializeField] private VisualTreeAsset pickNamePopup;
        [SerializeField] private VisualTreeAsset listPopup;

        [SerializeField] private string m_activePopup;

        interface IPopup
        {
            void OnOpen(VisualElement element, object parameter) { }
            void OnClose(VisualElement element) { }
        }

        class PopupView : ViewManager<IPopup>
        {

            protected override Dictionary<Type, VisualTreeAsset> GetTemplates() =>
                new()
                {

                    { typeof(OverviewPopup), window.overviewPopup },
                    { typeof(SettingsPopup), window.settingsPopup },
                    { typeof(MenuPopup), window.menuPopup },
                    { typeof(CollectionPopup), window.collectionPopup },
                    { typeof(DynamicCollectionPopup), window.dynamicCollectionPopup },
                    { typeof(ScenePopup), window.scenePopup },

                    { typeof(ProfilePopup), window.listPopup },
                    { typeof(ExtraCollectionPopup), window.listPopup },

                    { typeof(PickNamePopup), window.pickNamePopup },

                    { typeof(ImportScenePopup), window.importScenePopup },
                    { typeof(InvalidScenePopup), window.importScenePopup },
                    { typeof(UntrackedScenePopup), window.importScenePopup },
                    { typeof(ImportedBlacklistedScenePopup), window.importScenePopup },
                    { typeof(BadPathScenePopup), window.importScenePopup },

                };

            protected override VisualElement GetParent() =>
                rootVisualElement.Q("popup-overlay");

            public override void OnCreateGUI(VisualElement element)
            {

                base.OnCreateGUI(element);

                parent.RegisterCallback<ClickEvent>(async e =>
                {
                    if (e.target == parent)
                        await Close();
                });

                UpdateMaxHeight();
                rootVisualElement.RegisterCallback<GeometryChangedEvent>(e => UpdateMaxHeight());

                var transitions = parent.style.transitionProperty;
                parent.style.transitionProperty = null;

                parent.style.paddingTop = window.position.height * 2;
                parent.style.opacity = 0;

                parent.style.transitionProperty = transitions;

                if (Type.GetType(window.m_activePopup ?? "", false) is Type type)
                    TryOpen(type);

                var scroll = element.Q<ScrollView>();
                scroll.RegisterCallback<WheelEvent>(e =>
                {
                    if (scroll.contentContainer.resolvedStyle.height <= scroll.contentViewport.resolvedStyle.height)
                    {
#if UNITY_2021 || UNITY_2022
                        e.PreventDefault();
#endif
                        e.StopPropagation();
                        e.StopImmediatePropagation();
                    }
                }, TrickleDown.TrickleDown);

            }

            void UpdateMaxHeight() =>
                effectiveParent.style.maxHeight = rootVisualElement.worldBound.height - 70;

            protected override async Task OnOpen(IPopup model, VisualElement element, object parameter = null)
            {

                rootVisualElement.Q<ScrollView>().verticalScrollerVisibility = ScrollerVisibility.Hidden;
                window.m_activePopup = model.GetType().FullName;

                parent.style.paddingTop = 0;
                parent.style.opacity = 1;

                model.OnOpen(element, parameter);
                await Task.Delay(250);

            }

            protected override async Task OnClose(IPopup model, VisualElement element, bool hasNewView)
            {

                parent.style.paddingTop = window.position.height * 2;
                parent.style.opacity = 0;

                rootVisualElement.Q<ScrollView>().verticalScrollerVisibility = ScrollerVisibility.Auto;
                window.m_activePopup = null;
                model.OnClose(element);

                await Task.Delay(250);
                window.save.Now();

            }

        }

    }

}

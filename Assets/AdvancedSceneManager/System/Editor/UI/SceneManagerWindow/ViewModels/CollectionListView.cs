using System.Collections.Generic;
using System.Linq;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    partial class SceneManagerWindow
    {

        [SerializeField] private VisualTreeAsset collectionView = null!;
        [SerializeField] private VisualTreeAsset sceneCollectionTemplate = null!;
        [SerializeField] private bool lastSearchScenes;
        [SerializeField] private string lastSearch;

        partial class CollectionListView : ViewModel
        {

            public ListView collectionsList;
            ListView dynamicCollectionsList;

            public Selection selection { get; } = new();

            public override void OnCreateGUI(VisualElement element)
            {

                selection.OnEnable();
                Profile.onProfileChanged += Reload;
                rootVisualElement.RegisterCallback<DetachFromPanelEvent>(e =>
                {
                    Profile.onProfileChanged -= Reload;
                    SceneImportUtility.scenesChanged -= Reload;
                });

                window.search.Initialize();
                Reload();

            }

            public void Reload()
            {

                element.Clear();
                element.Add(window.collectionView.Instantiate());

                collectionsList = element.Q<ListView>("list-collections");
                dynamicCollectionsList = element.Q<ListView>("list-dynamic-collections");

                collectionsList.SetVisible(SceneManager.profile);
                dynamicCollectionsList.SetVisible(SceneManager.profile);

                SetupNoProfileMessage();
                SetupNoItemsMessage();
                SetupLine();

                if (!SceneManager.profile)
                    return;

                SetupCollectionList<SceneCollection>(collectionsList);
                SetupCollectionList<DynamicCollection>(dynamicCollectionsList);
                SetupSingleCollection(Profile.current.standaloneScenes);

                SetupList(collectionsList);
                SetupList(dynamicCollectionsList);

                EditorApplication.delayCall += () =>
                {
                    UpdateSeparator();
                    ApplyAppearanceSettings(element);
                };

            }

            internal void UpdateSeparator()
            {
                var c = SceneManager.assets.collections.LastOrDefault();
                if (!c)
                    return;

                collectionsList.style.marginBottom =
                    window.expandedCollections.Contains(c.id)
                    ? 0
                    : 8;
            }

            void SetupList(ListView list)
            {

                //Both lists should use the same scrollview, so lets disable the lists own internal scrollview
                list.Q<ScrollView>().verticalScrollerVisibility = ScrollerVisibility.Hidden;

                //We use padding-bottom to give some space for expanded collections, this prevents that on last item in list
                list.Query<TemplateContainer>().Last()?.AddToClassList("last");

                list.itemIndexChanged -= OnItemIndexChanged;
                list.itemIndexChanged += OnItemIndexChanged;

                void OnItemIndexChanged(int oldIndex, int newIndex) =>
                  EditorApplication.delayCall += window.collections.Reload;

            }

            void SetupNoItemsMessage() => element.Q("label-no-items").visible = SceneManager.profile && !SceneManager.profile.collections.Any();
            void SetupNoProfileMessage() => element.Q("label-no-profile").visible = !SceneManager.profile;
            void SetupLine() => element.Q("line").visible = SceneManager.profile;

            void SetupCollectionList<T>(ListView list) where T : ISceneCollection
            {

                list.makeItem = window.sceneCollectionTemplate.Instantiate;

                if (typeof(T) == typeof(SceneCollection))
                {

                    if (window.search.isSearching && window.search.savedSearch != null)
                    {
                        var items = window.search.savedSearch.Keys.ToList();
                        list.itemsSource = items;
                        list.bindItem = (element, index) =>
                        {
                            if (items.ElementAtOrDefault(index) is SceneCollection c)
                                OnSetupCollection(element, c);
                        };
                    }
                    else
                    {
                        var property = new SerializedObject(Profile.current).FindProperty("m_collections");
                        list.bindItem = (element, index) =>
                        {
                            if (Profile.current && Profile.current.collections.ElementAtOrDefault(index) is SceneCollection c && c)
                                OnSetupCollection(element, c);
                        };
                        list.BindProperty(property);
                    }

                }
                else if (typeof(T) == typeof(DynamicCollection))
                {
                    var collections = Profile.current.dynamicCollections.ToArray();
                    list.bindItem = (element, index) => OnSetupCollection(element, collections[index]);
                    list.itemsSource = collections;
                }

            }

            void SetupSingleCollection(ISceneCollection collection)
            {

                var element = window.sceneCollectionTemplate.Instantiate();
                rootVisualElement.Q("list-special-collections").Add(element);
                OnSetupCollection(element, collection);

            }

            public readonly Dictionary<ISceneCollection, CollectionItem> views = new();
            void OnSetupCollection(VisualElement element, ISceneCollection collection)
            {

                if (collection is null)
                    return;

                element.RegisterCallback<DetachFromPanelEvent>(e =>
                {
                    if (views.Remove(collection, out var item))
                        item.OnRemoved();
                });

                var view = views.Set(collection, new CollectionItem(collection));
                view.element = element;
                view.OnCreateGUI(element);

            }

            public override void ApplyAppearanceSettings(VisualElement element)
            {
                foreach (var view in views)
                    view.Value.ApplyAppearanceSettings(view.Value.element);
            }

        }

    }

}

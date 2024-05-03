using System;
using System.Collections.Generic;
using System.Linq;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    partial class SceneManagerWindow
    {

        void OnGUI()
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.F)
            {
                if (Event.current.control)
                    search.Start();
            }
        }

        public Search search { get; } = new();

        public class Search
        {

            public bool lastSearchScenes
            {
                get => window.lastSearchScenes;
                private set => window.lastSearchScenes = value;
            }

            public string lastSearch
            {
                get => window.lastSearch;
                private set => window.lastSearch = value;
            }

            public bool IsInActiveSearch(SceneCollection collection, Scene scene = null)
            {
                if (!isSearching)
                    return false;

                if (!lastSearchScenes)
                    return savedSearch.ContainsKey(collection);
                else
                    return savedSearch.GetValueOrDefault(collection)?.Contains(scene) ?? false;
            }

            public SerializableDictionary<SceneCollection, Scene[]> savedSearch { get; private set; }

            public void Initialize()
            {
                Setup();
            }

            public void Toggle()
            {
                if (searchGroup.style.display == DisplayStyle.Flex)
                    Stop();
                else
                    Start();
            }

            public void Start()
            {
                searchGroup.SetVisible(true);
                UpdateSaved();
                search.Focus();

                EditorApplication.delayCall = UpdateSearchButton;
            }

            public void Stop()
            {
                window.lastSearch = string.Empty;
                savedSearch = null;
                search.SetValueWithoutNotify(string.Empty);
                searchGroup.SetVisible(shouldDisplaySearch);
                window.collections.Reload();
                UpdateSearchButton();
            }

            GroupBox searchGroup;
            TextField search;
            RadioButton searchCollections;
            RadioButton searchScenes;
            Button searchButton;

            Button saveButton;
            VisualElement list;

            public bool isSearching => !string.IsNullOrEmpty(window.lastSearch);
            public bool shouldDisplaySearch => SceneManager.settings.user.alwaysDisplaySearch || isSearching || search?.panel?.focusController?.focusedElement == search;

            #region Setup

            void Setup() =>
                SceneManager.OnInitialized(() =>
                {

                    searchGroup = rootVisualElement.Q<GroupBox>("group-search");
                    search = rootVisualElement.Q<TextField>("text-search");
                    placeholder = search.Q("label-placeholder");
                    searchCollections = rootVisualElement.Q<RadioButton>("toggle-collections");
                    searchScenes = rootVisualElement.Q<RadioButton>("toggle-scenes");
                    saveButton = rootVisualElement.Q<Button>("button-save-search");
                    list = rootVisualElement.Q("list-saved");
                    searchButton = rootVisualElement.Q<Button>("button-search");

                    if (isSearching && savedSearch == null)
                        UpdateSearch(window.lastSearch, window.lastSearchScenes, true, false);

                    SetupSave();

                    SetupToggles();
                    SetupPlaceholder();
                    SetupSearchBox();
                    SetupSearchButton();
                    SetupGroup();

                    SceneManager.OnInitialized(() =>
                    {
                        SceneManager.settings.user.PropertyChanged += (s, e) =>
                        {
                            searchGroup.SetVisible(shouldDisplaySearch);
                            UpdateSearchButton();
                        };
                    });

                });

            void SetupSave()
            {

                UpdateSaved();
                UpdateSaveButton();

                saveButton.clickable = null;
                saveButton.clickable = new(() =>
                {
                    if (SceneManager.settings.user.savedSearches.Contains(search.text))
                        ArrayUtility.Remove(ref SceneManager.settings.user.savedSearches, search.text);
                    else
                        ArrayUtility.Add(ref SceneManager.settings.user.savedSearches, search.text);
                    SceneManager.settings.user.Save();
                    UpdateSaved();
                    UpdateSaveButton();
                });

                search.RegisterValueChangedCallback(e => UpdateSaveButton());

            }

            void SetupToggles()
            {
                UpdateToggles();

                searchCollections.RegisterValueChangedCallback(e => { if (e.newValue) UpdateSearch(window.lastSearch, false); });
                searchScenes.RegisterValueChangedCallback(e => { if (e.newValue) UpdateSearch(window.lastSearch, true); });
            }

            VisualElement placeholder;
            void SetupPlaceholder()
            {
                if (placeholder == null)
                    UpdatePlaceholder();
            }

            void UpdatePlaceholder()
            {
                placeholder.SetVisible(string.IsNullOrEmpty(search.text));
            }

            void SetupSearchBox()
            {
#if UNITY_2022_1_OR_NEWER
                search.selectAllOnMouseUp = false;
                search.selectAllOnFocus = false;
#endif
                search.value = window.lastSearch;

                UpdatePlaceholder();
                search.RegisterValueChangedCallback(e => UpdatePlaceholder());

                search.RegisterCallback<KeyUpEvent>(e =>
                {
                    if (e.keyCode is KeyCode.KeypadEnter or KeyCode.Return)
                        UpdateSearch(search.text, window.lastSearchScenes);
                    else
                        UpdateSearchDelayed();
                    UpdateSaveButton();
                });

            }

            void SetupSearchButton()
            {
                rootVisualElement.Q<Button>("button-search").clicked += window.search.Toggle;

                UpdateSearchButton();
            }

            void SetupGroup()
            {
                searchGroup.SetVisible(shouldDisplaySearch);

                var isHover = false;
                searchGroup.RegisterCallback<PointerEnterEvent>(e =>
                {
                    isHover = true;
                });

                searchGroup.RegisterCallback<PointerLeaveEvent>(e =>
                {
                    isHover = false;
                });

                searchGroup.RegisterCallback<FocusOutEvent>(e =>
                    EditorApplication.delayCall += () =>
                    {

                        if (isHover)
                        {
                            if (search.panel.focusController.focusedElement != search)
                            {
                                RefocusSearch();
                            }
                        }
                        else
                        {
                            UpdateSearch(window.lastSearch, window.lastSearchScenes);
                            if (string.IsNullOrEmpty(search.text))
                                Stop();
                        }

                    });

                searchGroup.RegisterCallback<PointerDownEvent>(e =>
                {
                    if (search.panel.focusController.focusedElement != search)
                        RefocusSearch(e.clickCount > 2);
                });

                void RefocusSearch(bool isDoubleClick = false)
                {

                    search.Focus();

                    if (isDoubleClick)
                        search.SelectAll();
                    else
                        search.SelectRange(search.text.Length, search.text.Length);

                }

            }

            #endregion
            #region Search

            System.Timers.Timer timer = null;

            void UpdateSearchDelayed()
            {

                if (timer is null)
                {
                    timer = new(500) { Enabled = false };
                    timer.Elapsed += (s, e) =>
                    {
                        EditorApplication.delayCall += () => UpdateSearch(search.text, window.lastSearchScenes);
                    };
                }

                timer.Stop();
                timer.Start();

            }

            void UpdateSearch(string q, bool searchScenes, bool force = false, bool reload = true)
            {

                timer?.Stop();

                if (!force && window.lastSearch == q && window.lastSearchScenes == searchScenes)
                    return;

                window.lastSearch = q;
                window.lastSearchScenes = searchScenes;
                UpdateToggles();

                savedSearch =
                    !window.lastSearchScenes
                    ? FindCollections(q)
                    : FindScenes(q);

                if (reload)
                    window.collections.Reload();

            }

            #endregion
            #region Update UI

            void UpdateToggles()
            {
                searchCollections.SetValueWithoutNotify(!window.lastSearchScenes);
                searchScenes.SetValueWithoutNotify(window.lastSearchScenes);
            }

            void UpdateSearchButton()
            {
                searchButton.text = shouldDisplaySearch ? "" : "";
                searchButton.tooltip = shouldDisplaySearch ? "Clear search" : "Search";
            }

            void UpdateSaveButton()
            {

                saveButton.SetVisible(!string.IsNullOrEmpty(search.text));
                if (SceneManager.settings.user.savedSearches?.Contains(search.text) ?? false)
                {
                    saveButton.RemoveFromClassList("fontAwesomeRegular");
                    saveButton.text = "";
                }
                else
                {
                    saveButton.AddToClassList("fontAwesomeRegular");
                    saveButton.text = "";
                }

            }

            void UpdateSaved()
            {
                list.Clear();
                foreach (var item in SceneManager.settings.user.savedSearches ?? Array.Empty<string>())
                    list.Add(new Button(() => { search.value = item; UpdateSearch(item, lastSearchScenes); UpdateSaveButton(); }) { text = item, name = "button-saved-search" });
            }

            #endregion
            #region Find

            SerializableDictionary<SceneCollection, Scene[]> FindCollections(string q)
            {

                var dict = new Dictionary<SceneCollection, List<Scene>>();
                foreach (var collection in Profile.current.collections)
                    if (collection.title.Contains(q, System.StringComparison.InvariantCultureIgnoreCase))
                        dict.Add(collection, null);

                var dict2 = new SerializableDictionary<SceneCollection, Scene[]>() { throwOnDeserializeWhenKeyValueMismatch = false };
                foreach (var item in dict)
                    dict2.Add(item.Key, null);

                return dict2;

            }

            SerializableDictionary<SceneCollection, Scene[]> FindScenes(string q)
            {

                var dict = new Dictionary<SceneCollection, List<Scene>>();
                foreach (var collection in Profile.current.collections)
                    foreach (var scene in collection.scenes.NonNull())
                    {
                        if (scene.name.Contains(q, System.StringComparison.InvariantCultureIgnoreCase))
                            if (dict.ContainsKey(collection))
                                dict[collection].Add(scene);
                            else
                                dict.Add(collection, new() { scene });
                    }

                var dict2 = new SerializableDictionary<SceneCollection, Scene[]>() { throwOnDeserializeWhenKeyValueMismatch = false };
                foreach (var item in dict)
                    dict2.Add(item.Key, item.Value.ToArray());

                return dict2;

            }

            #endregion

        }

    }

}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Models.Utility;
using AdvancedSceneManager.Utility;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    partial class SceneManagerWindow
    {

        [SerializeField] private VisualTreeAsset sceneTemplate = null!;
        [SerializeField] private List<string> expandedCollections = new();

        class CollectionItem : ViewModel
        {

            public ISceneCollection collection { get; private set; }

            bool isSearchMode => (collection is SceneCollection && window.search.isSearching);
            bool shouldOpenInSearchMode => isSearchMode && window.lastSearchScenes;

            public CollectionItem(ISceneCollection collection) =>
                this.collection = collection;

            public override void OnCreateGUI(VisualElement element)
            {

                if (collection is SceneCollection c)
                    element.Bind(new(c));

                SetupHeader();
                SetupContent();

            }

            public override void OnRemoved()
            {
                if (views is not null)
                    foreach (var view in views)
                        view.OnRemoved();
                views = Array.Empty<SceneItem>();
            }

            #region Header

            #region Button callbacks

            void Remove(params ISceneCollection[] collections)
            {
                foreach (var collection in collections)
                    Profile.current.Remove(collection);
                EditorApplication.delayCall += window.undo.Reload;
                window.collections.Reload();
            }

            void CreateTemplate(SceneCollection collection) =>
                SceneCollectionTemplate.CreateTemplate(collection);

            void Play(bool openAll)
            {
                if (collection is SceneCollection c)
                    SceneManager.app.Start(new() { openCollection = c, forceOpenAllScenesOnCollection = openAll });
            }

            void Open(bool openAll)
            {
                if (collection is SceneCollection c)
                    SceneManager.runtime.Open(c, openAll).CloseAll();
            }

            void OpenAdditive(bool openAll)
            {

                if (collection is not SceneCollection c)
                    return;

                if (c.isOpen)
                    SceneManager.runtime.Close(c);
                else
                    SceneManager.runtime.OpenAdditive(c, openAll);

            }

            #endregion

            void SetupHeader()
            {

                element.Q<Label>(name: "label-title").BindText(collection, nameof(collection.title));

                SetupContextMenu();

                SetupOpenButtons();

                SetupExpander();
                SetupCollectionDrag();
                SetupSceneHeaderDrop();
                SetupStartupIndicator();

                SetupMenu();
                SetupRemove();
                SetupAdd();

                ApplyAppearanceSettings(element);
                SetupLocking();

            }

            void SetupLocking()
            {

                if (collection is not SceneCollection c)
                    return;

                element.SetupLockBindings(c);

                var menuButton = element.Q<Button>("button-menu");
                var lockButton = element.Q<Button>("button-collection-header-unlock");
                lockButton.clicked += () => c.Unlock(prompt: true);

                BindingHelper lockBinding = null;
                BindingHelper menuBinding = null;

                ReloadButtons();

                void ReloadButtons()
                {

                    lockBinding?.Unbind();
                    menuButton?.Unbind();
                    lockButton.SetVisible(false);
                    menuButton.SetVisible(true);

                    if (!SceneManager.settings.project.allowCollectionLocking)
                        return;

                    lockBinding = lockButton.BindVisibility(c, nameof(c.isLocked), false);
                    menuBinding = menuButton.BindVisibility(c, nameof(c.isLocked), true);

                }

            }

            void SetupContextMenu()
            {

                if (collection is not SceneCollection c)
                    return;

                element.Q("button-header").ContextMenu(e =>
                {

                    e.StopPropagation();

                    var collections = window.collections.selection.collections.Concat(c).ToArray();
                    GenerateCollectionHeader(collections);

                    var isSingleVisibility = collections.Length == 1 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;

                    e.menu.AppendAction("View in project view...", e => EditorGUIUtility.PingObject(collection is SceneCollection c ? c : Profile.current), isSingleVisibility);

                    e.menu.AppendAction("Create template...", e =>
                    {
                        CreateTemplate((SceneCollection)collection);
                        window.popups.Open<ExtraCollectionPopup>();
                    }, isSingleVisibility);

                    e.menu.AppendSeparator(); e.menu.AppendSeparator();
                    e.menu.AppendAction("Remove...", e => Remove(collections.ToArray()));

                    void GenerateCollectionHeader(params SceneCollection[] collections)
                    {
                        foreach (var c in collections)
                            e.menu.AppendAction(c.title, e => { }, DropdownMenuAction.Status.Disabled);
                        e.menu.AppendSeparator();
                    }

                });

            }

            public override void ApplyAppearanceSettings(VisualElement element)
            {

                element?.Q("toggle-collection-include")?.SetVisible(collection is SceneCollection && SceneManager.settings.project.allowExcludingCollectionsFromBuild);
                element?.Q("button-collection-play")?.SetVisible(collection is SceneCollection && SceneManager.settings.user.displayCollectionPlayButton);
                element?.Q("button-collection-open")?.SetVisible(collection is SceneCollection && SceneManager.settings.user.displayCollectionOpenButton);
                element?.Q("button-collection-open-additive")?.SetVisible(collection is SceneCollection && SceneManager.settings.user.displayCollectionAdditiveButton);

                element?.Q("label-reorder-collection")?.SetVisible(collection is SceneCollection && !window.search.isSearching);
                element?.Q("button-add-scene")?.SetVisible(collection is ISceneCollection.IEditable);
                element?.Q("button-remove")?.SetVisible(collection is SceneCollection or DynamicCollection);
                element?.Q("button-menu")?.SetVisible(collection is SceneCollection or DynamicCollection);

                if (views?.Any() ?? false)
                    foreach (var view in views)
                        view.ApplyAppearanceSettings(view.element);

            }

            #region Left

            void SetupOpenButtons()
            {

                if (collection is not SceneCollection c || !c)
                    return;

                element.Q<Button>("button-collection-open-additive").BindText(c, nameof(c.isOpen), "", "");

                Setup(element.Q<Button>("button-collection-play"), Play);
                Setup(element.Q<Button>("button-collection-open"), Open);
                Setup(element.Q<Button>("button-collection-open-additive"), OpenAdditive);

                void Setup(Button button, Action<bool> action)
                {

                    button.clickable = new(() => { });
                    button.clickable.activators.Add(new() { modifiers = EventModifiers.Shift });
                    button.clickable.clickedWithEventInfo += (e) =>
                    {
                        if (e is PointerUpEvent e1)
                            action.Invoke(e1.shiftKey);
                        else if (e is MouseUpEvent e2)
                            action.Invoke(e2.shiftKey);
                    };

#if !COROUTINES
                    button.SetEnabled(false);
                    button.tooltip = "Editor coroutines needed to use this feature.";
#endif

                }

            }

            #endregion
            #region Middle

            void SetupExpander()
            {

                var header = element.Q("collection-header");
                var expander = element.Q<Button>("button-header");
                var list = element.GetAncestor<ListView>();
                bool? expandedOverride = null;

                UpdateExpanded();
                UpdateSelection();

                expander.clickable = null;
                expander.clickable = new(() => { });
                expander.clickable.activators.Add(new() { modifiers = EventModifiers.Control });
                expander.clickable.clickedWithEventInfo += (_e) =>
                {

#if UNITY_2022 || UNITY_2021
                    if (_e is not MouseUpEvent e)
                        return;
#else
                    if (_e is not PointerUpEvent e)
                        return;
#endif

                    if (e.ctrlKey || e.commandKey)
                    {

                        if (collection is not SceneCollection c)
                            return;

                        window.collections.selection.ToggleSelection(this);

                        var i = Profile.current.IndexOf(c);
                        if (list.selectedIndices.Contains(i))
                            list.RemoveFromSelection(i);
                        else
                            list.AddToSelection(i);

                        UpdateSelection();

                    }
                    else
                        ToggleExpanded(collection);

                };

                void UpdateSelection() =>
                    header.EnableInClassList("selected", window.collections.selection.IsSelected(this));

                void UpdateExpanded()
                {
                    var isExpanded = shouldOpenInSearchMode || window.expandedCollections.Contains(collection.id);
                    if (isSearchMode && expandedOverride.HasValue)
                        isExpanded = expandedOverride.Value;

                    element.Q("collection").EnableInClassList("expanded", isExpanded);
                    element.Q<Label>("label-expanded-status").text = isExpanded ? "" : "";
                    element.Q<Label>("label-expanded-status").style.marginTop = isExpanded ? 0 : 1;
                    window.collections.UpdateSeparator();
                    SetupScenes();
                }

                void ToggleExpanded(ISceneCollection collection)
                {
                    if (isSearchMode)
                        expandedOverride = !(expandedOverride ?? false);
                    else
                    {
                        if (!window.expandedCollections.Remove(collection.id))
                            window.expandedCollections.Add(collection.id);
                    }

                    UpdateExpanded();
                }

            }

            void SetupCollectionDrag()
            {

                if (this.collection is not SceneCollection collection)
                    return;

                var header = element.Q("button-header");

                bool isDown = false;

#if UNITY_2022
                header.RegisterCallback<MouseDownEvent>(e => isDown = true, TrickleDown.TrickleDown);
#else
                header.RegisterCallback<PointerDownEvent>(e => isDown = true, TrickleDown.TrickleDown);
#endif

                header.RegisterCallback<MouseLeaveEvent>(e =>
                {
                    var isDragging = DragAndDrop.objectReferences.Length == 1 && DragAndDrop.objectReferences[0] == collection;
                    if (isDown && !isDragging && e.pressedButtons == 1)
                    {
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.objectReferences = new[] { collection };
                        DragAndDrop.StartDrag("Collection drag: " + collection.name);
                        window.collections.Reload();
                    }
                    isDown = false;

                });

            }

            void SetupSceneHeaderDrop()
            {

                if (this.collection is not ISceneCollection.IEditable collection)
                    return;

                var header = element.Q("button-header");

                header.RegisterCallback<DragUpdatedEvent>(e =>
                {
                    e.StopPropagation();
                    var scenes = SceneField.GetDragDropScenes().ToArray();
                    DragAndDrop.visualMode = scenes.Length > 0 ? DragAndDropVisualMode.Link : DragAndDropVisualMode.Rejected;
                });

                header.RegisterCallback<DragPerformEvent>(e =>
                {

                    var scenes = SceneField.GetDragDropScenes();
                    if (scenes.Any())
                        collection.Add(scenes.ToArray());

                });

            }

            void SetupStartupIndicator()
            {
                if (collection is SceneCollection c)
                    element.Q("label-startup").BindVisibility(c, nameof(c.isStartupCollection));
                else
                    element.Q("label-startup").SetVisible(false);
            }

            #endregion
            #region Right

            void SetupMenu()
            {
                element.Q<Button>("button-menu").clicked += () =>
                {

                    if (collection is SceneCollection sc)
                        window.popups.Open<CollectionPopup>(sc);

                    else if (collection is DynamicCollection dc)
                        window.popups.Open<DynamicCollectionPopup>(dc);

                };
            }

            void SetupRemove() =>
                element.Q<Button>("button-remove").clicked += () => Remove(collection);

            void SetupAdd()
            {

                element.Q<Button>("button-add-scene").clickable = new Clickable(() =>
                {

                    (collection as ISceneCollection.IEditable)?.AddEmptyScene();
                    _ = window.expandedCollections.Remove(collection.id);
                    window.expandedCollections.Add(collection.id);

                    window.collections.Reload();

                });

            }

            #endregion

            #endregion
            #region Content

            void SetupContent()
            {
                SetupSceneReorder();
                SetupDescription();
                SetupScenes();
                SetupNoScenesLabel();
                SetupSceneDropZone();
            }

            void SetupSceneReorder()
            {

                var list = element.Q<ListView>("list");

                //Nested listviews are not supported out of the box, this fixes that by just preventing events from reaching parent listview
                list.RegisterCallback<PointerDownEvent>(e =>
                {
                    e.StopPropagation();
                    list.CaptureMouse();
                    list.Clear();
                });

                list.RegisterCallback<PointerMoveEvent>(e =>
                {
                    if (e.pressedButtons == 0)
                        list.ReleaseMouse();
                });

                list.itemIndexChanged += (oldIndex, newIndex) =>
                {
                    list.ReleaseMouse();
                    if (collection is ISceneCollection.IEditable c)
                        EditorApplication.delayCall += () => c.Move(oldIndex, newIndex);
                };

            }

            void SetupDescription()
            {
                element.Q<Label>("label-description").text = collection.description;
                element.Q<BindableElement>("label-description").BindVisibility(collection, propertyPath: nameof(collection.description));
            }

            public SceneItem[] views;
            void SetupScenes()
            {

                var list = element.Q<ListView>("list");

                if (collection is DynamicCollection)
                    list.AddToClassList("dynamic");

                list.makeItem = window.sceneTemplate.Instantiate;

                list.bindItem = (element, index) =>
                {

                    if (views is null || views.ElementAtOrDefault(index) is not SceneItem view)
                        return;

                    view.element = element;
                    view.OnCreateGUI(element);
                    view.ApplyAppearanceSettings(element);

                };

                collection.PropertyChanged -= OnPropertyChanged;
                collection.PropertyChanged += OnPropertyChanged;

                void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
                {
                    if (e.PropertyName is nameof(collection.scenes))
                        Reload();
                }

                Reload();
                void Reload()
                {

                    if (collection is DynamicCollection d)
                        views = collection.scenePaths?.Select((path, i) => new SceneItem(collection, path, i))?.ToArray() ?? Array.Empty<SceneItem>();
                    else
                        views = collection.scenes?.Where(IsVisible)?.Select((scene, i) => new SceneItem(collection, scene, i))?.ToArray() ?? Array.Empty<SceneItem>();

                    list.itemsSource = views;
                    list.SetVisible(views.Any());
                    list.Rebuild();
                    SetupNoScenesLabel();

                }

                bool IsVisible(Scene scene)
                {
                    if (!isSearchMode)
                        return true;

                    return window.search.IsInActiveSearch(collection as SceneCollection, scene);
                }

            }

            void SetupNoScenesLabel()
            {

                var list = element.Q<ListView>("list");
                var labelNoScenes = element.Q<Label>("label-no-scenes");

                labelNoScenes.SetVisible(list.itemsSource is null || list.itemsSource.Count == 0);

                if (collection is SceneCollection or StandaloneCollection)
                    labelNoScenes.text = "No scenes here, you can add some using the plus button above.";
                else if (collection is DynamicCollection)
                    labelNoScenes.text =
                        "Dynamic collections guarantee that all scenes within a certain folder will be included in build.\n\n" +
                        "No scenes were found in target folder.";

            }

            void SetupSceneDropZone()
            {

                if (this.collection is not ISceneCollection.IEditable collection)
                    return;

                var zone = element.Q("scene-drop-zone");

                zone.RegisterCallback<DragUpdatedEvent>(e =>
                {
                    e.StopPropagation();
                    e.StopImmediatePropagation();
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                });

                zone.RegisterCallback<DragPerformEvent>(e =>
                {
                    var scenes = SceneField.GetDragDropScenes();
                    collection.Add(SceneField.GetDragDropScenes().ToArray());
                    zone.SetVisible(false);
                });

                rootVisualElement.RegisterCallback<DragUpdatedEvent>(e =>
                {
                    zone.SetVisible(IsSceneDrag());
                }, TrickleDown.TrickleDown);

                rootVisualElement.RegisterCallback<DragExitedEvent>(e =>
                {
                    zone.SetVisible(false);
                });

                rootVisualElement.RegisterCallback<PointerLeaveEvent>(e =>
                {
                    zone.SetVisible(false);
                });

                bool IsSceneDrag()
                {

                    if (DragAndDrop.objectReferences.Length == 0)
                        return false;

                    var scenes = SceneField.GetDragDropScenes();
                    return scenes.Any();

                }

            }

            #endregion

        }

    }

}

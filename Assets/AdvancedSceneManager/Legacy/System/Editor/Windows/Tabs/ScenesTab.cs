using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Timers;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static AdvancedSceneManager.Editor.SceneManagerWindow;
using Lightmapping = UnityEditor.Lightmapping;
using Selection = AdvancedSceneManager.Editor.SceneManagerWindow.Selection;

namespace AdvancedSceneManager.Editor.Window
{

    public static class ScenesTab
    {

        #region OnEnable

        public static void OnEnable(VisualElement element)
        {

            SceneManagerWindow.window.LoadContent(collectionTemplate, element, loadStyle: true);
            SceneManagerWindow.window.LoadContent(sceneTemplate, element, loadStyle: true);

            PopulateList(element.Q("collection-list"));
            Selection.OnSelectionChanged += RefreshFooterButtons;

            Popup.OnClosed += Popup_OnClosed;

            _ = ExtraAddMenuButton.Visible(SceneManager.settings.local.displayExtraAddCollectionButton);

        }

        public static void OnDisable()
        {
            Selection.OnSelectionChanged -= RefreshFooterButtons;
            Popup.OnClosed -= Popup_OnClosed;
            ClearListeners();
        }

        static void Popup_OnClosed() =>
            EditorApplication.delayCall += Selection.Clear;

        const string collectionTemplate = "AdvancedSceneManager/Templates/SceneCollection";
        const string sceneTemplate = "AdvancedSceneManager/Templates/Scene";

        static VisualElement list;

        /// <summary>Populate using last <see cref="VisualElement"/> list.</summary>
        static void PopulateList() => PopulateList(null);

        /// <summary>
        /// <para>Populate the <see cref="VisualElement"/> list.</para>
        /// <para>Passing <paramref name="list"/> as null will use last list.</para>
        /// </summary>
        static void PopulateList(VisualElement list = null)
        {

            if (BuildPipeline.isBuildingPlayer)
                return;

            ScenesTab.list?.Clear();
            if (list == null)
                list = ScenesTab.list;
            if (list != null)
            {

                ScenesTab.list = list;
                list.Clear();

                PromptAddCollection();
                Collections();
                DynamicCollections();
                RoundedCornerHelper.Update();

            }

        }

        #endregion
        #region Collection prompt

        static void PromptAddCollection()
        {

            var element = list.parent.Q("noCollectionsPrompt");
            if (element == null)
                return;

            if (!Profile.current)
                element.style.display = DisplayStyle.None;
            else if (!Profile.current.collections.Any())
                element.style.display = DisplayStyle.Flex;
            else
                element.style.display = DisplayStyle.None;

        }

        #endregion
        #region Create collection elements

        class HeaderManipulator : MouseManipulator
        {

            readonly ToolbarToggle toggle;
            public bool CanInitiateDragDrop { get; set; }

            public HeaderManipulator(ToolbarToggle toggle) =>
                this.toggle = toggle;

            static IEventHandler downHeader = null;
            protected override void RegisterCallbacksOnTarget()
            {
                target.RegisterCallback<MouseDownEvent>(MouseDown);
                target.RegisterCallback<MouseLeaveEvent>(MouseLeave);
                target.RegisterCallback<MouseUpEvent>(MouseUp);
                if (CanInitiateDragDrop)
                    target.RegisterCallback<MouseMoveEvent>(MouseMove);
            }

            protected override void UnregisterCallbacksFromTarget()
            {
                target.UnregisterCallback<MouseDownEvent>(MouseDown);
                target.UnregisterCallback<MouseLeaveEvent>(MouseLeave);
                target.UnregisterCallback<MouseUpEvent>(MouseUp);
                target.UnregisterCallback<MouseMoveEvent>(MouseMove);
                down = false;
            }

            bool down;
            Vector2? startPos;
            void MouseDown(MouseDownEvent e)
            {
                if (e.button == 0 && e.modifiers == EventModifiers.None)
                {
                    down = true;
                    downHeader = e.target;
                    startPos = e.localMousePosition;
                }
            }

            void MouseLeave(MouseLeaveEvent e)
            {
                downHeader = null;
                down = false;
                startPos = null;
            }

            void MouseUp(MouseUpEvent e)
            {
                if (downHeader == e.target && e.button == 0 && e.modifiers == EventModifiers.None)
                {
                    toggle.value = !toggle.value;
                    down = false;
                    startPos = null;
                }
            }

            void MouseMove(MouseMoveEvent e)
            {

                if (!down || !startPos.HasValue)
                    return;

                if (Vector2.Distance(e.localMousePosition, startPos.Value) < 10)
                    return;

                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new[] { toggle.userData as SceneCollection };
                DragAndDrop.StartDrag("SceneCollection");

            }

        }

        static void Collections()
        {

            if (!Profile.current)
                return;

            var elements = new List<(VisualElement header, VisualElement body)>();
            var i = 0;
            foreach (var collection in Profile.current.collections.OrderBy(Profile.current.Order).ToArray())
            {

                var element = CreateItem(collection, i, list,
                      addSceneAction: scenes =>
                      {

                          if (scenes.Any())
                              foreach (var scene in scenes)
                                  CreateNewScene(collection, scene);
                          else
                              collection.AddScene();

                      },
                      removeSceneAction: (c, index) => RemoveScene(c, index),
                      emptyMessage: "No scenes here, add some by using the + symbol above");

                if (element == null)
                    continue;

                var header = element.Q(className: "Collections-template-header");
                var body = element.Q("Collections-template-content");
                elements.Add((header, body));
                i += 1;

            }

            RoundedCornerHelper.Add(elements.ToArray());

            DragAndDropReorder.RegisterList(list,
                itemRootName: "collection-drag-root", dragButtonName: "collection-drag-button");

#if ASM_PLUGIN_ADDRESSABLES
            AdvancedSceneManager.Plugin.Addressables.Editor.UI.RefreshButtons();
#endif

        }

        static void DynamicCollections()
        {

            if (!Profile.current)
                return;

            var spacer = new VisualElement { name = "Collections-template-dynamic-collection-spacer" };
            spacer.EnableInClassList("hidden", !SceneManager.settings.local.displayDynamicCollections || Profile.current.dynamicCollections.Length == 0);

            list.Add(spacer);

            if (!SceneManager.settings.local.displayDynamicCollections)
                return;

            var elements = new List<(VisualElement header, VisualElement body)>();
            var i = 0;

            foreach (var collection in Profile.current.dynamicCollections)
            {

                if (collection == null)
                    continue;

                if (!collection.scenes.Any() && !collection.isStandalone)
                    continue;

                var c = ScriptableObject.CreateInstance<SceneCollection>();

                var title =
                    (collection.isStandalone ? "Standalone" : null) ??
                    (collection.isASM ? "Advanced Scene Manager defaults" : null) ??
                    (collection.title);

                var description =
                    (collection.isStandalone ? "Standalone scenes (and other dynamic collection scenes) are guaranteed to be included build, even if they are not contained in a normal collection." : null) ??
                    (collection.isASM ? "These are scenes that ASM provides out-of-the-box as a convinience, these are listed here to make sure they are included in build by default.\n\nIf you aren't using any of these, you may remove this dynamic collection in settings." : null) ??
                    (collection.title);

                c.m_title = title;
                c.scenes = collection.scenes.Select(s => Scene.Find(s)).ToArray();

                var element = CreateItem(c, i, list,
                      isReadOnly: true,
                      canRemoveCollection: false,
                      canAddScenes: collection.isStandalone,
                      canRemoveScenes: collection.isStandalone,
                      addSceneAction: scenes =>
                      {

                          if (scenes.Any())
                          {
                              //Drag and drop
                              foreach (var scene in scenes)
                                  Profile.current.Set(scene, true);
                          }
                          else
                          {
                              Profile.current.standalone.AddEmptySceneField();
                              SceneManagerWindow.Save(Profile.current);
                              SceneManagerWindow.Reload();
                          }

                      },
                      removeSceneAction: (c1, index) =>
                      {
                          Profile.current.standalone.RemoveSceneField(index);
                          SceneManagerWindow.Save(Profile.current);
                          SceneManagerWindow.Reload();
                      },
                      onSceneAssigned: (scene, index) =>
                      {
                          Profile.current.standalone.SetField(scene, index);
                          SceneManagerWindow.Save(Profile.current);
                          SceneManagerWindow.Reload();
                      },
                      emptyMessage: collection.isStandalone ? "No scenes here, + button or drag drop can be used to add some" : "No scenes here",
                      description: description);

                if (element == null)
                    continue;

                var header = element.Q(className: "Collections-template-header");
                var body = element.Q("Collections-template-content");
                elements.Add((header, body));
                i += 1;

            }

            RoundedCornerHelper.Add(elements.ToArray());

        }

        static VisualElement CreateItem(SceneCollection collection, int index, VisualElement list, bool isReadOnly = false, bool canRemoveCollection = true, bool canAddScenes = true, bool canRemoveScenes = true, Action<IEnumerable<Scene>> addSceneAction = null, Action<SceneCollection, int> removeSceneAction = null, string emptyMessage = null, string description = null, Action<Scene, int> onSceneAssigned = null)
        {

            if (BuildPipeline.isBuildingPlayer || !collection)
                return null;

            AddListener(collection);

            var res = Resources.Load<VisualTreeAsset>(collectionTemplate);
            if (!res)
                return null;
            var element = res.CloneTree();

            if (isReadOnly)
            {
                element.AddToClassList("isReadOnly");
                element.Q("collection-drag-button").style.display = DisplayStyle.None;
            }

            list.Add(element);

            var content = element.Q("Collections-template-content");
            if (isReadOnly)
                content.style.paddingRight = 4;

            var expander = element.Q<ToolbarToggle>("Collections-template-expander");
            var descriptionLabel = element.Q<Label>("Collections-template-description");

            #region Open buttons

            //Play button
            var button = element.Q<Button>("openPlay");
            button.EnableInClassList("hidden", isReadOnly || !SceneManager.settings.local.displayCollectionPlayButton);
            if (SceneManager.settings.local.displayCollectionPlayButton)
            {
                button.clickable.activators.Add(new ManipulatorActivationFilter() { modifiers = EventModifiers.Shift });
                button.clicked += () => Play(collection, Event.current?.shift ?? false);
                button.SetEnabled(collection.hasScenes);
            }

            //Open button
            button = element.Q<Button>("open");
            button.EnableInClassList("hidden", isReadOnly || !SceneManager.settings.local.displayCollectionOpenButton);
            if (SceneManager.settings.local.displayCollectionOpenButton)
            {

                button.style.unityFont = new StyleFont(Resources.Load<Font>("Fonts/Inter-Regular"));
                button.SetEnabled(collection.hasScenes);

                button.clickable.activators.Add(new ManipulatorActivationFilter() { modifiers = EventModifiers.Shift });
                button.clicked += () => Open(collection, forceOpenAllScenes: Event.current?.shift ?? false);

            }

            //Open additive button
            button = element.Q<Button>("openAdditive");
            button.EnableInClassList("hidden", isReadOnly || !SceneManager.settings.local.displayCollectionAdditiveButton);
            if (SceneManager.settings.local.displayCollectionAdditiveButton)
            {
                button.clickable.activators.Add(new ManipulatorActivationFilter() { modifiers = EventModifiers.Shift });
                button.SetEnabled(collection.hasScenes);
                button.clickable.clickedWithEventInfo += (e) => Open(collection, additive: true, forceOpenAllScenes: Event.current?.shift ?? false);
            }

            SceneManager.editor.scenesUpdated += UpdateAdditiveButton;
            EditorApplication.playModeStateChanged += _ => UpdateAdditiveButton();
            UpdateAdditiveButton();
            void UpdateAdditiveButton()
            {
                _ = button.SetEnabledExt(!Application.isPlaying && collection.hasScenes);
                button.text = IsOpen(collection) ? "-" : "+";
            }

            SceneField.onValueChanged -= OnSceneChanged;
            SceneField.onValueChanged += OnSceneChanged;

            void OnSceneChanged(object sender, (Scene oldValue, Scene newValue) e)
            {

                if (element == null)
                    return;

                if (!collection)
                    return;

                if (element.Q<Button>("openPlay") is Button button1)
                    button1.SetEnabled(collection.hasScenes);
                if (element.Q<Button>("openPlay") is Button button2)
                    button2.SetEnabled(collection.hasScenes);
                UpdateAdditiveButton();

            }

            #endregion
            #region Expander

            expander.SetValueWithoutNotify(IsExpanded(collection));
            element.Q(className: "Collections-template-header").AddManipulator(new HeaderManipulator(expander) { CanInitiateDragDrop = !isReadOnly });

            //Used to restore expanded state after reorder
            expander.userData = collection;

            _ = expander.RegisterValueChangedCallback(b => OnChecked());
            OnChecked();

            void OnChecked()
            {

                element.EnableInClassList("expanded", expander.value);
                element.Q(className: "Collections-template-header").EnableInClassList("expanded", expander.value);
                expander.text = expander.value ? "▼" : "►";
                _ = IsExpanded(collection, expander.value);
                content.EnableInClassList("hidden", !expander.value);
                UpdateDescription(expander.value);
                RoundedCornerHelper.Update();

                if (expander.value)
                {
                    CreateSceneItems(collection, content, isReadOnly, canAddScenes, canRemoveScenes, removeSceneAction, emptyMessage, onSceneAssigned);
                    if (!isReadOnly)
                        DragAndDropReorder.RegisterList(content, dragButtonName: "scene-drag-button", itemRootName: "scene-drag-root");
                }
                else
                {
                    ClearSceneItems(content);
                    if (!isReadOnly)
                        DragAndDropReorder.UnregisterList(content);
                }

            }

            #endregion
            #region Title

            RefreshStartup(element.Q("Collection-template-header-Startup"), collection, index);

            var label = element.Q<Label>("Collection-template-header-Label");

            ReloadTitle();
            void ReloadTitle(string preview = null)
            {
                label.text = string.IsNullOrEmpty(preview)
                    ? collection ? collection.title : ""
                    : preview;
                TrimUtility.TrimLabel(label, label.text, MaxLabelWidth, enableAuto: true);
            }

            float MaxLabelWidth() =>
                element.Q(name: "Extra-Buttons").localBound.x - label.localBound.x;

            #endregion
            #region Menu / Add / Remove

            if (canAddScenes)
                element.Q<Button>("Collection-template-header-Add").clicked += () =>
                {
                    if (addSceneAction != null)
                        addSceneAction.Invoke(Array.Empty<Scene>());
                    else
                        CreateNewScene(collection);
                };
            else
                element.Q<Button>("Collection-template-header-Add").style.display = DisplayStyle.None;

            if (canRemoveCollection)
                element.Q<Button>("Collection-template-header-Remove").clicked += () => RemoveCollection(collection);
            else
                element.Q<Button>("Collection-template-header-Remove").style.display = DisplayStyle.None;

            if (!isReadOnly)
            {

                //Edit collection popup
                var menuButton = element.Q<ToolbarToggle>(name: "settingsButton");
                menuButton.style.unityFont = new StyleFont(Resources.Load<Font>("Fonts/Inter-Regular"));
                _ = menuButton.RegisterValueChangedCallback(e =>
                  EditCollectionPopup.Open(
                      placementTarget: menuButton,
                      parent: SceneManagerWindow.window,
                      alignRight: true,
                      offset: new Vector2(0, -3)
                      ).Refresh(collection, ReloadTitle, RefreshStartup));

            }
            else
                element.Q<ToolbarToggle>(name: "settingsButton").style.display = DisplayStyle.None;

            #endregion
            #region Include in build

            if (element.Q<Toggle>("toggleIncludeInBuild") is Toggle toggle)
            {

                toggle.style.display = !isReadOnly && SceneManager.settings.project.allowExcludingCollectionsFromBuild ? DisplayStyle.Flex : DisplayStyle.None;

                toggle.SetValueWithoutNotify(collection.isIncluded);
                _ = toggle.RegisterValueChangedCallback(e =>
                {
                    collection.isIncluded = e.newValue;
                    BuildUtility.UpdateSceneList();
                    RefreshStartup();
                });

            }

            #endregion
            #region Drop template

            if (!isReadOnly)
            {

                element.RegisterCallback<DragEnterEvent>(e =>
                {

                    if (DragAndDrop.objectReferences.OfType<SceneCollectionTemplate>().Any())
                    {
                        DragAndDrop.AcceptDrag();
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        Selection.Select(collection);
                    }

                });

                element.RegisterCallback<DragUpdatedEvent>(e =>
                {
                    if (!DragAndDrop.objectReferences.OfType<SceneCollectionTemplate>().Any())
                        return;
                    DragAndDrop.AcceptDrag();
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                });

                SceneManagerWindow.window.rootVisualElement.UnregisterCallback<DragPerformEvent>(e => CancelApplyTemplate());
                SceneManagerWindow.window.rootVisualElement.RegisterCallback<DragPerformEvent>(e => CancelApplyTemplate());
                element.RegisterCallback<DragLeaveEvent>(e => CancelApplyTemplate());

                element.RegisterCallback<DragPerformEvent>(e =>
                {
                    ApplyTemplate(collection, DragAndDrop.objectReferences.OfType<SceneCollectionTemplate>().FirstOrDefault());
                    CancelApplyTemplate();
                });

                void CancelApplyTemplate()
                {

                    if (DragAndDrop.objectReferences.OfType<SceneCollectionTemplate>().Any())
                        Selection.Unselect(collection);

                }

            }

            #endregion
            #region Drop area

            if (canAddScenes)
            {

                var dropElement = new VisualElement();
                dropElement.style.backgroundColor = new StyleColor(new Color32(0, 122, 163, 255));
                dropElement.style.height = 22;
                dropElement.style.marginBottom = 10;
                dropElement.EnableInClassList("hidden", true);
                dropElement.style.paddingTop = 3;
                dropElement.style.unityTextAlign = TextAnchor.MiddleCenter;
                dropElement.style.alignContent = Align.Center;

                var l = new Label("+");

                dropElement.Add(l);

                element.Add(dropElement);

                var scenes = new List<Scene>();

                element.RegisterCallback<DragEnterEvent>(e =>
                {

                    content.style.marginBottom = 0;

                    scenes.Clear();
                    scenes.AddRange(DragAndDrop.objectReferences.OfType<Scene>());
                    scenes.AddRange(DragAndDrop.objectReferences.OfType<SceneAsset>().Select(s => s.FindASMScene()));
                    scenes.AddRange(DragAndDrop.paths.Select(path => AssetDatabase.LoadAssetAtPath<SceneAsset>(path.Replace('\\', '/').Replace(Application.dataPath, ""))).OfType<SceneAsset>().Select(s => s.FindASMScene()));

                    scenes = scenes.Where(s => s).GroupBy(s => s.path).Select(g => g.First()).ToList();

                    if (scenes.Any())
                    {
                        dropElement.EnableInClassList("hidden", false);
                        DragAndDrop.AcceptDrag();
                        DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                    }

                });

                SceneManagerWindow.window.rootVisualElement.UnregisterCallback<DragPerformEvent>(e => CancelDropArea());
                SceneManagerWindow.window.rootVisualElement.RegisterCallback<DragPerformEvent>(e => CancelDropArea());
                element.RegisterCallback<DragLeaveEvent>(e => CancelDropArea());

                dropElement.RegisterCallback<DragUpdatedEvent>(e =>
                {
                    DragAndDrop.AcceptDrag();
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                });

                dropElement.RegisterCallback<DragPerformEvent>(e =>
                {
                    if (canAddScenes)
                        addSceneAction?.Invoke(scenes);
                    CancelDropArea();
                });

                void CancelDropArea()
                {

                    scenes.Clear();
                    content.style.marginBottom = 10;
                    dropElement.EnableInClassList("hidden", true);

                }

                var hadDragAndDrop = false;
                OnGUIEvent -= ScenesTab_OnGUIEvent;
                OnGUIEvent += ScenesTab_OnGUIEvent;

                void ScenesTab_OnGUIEvent()
                {
                    var isDragAndDrop = DragAndDrop.objectReferences.Any();
                    if (hadDragAndDrop && !isDragAndDrop)
                        CancelDropArea();
                    hadDragAndDrop = isDragAndDrop;
                }

            }

            #endregion
            #region Description

            descriptionLabel.text = description;
            UpdateDescription(expander.value);

            void UpdateDescription(bool expanded) =>
                descriptionLabel.EnableInClassList("hidden", !expanded || string.IsNullOrEmpty(description));

            #endregion

            if (!isReadOnly)
            {

                element.Q(className: "Collections-template-header").AddManipulator(new Selection.Manipulator(collection, selectionVisual: element.Q(className: "Collections-template-header")));
                element.Q(className: "Collections-template-header").AddManipulator(new ContextualMenuManipulator(e => Menu(e, collection: collection, i: Profile.current.Order(collection))));

                ExtraButtons(collection, element.Q(name: "Extra-Buttons"));
                element.SetLocked(AssetDatabase.GetAssetPath(collection));

            }

            return element;

        }

        static bool IsExpanded(SceneCollection collection, bool? expanded = null)
        {

            var key = collection.name;
            if (string.IsNullOrEmpty(key))
                key = collection.title;

            if (expanded.HasValue)
                _ = SceneManagerWindow.window.openCollectionExpanders.Set(key, expanded.Value);
            return SceneManagerWindow.window.openCollectionExpanders.GetValue(key);

        }

        #region Startup

        static void RefreshStartup()
        {
            list.Query("Collection-template-header-Startup").ForEach(star =>
            {

                var (collection, index) = ((SceneCollection collection, int index))star.userData;

                if (collection)
                    RefreshStartup(star, collection, index);

            });
        }

        static void RefreshStartup(VisualElement startup, SceneCollection collection, int index)
        {

            var collections = Profile.current.StartupCollections();
            var isImplicit = collections.Count() == 1 && collections.ElementAt(0).startupOption == CollectionStartupOption.DoNotOpen && collection == collections.ElementAt(0);

            if (collections.Contains(collection))
            {

                startup.EnableInClassList("hidden", false);
                startup.tooltip = isImplicit
                    ? "This collection will open on startup, since no collection is explicitly flagged to do so."
                    : "This collection will open on startup.";

            }
            else
                startup.EnableInClassList("hidden", true);

            startup.userData = (collection, index); //Enables us to use RefreshStars()
            startup.pickingMode = PickingMode.Ignore;

        }

        #endregion

        static void ApplyTemplate(SceneCollection collection, SceneCollectionTemplate template)
        {

            if (!template || !collection)
                return;

            if (EditorUtility.DisplayDialog("Applying template...", $"Are you sure you want to apply the template '{template.title}' to '{collection.title}'?", ok: "Cancel", cancel: "Apply"))
                return;

            template.Apply(collection);
            AssetDatabase.SaveAssets();
            Reload();

        }

        #endregion
        #region Create scene elements

        static void CreateSceneItems(SceneCollection collection, VisualElement scenesList, bool isReadOnly = false, bool canAddScenes = true, bool canRemoveScenes = true, Action<SceneCollection, int> removeAction = null, string emptyMessage = null, Action<Scene, int> onSceneAssigned = null)
        {

            var scenes = collection.scenes;
            for (var i = 0; i < scenes.Length; i++)
                _ = CreateItem(collection, scenes[i], i, scenesList, SceneManagerWindow.window, label: "", isReadOnly, canAddScenes, canRemoveScenes, removeAction, onSceneAssigned);

            if (!scenes.Any())
            {
                var label = new Label(emptyMessage);
                label.AddToClassList("text");
                label.style.alignSelf = Align.Center;
                label.style.fontSize = 13;
                label.style.SetMargin(vertical: 4);
                scenesList.Add(label);
            }

        }

        static void ClearSceneItems(VisualElement scenesList) =>
            scenesList?.Clear();

        internal static VisualElement CreateItem(SceneCollection collection, Scene scene, int index, VisualElement scenesList, IUIToolkitEditor parent, string label = "", bool isReadOnly = false, bool canAddScenes = true, bool canRemoveScenes = true, Action<SceneCollection, int> removeAction = null, Action<Scene, int> onSceneAssigned = null)
        {

            if (!canAddScenes && isReadOnly && !scene)
                return null;

            if (scenesList == null)
                return null;

            AddListener(scene);

            var resource = Resources.Load<VisualTreeAsset>(sceneTemplate);
            if (!resource)
                return null;
            var element = resource.CloneTree();
            scenesList.Add(element);

            //Used on reorder to get assets
            if (isReadOnly)
                element.Q(name: "scene-drag-button").style.display = DisplayStyle.None;
            element.Q(name: "scene-drag-root").userData = (collection, scene);

            if (!isReadOnly && (UnityEngine.Object)parent == SceneManagerWindow.window)
            {
                element.Q(name: "scene-drag-root").AddManipulator(new Selection.Manipulator(collection, index));
                element.Q(name: "scene-drag-root").AddManipulator(new ContextualMenuManipulator(e => Menu(e, collection, index, isScene: true)));
            }

            var sceneField = element.Q<SceneField>("sceneField");
            sceneField.Collection = collection;

            sceneField.EnableInClassList("hidden", false);

            sceneField.label = label;
            sceneField.isReadOnly = !(canAddScenes && collection);
            _ = sceneField.SetValueWithoutNotify(scene);

            if (!isReadOnly || canAddScenes)
                sceneField.RegisterValueChangedCallback(e =>
                {

                    if (onSceneAssigned != null)
                        onSceneAssigned.Invoke(e.newValue, index);
                    else
                    {

                        var scenes = collection.scenes;
                        scenes[index] = e.newValue;
                        collection.scenes = scenes;
                        Save(collection);

                        if (!isReadOnly)
                            SetTag(element, parent, e.newValue, collection);

                        ReloadNewButton(e.newValue);

                    }

                });

            SetTag(element, parent, scene, collection, isReadOnly);

            if (removeAction != null && canRemoveScenes)
                element.Q<Button>("Scene-template-header-Remove").clicked += () => removeAction?.Invoke(collection, index);
            else
                element.Q<Button>("Scene-template-header-Remove").style.display = DisplayStyle.None;

            if (isReadOnly)
                element.Q<Button>(className: "NewScene").style.display = DisplayStyle.None;
            else
            {

                element.Q<Button>(className: "NewScene").clicked += () => CreateNewScene(collection, index);

                ReloadNewButton(scene);

                element.SetLocked(AssetDatabase.GetAssetPath(collection));

            }

            ExtraButtons(scene, element.Q(name: "Extra-Buttons"));
#if ASM_PLUGIN_ADDRESSABLES
            AdvancedSceneManager.Plugin.Addressables.Editor.UI.RefreshButtons();
#endif

            void ReloadNewButton(Scene s) =>
                element.Q<Button>(className: "NewScene").EnableInClassList("hidden", s);

            return element;

        }

        static void SetTag(VisualElement element, IUIToolkitEditor parent, Scene scene, SceneCollection collection, bool isReadOnly = false)
        {

            if (element.Q<Button>(className: "LayerDropDown") is Button dropdown)
            {

                dropdown.EnableInClassList("hidden", isReadOnly || !scene);
                if (!scene || isReadOnly)
                    return;

                void UpdateMenu()
                {
                    var current = collection.Tag(scene);
                    dropdown.text = current.name;
                }

                if (collection == null)
                    return;

                var tag = collection.Tag(scene);
                dropdown.TrimLabel(tag.name, () => dropdown.resolvedStyle.width - 12, enableAuto: true);

                dropdown.clicked += () =>
                    PickTagPopup.Open(dropdown, parent, alignRight: true).
                    Refresh(tag, onSelected: layer =>
                    {
                        _ = collection.Tag(scene, setTo: layer);
                        SceneManagerWindow.Save(collection);
                        UpdateMenu();
                    });

                UpdateMenu();

            }

        }

        #endregion
        #region Context menu

        static void Menu(ContextualMenuPopulateEvent e, SceneCollection collection = null, int i = 0, bool isScene = false)
        {

            Selection.ClearWhenGUIReturns();

            var collections = Selection.collections;
            if (!isScene)
                collections = collections.Concat(new[] { collection }).Distinct();

            var scenes = Selection.scenes;
            if (isScene)
                scenes = scenes.Concat(new[] { (collection, i) }).Distinct();

            var collectionPath = $"{collections?.Count()} collections" + "/";
            var scenePath = $"{scenes?.Count()} scenes" + "/";

            var isMultiple = scenes.Count() > 1 || collections.Count() > 1 || (collections.Any() && scenes.Any());

            CollectionMenu(isMultiple ? collectionPath : null, e, collections, i);
            SceneMenu(isMultiple ? scenePath : null, e, scenes, i);

        }

        static bool respectDoNotOpenTag
        {
            get => SceneManager.settings.local.respectDoNotOpenTag;
            set => SceneManager.settings.local.respectDoNotOpenTag = value;
        }

        static void CollectionMenu(string path, ContextualMenuPopulateEvent e, IEnumerable<SceneCollection> collections, int clickedIndex)
        {

            if (!collections.Any())
                return;

            collections = collections.Distinct().OrderBy(Profile.current.Order);

            #region Register MenuItems

            e.menu.AppendAction(path + "Open/Open...", _ => Open(additive: false), status: ShowIf(IsSingleSelected()));
            e.menu.AppendAction(path + "Open/Open additive (edit mode only)...", _ => Open(additive: true));
            e.menu.AppendAction(path + "Open/Open in play mode...", _ => Open(playMode: true), status: ShowIf(IsSingleSelected()));
            e.menu.AppendSeparator(path + "Open/");

            e.menu.AppendAction(path + "Open/Respect DoNotOpen tag (only applies to this menu)", a => respectDoNotOpenTag = !respectDoNotOpenTag, status: respectDoNotOpenTag ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            e.menu.AppendSeparator(path + "");
            e.menu.AppendAction(path + "Push up", status: ShowIf(CanPushUp()), action: _ => Push(up: true));
            e.menu.AppendAction(path + "Push down", status: ShowIf(CanPushDown()), action: _ => Push(up: false));

            e.menu.AppendSeparator(path + "");
            e.menu.AppendAction(path + "Create template...", _ => CreateTemplate(), status: ShowIf(IsSingleSelected()));

            e.menu.AppendSeparator(path + "");
            e.menu.AppendAction(path + "Remove", _ => Remove());

            #endregion
            #region Conditions

            DropdownMenuAction.Status ShowIf(bool value) =>
                value ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;

            bool IsSingleSelected() =>
                collections.Count() < 2;

            bool CanPushUp() =>
                Profile.current.Order(collections.FirstOrDefault()) > 0;

            bool CanPushDown() =>
                Profile.current.Order(collections.LastOrDefault()) < Profile.current.collections.Count() - 1;

            #endregion
            #region OnClick

            void Open(bool additive = false, bool playMode = false)
            {
                if (playMode)
                    Play(collections.ElementAt(0), forceOpenAllScenes: !respectDoNotOpenTag);
                else
                    foreach (var c in collections)
                        ScenesTab.Open(c, additive, forceOpenAllScenes: !respectDoNotOpenTag);
            }

            void Push(bool up)
            {

                var collectionsWithIndexes = collections.Select(c => (c, index: Profile.current.Order(c))).ToArray();

                //Get either clickedIndex, or index of top/bottom-most item
                clickedIndex = up
                    ? Mathf.Min(clickedIndex, collectionsWithIndexes.Min(s => s.index))
                    : Mathf.Max(clickedIndex, collectionsWithIndexes.Max(s => s.index)) - 1;
                Debug.Log(clickedIndex);

                var i = up
                     ? clickedIndex - 1
                     : clickedIndex + 1;

                foreach (var collection in collections)
                    _ = Profile.current.m_collections.Remove(collection);

                i = Mathf.Clamp(i, 0, Profile.current.m_collections.Count);
                Profile.current.m_collections.InsertRange(i, collections.Reverse());

                Reload();

            }

            void CreateTemplate() =>
                SceneCollectionTemplate.CreateTemplateInCurrentFolder(collections.ElementAt(0));

            void Remove()
            {

                if (!EditorUtility.DisplayDialog(
                     title: $"Deleting {collections.Count()} collection(s)...",
                     message: "Are you sure you wish to remove the following collections?\n\n" + string.Join("\n", collections.Select(c => c.title)),
                     ok: "Yes",
                     cancel: "Cancel",
                     dialogOptOutDecisionType: DialogOptOutDecisionType.ForThisSession,
                     dialogOptOutDecisionStorageKey: "AdvancedSceneManager.DeleteCollections"))
                    return;

                foreach (var c in collections)
                    RemoveCollection(c);

            }

            #endregion

        }

        static void SceneMenu(string path, ContextualMenuPopulateEvent e, IEnumerable<(SceneCollection collection, int i)> scenes, int clickedIndex)
        {

            if (!scenes.Any())
                return;

            scenes = scenes.Distinct().OrderBy(s => s.collection).ThenBy(s => Array.IndexOf(s.collection.scenes, s));

            //The following can only be used if IsAllSameCollection()
            SceneCollection collection = null;
            int? firstIndex = null;
            int? lastIndex = null;
            if (IsAllSameCollection())
            {
                collection = scenes.Select(s => s.collection).FirstOrDefault();
                firstIndex = scenes.Min(s => s.i);
                lastIndex = scenes.Max(s => s.i);
            }

            #region Register MenuItems

            e.menu.AppendAction(path + "Open...", status: ShowIf(IsSingleSelected() && HasValue()), action: _ => Open(additive: false));
            e.menu.AppendAction(path + "Open additive...", status: ShowIf(HasValue()), action: _ => Open(additive: true));

            e.menu.AppendSeparator(path + "");
            e.menu.AppendAction(path + "Bake lightmaps", status: ShowIf(IsMultipleValidSelected()), action: _ => BakeLightmaps());
            e.menu.AppendAction(path + "Combine", status: ShowIf(IsMultipleValidSelected()), action: _ => CombineScenes());

            e.menu.AppendSeparator(path + "");
            e.menu.AppendAction(path + "Push up", status: ShowIf(CanPushUp()), action: _ => Push(up: true));
            e.menu.AppendAction(path + "Push down", status: ShowIf(CanPushDown()), action: _ => Push(up: false));
            e.menu.AppendSeparator(path + "");

            e.menu.AppendAction(path + "Remove", _ => Remove());

            #endregion
            #region Conditions

            DropdownMenuAction.Status ShowIf(bool value) =>
                value ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;

            bool IsSingleSelected() =>
                scenes.Count() == 1;

            bool IsMultipleValidSelected() =>
                scenes.Where(s => !string.IsNullOrWhiteSpace(s.Path())).Count() > 1;

            bool IsAllSameCollection() =>
                scenes.GroupBy(s => s.collection).Count() == 1;

            bool CanPushUp() =>
                IsAllSameCollection() && (!IsSingleSelected() || firstIndex != 0);

            bool CanPushDown() =>
                IsAllSameCollection() && (!IsSingleSelected() || lastIndex != collection.m_scenes.Length - 1);

            bool HasValue() =>
                scenes.Any(s => !string.IsNullOrWhiteSpace(s.Path()));

            #endregion
            #region OnClick

            void Open(bool additive)
            {
                if (!additive)
                    scenes.ElementAt(0).Open(additive: false); //Can only be used if IsSingleSelected()
                else
                    foreach (var scene in scenes)
                        scene.Open(additive: true);
            }

            void Push(bool up)
            {

                var count = scenes.Count();

                if (!IsAllSameCollection())
                    return;

                //Get either clickedIndex, or index of top/bottom-most item
                clickedIndex = up
                    ? Mathf.Min(clickedIndex, scenes.Min(s => s.i))
                    : Mathf.Max(clickedIndex, scenes.Max(s => s.i)) - (count > 1 ? 1 : 0);

                var index = up
                    ? clickedIndex - 1
                    : clickedIndex + 1;

                var scenesWithIndex = collection.m_scenes.Select((s, i) => (s, i)); //Get index for each item
                var scenesWithIndexWithoutMoved = scenesWithIndex.Where(s => !scenes.Any(s1 => s1.i == s.i)); //Remove indexes that are to be moved
                var newScenes = scenesWithIndexWithoutMoved.Select(s => s.s).ToList(); //List contains InsertRange()

                index = Mathf.Clamp(index, 0, newScenes.Count); //Make sure we don't go outside bounds

                newScenes.InsertRange(index, scenes.Select(Path).Reverse()); //Insert items at new index
                collection.m_scenes = newScenes.ToArray(); //Set array back to collection

                foreach (var scene in scenes)
                    Selection.Unselect(collection, scene.i);

                for (var i = 0; i < count; i++)
                    Selection.Select(collection, index + i);

                Reload();

            }

            void Remove()
            {

                if (scenes.All(s => string.IsNullOrEmpty(s.Path())) || EditorUtility.DisplayDialog(
                    title: $"Deleting {scenes.Count()} scenes(s)...",
                    message: "Are you sure you wish to remove the following scenes?\n\n" + string.Join("\n", scenes.Select(s => s.Path()).Where(s => !string.IsNullOrWhiteSpace(s))),
                    ok: "Yes",
                    cancel: "Cancel",
                    dialogOptOutDecisionType: DialogOptOutDecisionType.ForThisSession,
                    dialogOptOutDecisionStorageKey: "AdvancedSceneManager.DeleteScenes"))
                {

                    foreach (var c in scenes.GroupBy(s => s.collection))
                    {

                        var collectionScenes = c.Key.scenes.Select((s, i) => (s, i)); //Get index for each items
                        var newScenes = collectionScenes.Where(s => !c.Any(s1 => s1.i == s.i)); //Get all items except for matching indecies
                        c.Key.scenes = newScenes.Select(s => s.s).ToArray(); //Set new list

                        Save(c.Key);

                    }

                    PopulateList();

                }

            }

            void BakeLightmaps() =>
                Lightmapping.BakeMultipleScenes(scenes.Select(s => s.Path()).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray());

            void CombineScenes() =>
                SceneUtility.MergeScenes(scenes.Select(s => s.Path()).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray());

            #endregion

        }

        public static void Open(this (SceneCollection collection, int i) scene, bool additive) =>
            SceneField.OpenScene(scene.collection[scene.i], additive);

        public static string Path(this (SceneCollection collection, int i) scene)
        {
            var s = scene.collection[scene.i];
            return s ? s.path : "";
        }

        #endregion
        #region Create collection / scene assets

        static void CreateNewCollection() =>
            SceneCollectionUtility.Create("New Collection");

        static void CreateNewCollection(SceneCollectionTemplate template) =>
            template.CreateCollection(Profile.current);

        static void RemoveCollection(SceneCollection collection) =>
            Profile.current.Remove(collection);

        static void CreateNewScene(SceneCollection collection, int index)
        {
            if (SceneManager.settings.local.useSaveDialogWhenCreatingScenesFromSceneField)
                SceneUtility.Create(onCreated: null, collection, index, replaceIndex: true, save: false);
            else
                SceneUtility.CreateInCurrentFolder(onCreated: null, collection, index, replaceIndex: true, save: false);
            Save(collection);
            PopulateList();
        }

        static void CreateNewScene(SceneCollection collection, Scene scene = null)
        {
            var scenes = collection.scenes;
            ArrayUtility.Add(ref scenes, scene);
            collection.scenes = scenes;
            Save(collection);
            _ = IsExpanded(collection, true);
            PopulateList();
        }

        static void RemoveScene(SceneCollection collection, int index)
        {
            var scenes = collection.scenes;
            ArrayUtility.RemoveAt(ref scenes, index);
            collection.scenes = scenes;
            Save(collection);
            PopulateList();
        }

        #endregion
        #region Open

        static void Play(SceneCollection collection, bool forceOpenAllScenes) =>
            SceneManager.runtime.Start(collection, forceOpenAllScenes, playSplashScreen: false);

        static void Open(SceneCollection collection, bool additive = false, bool forceOpenAllScenes = false)
        {

            if (Application.isPlaying)
            {
                if (collection.IsOpen())
                    _ = collection.Close();
                else
                    _ = collection.Open();
            }
            else if (!additive)
                SceneManager.editor.Open(collection, forceOpenAllScenes: forceOpenAllScenes);
            else if (SceneManager.editor.IsOpen(collection))
            {
                if (SceneManager.editor.CanClose(collection))
                    SceneManager.editor.Close(collection);
            }
            else
                SceneManager.editor.Open(collection, additive: true, forceOpenAllScenes: forceOpenAllScenes);

        }

        static bool IsOpen(SceneCollection collection) =>
            !Application.isPlaying && SceneManager.editor.IsOpen(collection);

        #endregion
        #region Plugin support

        //Used by plugins.asm.locking and plugins.asm.addressables

        internal delegate VisualElement ExtraCollectionButton(SceneCollection collection);
        internal delegate VisualElement ExtraSceneButton(Scene scene);

        static readonly Dictionary<ExtraCollectionButton, (int? position, bool isLockable)> extraCollectionButtons = new Dictionary<ExtraCollectionButton, (int? position, bool isLockable)>();
        static readonly Dictionary<ExtraSceneButton, (int? position, bool isLockable)> extraSceneButtons = new Dictionary<ExtraSceneButton, (int? position, bool isLockable)>();
        internal static void AddExtraButton(ExtraCollectionButton callback, int? position = null, bool isLockable = true)
        {
            if (!extraCollectionButtons.ContainsKey(callback))
                extraCollectionButtons.Add(callback, (position, isLockable));
        }

        internal static void RemoveExtraButton(ExtraCollectionButton callback) =>
            extraCollectionButtons.Remove(callback);

        internal static void AddExtraButton(ExtraSceneButton callback, int? position = null, bool isLockable = true)
        {
            if (!extraSceneButtons.ContainsKey(callback))
                extraSceneButtons.Add(callback, (position, isLockable));
        }

        internal static void RemoveExtraButton(ExtraSceneButton callback) =>
            extraSceneButtons.Remove(callback);

        static void ExtraButtons(SceneCollection collection, VisualElement panel)
        {
            foreach (var callback in extraCollectionButtons.OrderBy(e => e.Value.position).ToArray())
                if (callback.Key?.Invoke(collection) is VisualElement element)
                {
                    panel.Add(element);
                    element.EnableInClassList("lockable", callback.Value.isLockable);
                }
        }

        static void ExtraButtons(Scene scene, VisualElement panel)
        {
            foreach (var callback in extraSceneButtons.OrderBy(e => e.Value.position).ToArray())
                if (callback.Key?.Invoke(scene) is VisualElement element)
                {
                    panel.Add(element);
                    element.EnableInClassList("lockable", callback.Value.isLockable);
                }
        }

        #endregion
        #region Footer

        static FooterItem ExtraAddMenuButton = FooterItem.Create().OnRight().Button("⋮", ExtraAddMenu, setup: b => { b.style.fontSize = 14; b.style.SetPadding(2); b.style.borderLeftWidth = b.style.borderRightWidth = b.style.borderTopWidth = b.style.borderBottomWidth = 0; });

        public static FooterItem[] FooterButtons { get; } = new FooterItem[]
        {
            FooterItem.Create().OnLeft().Button("Bake lightmaps", () => Lightmapping.BakeMultipleScenes(Selection.scenes.Select(s => s.Path()).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray())).Hidden(),
            FooterItem.Create().OnLeft().Button("Combine scenes", () => SceneUtility.MergeScenes(Selection.scenes.Select(s => s.Path()).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray())).Hidden(),
            FooterItem.Create().OnRight().Button("New collection", () => CreateNewCollection()),
            ExtraAddMenuButton,
        };

        static void RefreshFooterButtons()
        {
            _ = FooterButtons[0].Visible(Selection.scenes.Count() > 1);
            _ = FooterButtons[1].Visible(Selection.scenes.Count() > 1);
        }

        static void ExtraAddMenu()
        {

            var menu = new GenericMenu();

            var templates = AssetDatabase.FindAssets("t:SceneCollectionTemplate").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<SceneCollectionTemplate>).ToArray();
            GUI.enabled = true;

            menu.AddDisabledItem(new GUIContent("Create from template:"));
            foreach (var template in templates)
                menu.AddItem(new GUIContent(template.title + (templates.Count(t => t.title == template.title) > 1 ? $" ({template.name})" : "")), false, () => CreateNewCollection(template));

            menu.ShowAsContext();

        }

        #endregion
        #region Reorder

        public static void OnReorderStart(DragAndDropReorder.DragElement element)
        {
            var isCollectionsList = element.list == list;
            if (isCollectionsList)
                list.Query<ToolbarToggle>("Collections-template-expander").ForEach(e =>
                {
                    if (e.value)
                        _ = SceneManagerWindow.window.openCollectionExpanders.Set((e.userData as SceneCollection).name, true);
                    e.value = false;
                });
        }

        public static void OnReorderEnd(DragAndDropReorder.DragElement element, int newIndex)
        {

            var isCollectionsList = element.list == list;

            if (isCollectionsList)
            {
                var l = Profile.current.collections.ToList();
                var item = l[element.index];
                _ = Profile.current.Order(item, newIndex);
                Save(Profile.current);
            }
            else //isSceneList
            {

                if (element.item.userData == null)
                    return;

                (var collection, _) = ((SceneCollection collection, Scene scene))element.item.userData;

                var l = collection.scenes.ToList();
                var item = l[element.index];
                l.RemoveAt(element.index);
                l.Insert(newIndex, item);
                collection.scenes = l.ToArray();

                Save(collection);

            }

        }

        #endregion
        #region PropertyChanged

        static Timer timer;
        static void OnPropertyChanged(object sender, EventArgs e)
        {
            if (timer == null)
            {
                timer = new Timer(500);
                timer.Elapsed += (s, e1) =>
                {
                    timer.Stop();
                    PopulateList();
                };
            }
            timer.Stop();
            timer.Start();
        }

        static void AddListener(INotifyPropertyChanged obj)
        {
            if (obj == null)
                return;
            obj.PropertyChanged -= OnPropertyChanged;
            obj.PropertyChanged += OnPropertyChanged;
            listeners.Add(obj);
        }

        static readonly List<INotifyPropertyChanged> listeners = new List<INotifyPropertyChanged>();
        static void ClearListeners()
        {
            listeners.ForEach(l => l.PropertyChanged -= OnPropertyChanged);
            listeners.Clear();
        }

        #endregion

    }

}

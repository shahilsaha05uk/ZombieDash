#pragma warning disable IDE0051 // Remove unused private members

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Editor.Window;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Setup;
using AdvancedSceneManager.Utility;
using Lazy.Utility;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using static AdvancedSceneManager.Editor.GenericPopup;

namespace AdvancedSceneManager.Editor
{

    public partial class SceneManagerWindow : EditorWindow_UIElements<SceneManagerWindow>, IUIToolkitEditor
    {

        public enum Tab
        {
            Scenes, Tags, Settings, NoProfile, Welcome, Trash
        }

        readonly Dictionary<Tab, (Type type, string path, bool showInheader)> tabs = new Dictionary<Tab, (Type type, string path, bool showInheader)>()
        {
            {Tab.Scenes,    (typeof(ScenesTab),             $"AdvancedSceneManager/Tabs/{Tab.Scenes}/Tab",      showInheader: true) },
            {Tab.Tags,      (typeof(TagsTab),               $"AdvancedSceneManager/Tabs/{Tab.Tags}/Tab",        showInheader: true) },
            {Tab.Settings,  (typeof(Window.SettingsTab),    $"AdvancedSceneManager/Tabs/{Tab.Settings}/Tab",    showInheader: true) },
            {Tab.NoProfile, (typeof(NoProfileTab),          $"AdvancedSceneManager/Tabs/{Tab.NoProfile}/Tab",   showInheader: false) },
            {Tab.Welcome,   (typeof(WelcomeTab),            $"AdvancedSceneManager/Tabs/{Tab.Welcome}/Tab",     showInheader: false) },
            {Tab.Trash,     (typeof(TrashTab),              $"AdvancedSceneManager/Tabs/{Tab.Trash}/Tab",       showInheader: false) },
        };

        [SerializeField] internal Tab tab;
        [SerializeField] internal SerializableStringBoolDict openCollectionExpanders = new SerializableStringBoolDict();
        [SerializeField] internal SerializableStringBoolDict openTagExpanders = new SerializableStringBoolDict();
        [SerializeField] internal SerializableStringBoolDict openSettingHeaders = new SerializableStringBoolDict();
        [SerializeField] private float scroll;

        public static event Action OnGUIEvent;
        public static event Action MouseUp;

        public static new Rect position;

        static void OnMouseUp(MouseUpEvent e) =>
            MouseUp?.Invoke();

        /// <summary>Gets if editor is using dark mode.</summary>
        public static bool IsDarkMode =>
            EditorGUIUtility.isProSkin;

        void OnGUI()
        {

            if (!window)
                return;

            if (!isMainContentLoaded || !window)
                return;

            //Tab logic is contained within static classes, some of them needs access to OnGUI, to check input, for example
            OnGUIEvent?.Invoke();

            if (!InternalEditorUtility.isApplicationActive)
                return;

            position = ((EditorWindow)window).position;

            //Invoke OnGUI() on tab
            _ = InvokeTab();

            //Repaint constantly if drag and drop operation is ongoing
            if (DragAndDropReorder.currentDragElement != null)
                Repaint();

        }

        [MenuItem("File/Scene Manager... %#m", priority = 205)]
        [MenuItem("Tools/Advanced Scene Manager/Window/Scene Manager Window", priority = 40)]
        public static void MenuItem() =>
            Open();

        #region Constructor and overrides

        const string windowPath = "AdvancedSceneManager/SceneManagerWindow";

        public override string path => windowPath;
        public override bool autoReloadOnWindowFocus => false;

        static SceneManagerWindow()
        {
            DragAndDropReorder.OnDragStarted += OnDragStarted;
            DragAndDropReorder.OnDragEnded += OnDragEnded;
            DragAndDropReorder.OnDragCancel += OnDragCancel;

            Profile.onProfileChanged += Reload;

            AssetUtility.onAssetsSaved += OnAssetsSave;

            AssetUtility.onAssetsChanged += () =>
            {
                if (!Application.isPlaying)
                    Reload();
            };

        }

        #endregion
        #region Reload / OnEnable, OnDisable

        internal static void Initialize()
        {

            if (window)
                Reload();

            if (!Setup.ASM.isSetup)
                EditorApplication.delayCall += Open;

        }

        public override void OnEnable()
        {

            if (!window)
                return;

            SceneManagerWindowProxy.requestSave -= SceneManagerWindowProxy_requestSave;
            SceneManagerWindowProxy.requestSave += SceneManagerWindowProxy_requestSave;

            base.OnEnable();

            LoadDefaultStyle();

            DragAndDropReorder.rootVisualElement = rootVisualElement;

            JsonUtility.FromJsonOverwrite(SceneManager.settings.local.sceneManagerWindow, this);

            Reload();

            //Invoke OnEnable on current tab
            _ = InvokeTab();

            RefreshReviewPrompt();

        }

        void OnDisable()
        {

            if (!window)
                return;

            SceneManagerWindowProxy.requestSave -= SceneManagerWindowProxy_requestSave;

            //Invoke OnDisable() on current tab
            _ = InvokeTab();

            SceneManager.settings.local.sceneManagerWindow = JsonUtility.ToJson(this);
            SceneManager.settings.local.Save();

        }

        void SceneManagerWindowProxy_requestSave(ScriptableObject obj, bool updateBuildSettings) =>
            Save(obj, updateBuildSettings);

        void LoadDefaultStyle()
        {

            if (rootVisualElement is null)
                return;

            DoLoad();

            //Sometimes, for some reason, style is null when loaded, but works perfectly fine next frame
            EditorApplication.delayCall += () =>
                DoLoad();

            void DoLoad()
            {

                var path = "AdvancedSceneManager/Default-" + (IsDarkMode ? "Dark" : "Light");

                if (Resources.Load<StyleSheet>(path) is StyleSheet style && style)
                    rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>(path));

            }

        }

        //Used by EditCollectionPopup to prevent reload when renaming
        internal static bool preventReload;

        internal static void ReloadSceneDragButton()
        {
            if (window && window.rootVisualElement.Q(name: "SceneHelperButton") is VisualElement button)
            {

                button.visible = SceneManager.settings.local.displaySceneHelperDragButton && window.tab != Tab.Welcome;

                var isDown = false;
                void OnDown(MouseDownEvent e) =>
                    isDown = true;
                void OnUp(MouseUpEvent e) =>
                    isDown = false;

                void OnLeave(MouseLeaveEvent e)
                {

                    if (!isDown)
                        return;
                    isDown = false;

                    if (DragAndDrop.objectReferences.Length == 1 && DragAndDrop.objectReferences.First() == SceneHelper.current)
                        return;

                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.objectReferences = new[] { SceneHelper.current };
                    DragAndDrop.StartDrag("SceneHelper");

                }

                button.UnregisterCallback<MouseDownEvent>(OnDown);
                button.UnregisterCallback<MouseUpEvent>(OnUp);
                button.UnregisterCallback<MouseLeaveEvent>(OnLeave);

                button.RegisterCallback<MouseDownEvent>(OnDown);
                button.RegisterCallback<MouseUpEvent>(OnUp);
                button.RegisterCallback<MouseLeaveEvent>(OnLeave);

            }
        }

        [DidReloadScripts]
        internal static void Reload()
        {

            if (preventReload)
                return;

            if (window is SceneManagerWindow w && w && !BuildPipeline.isBuildingPlayer)
            {

                w.rootVisualElement?.UnregisterCallback<MouseUpEvent>(OnMouseUp);

                if (w.rootVisualElement == null)
                    return;

                w.ReloadContent();
                ReopenTab();

                w.LoadDefaultStyle();

                if (w.rootVisualElement.Q<Button>("PlayButton") is Button playButton)
                {
                    playButton.style.unityFont = new StyleFont(Resources.Load<Font>("Fonts/Inter-Regular"));
                    playButton.clicked -= OnPlayButton;
                    playButton.clicked += OnPlayButton;
                }

                if (w.rootVisualElement.Q<Button>("button-reload") is Button reloadButton)
                {

                    bool isRefreshing = false;
                    EditorApplication.update -= Update;
                    EditorApplication.update += Update;

                    void Update()
                    {
                        if (isRefreshing == AssetUtility.isRefreshing)
                            return;
                        reloadButton.text = AssetUtility.isRefreshing ? "X" : "?";
                        isRefreshing = AssetUtility.isRefreshing;
                    }

                    reloadButton.clicked -= Click;
                    reloadButton.clicked += Click;

                    void Click()
                    {
                        if (AssetUtility.isRefreshing)
                            AssetRefreshUtility.Stop();
                        else
                            w.Refresh();
                    }

                    reloadButton.style.unityFont = new StyleFont(Resources.Load<Font>("Fonts/Inter-Regular"));

                }

                w.rootVisualElement.RegisterCallback<MouseUpEvent>(OnMouseUp);

                if (w.rootVisualElement.Q<ToolbarToggle>("button-menu") is ToolbarToggle menuButton)
                    menuButton.style.unityFont = new StyleFont(Resources.Load<Font>("Fonts/Inter-Regular"));

                w.RefreshReviewPrompt();
                w.RefreshLegacyPrompt();

                if (window && window.rootVisualElement?.Q("footer") is VisualElement footer)
                    footer.SetEnabled(Profile.current);

                ReloadSceneDragButton();

                //Register handler for upper right menu button
                var menu = w.rootVisualElement.Q<ToolbarToggle>("button-menu");
                _ = menu?.UnregisterValueChangedCallback(ShowMenu);
                _ = menu?.RegisterValueChangedCallback(ShowMenu);

                if (w.rootVisualElement?.Q("header") is VisualElement header)
                    header.style.display = window.tab != Tab.Welcome ? DisplayStyle.Flex : DisplayStyle.None;

                window.Repaint();

            }

            void ShowMenu(ChangeEvent<bool> e) =>
                w.ShowMenu();

            void OnPlayButton() =>
                SceneManager.runtime.Start();

        }

        [PostProcessBuild]
        static void PostBuild(BuildTarget _, string _1)
        {
            if (window is SceneManagerWindow w && w)
                w.Refresh();
        }

        public class PostProcess : AssetPostprocessor
        {

            static void OnPostprocessAllAssets(string[] _, string[] _1, string[] _2, string[] _3) =>
                Reload();

        }

        #endregion
        #region Review prompt

        void RefreshReviewPrompt()
        {

            if (!window)
                return;

            var link = rootVisualElement.Q<Button>("link-review");
            var close = rootVisualElement.Q<Button>("closeReviewPrompt");
            var container = rootVisualElement?.Q("review");

            if (link != null)
            {
                link.clicked -= GoToReview;
                link.clicked += GoToReview;
            }

            if (close != null)
            {
                close.clicked -= OnReviewClose;
                close.clicked += OnReviewClose;
            }

            if (container != null)
                container.EnableInClassList("hidden", !SceneManager.settings.local.displayReviewPrompt);

            void GoToReview() =>
                Application.OpenURL("https://assetstore.unity.com/packages/tools/utilities/advanced-scene-manager-174152#reviews");

            void OnReviewClose()
            {
                SceneManager.settings.local.displayReviewPrompt = false;
                RefreshReviewPrompt();
            }

        }

        #endregion
        #region Legacy prompt

        void RefreshLegacyPrompt()
        {

#if UNITY_2021_1_OR_NEWER && !ASM_DEV

            if (!window)
                return;

            var link = rootVisualElement.Q<Button>("link-legacy");
            var close = rootVisualElement.Q<Button>("closeLegacyPrompt");
            var container = rootVisualElement?.Q("legacy");

            if (link != null)
            {
                link.clicked -= Upgrade;
                link.clicked += Upgrade;
            }

            if (close != null)
            {
                close.clicked -= OnPromptClose;
                close.clicked += OnPromptClose;
            }

            if (container != null)
                container.EnableInClassList("hidden", !SceneManager.settings.local.displayLegacyUpgradePrompt);

            void Upgrade()
            {

                if (!EditorUtility.DisplayDialog("Upgrading to 2.0...", "You are about to upgrade to ASM 2.0\n\nThis will remove all your profiles, collections and imported scenes (just ASM scenes, not the unity scene files themselves)\n\nAre you sure you wish to continue?", "Yes, upgrade", "Cancel"))
                    return;

                SceneManagerWindow.Close();
                AssetDatabase.DeleteAsset(AdvancedSceneManager.Core.AssetRef.path);
                AdvancedSceneManager.Editor.Utility.ScriptingDefineUtility.Unset("ASM_LEGACY");

            }

            void OnPromptClose()
            {
                SceneManager.settings.local.displayReviewPrompt = false;
                RefreshLegacyPrompt();
            }

#endif

        }

        #endregion
        #region Upper right menu

        void ShowMenu()
        {

            if (rootVisualElement == null)
                return;

            var removedCollectionCount = Profile.current ? (Profile.current.removedCollections?.Where(c => c)?.Count() ?? 0) : 0;

            GenericPopup.Open(rootVisualElement.Q("button-menu"), this, alignRight: true, offset: new Vector2(0, -6)).
                Refresh(
                    Item.Create("ASM " + ASMInfo.GetVersionInfo().version),
                    Item.Create("View patches...", ViewPatches),
                    Item.Separator,
                    Item.Create("Scene Overview...", SceneOverviewWindow.Open),

                    Item.Separator.WithVisibleState(removedCollectionCount > 0),
                    Item.Create($"Removed collections ({removedCollectionCount})...", () => SetTab(Tab.Trash)).WithVisibleState(removedCollectionCount > 0),

                    Item.Separator,
                    Item.Create("Look at documentation...", OpenDocumentation),
                    Item.Create("Look at lazy.solutions...", OpenLazy),
                    Item.Separator,
                    Item.Create("Build temp (profiler)", () => BuildUtility.DoTempBuild(attachProfiler: true)),
                    Item.Create("Build temp", () => BuildUtility.DoTempBuild(attachProfiler: false)),
                    Item.Separator,
                    Item.Create("Reset", ResetSceneManager)
                );

        }

        void ViewPatches() =>
            Application.OpenURL("https://github.com/Lazy-Solutions/AdvancedSceneManager/releases");

        void OpenDocumentation() => Process.Start("https://github.com/Lazy-Solutions/AdvancedSceneManager/blob/1.9/readme.md");
        void OpenLazy() => Process.Start("http://lazy.solutions/");
        void ResetSceneManager()
        {
            if (EditorUtility.DisplayDialog("Resetting Advanced Scene Manager...", "This will reset all changes made in Advanced Scene Manager and cannot be undone. Are you sure you wish to continue?'", "Reset", "Cancel"))
                AssetUtility.Clear();
        }

        void Refresh()
        {

            if (!Profile.current)
            {
                _ = EditorUtility.DisplayDialog("Refreshing assets...", "No profile is currently active", "ok");
                return;
            }

            _ = AssetRefreshUtility.DoFullRefresh().StartCoroutine();

        }

        #endregion
        #region Drag and drop reorder

        /// <summary>An class that manages drag and drop reorder.</summary>
        public static class DragAndDropReorder
        {

            static DragAndDropReorder()
            {
                OnGUIEvent += OnGUI;
                MouseUp += StopDrag;
            }

            public static VisualElement rootVisualElement { get; set; }

            public static event Action<DragElement> OnDragStarted;
            public static event Action<DragElement, int> OnDragEnded;
            public static event Action<DragElement> OnDragCancel;

            public static DragElement currentDragElement { get; private set; }
            public static int newIndex { get; private set; } = -1;
            public static float offset { get; private set; }

            public class DragElement
            {
                public VisualElement list;
                public VisualElement item;
                public VisualElement button;
                public EventCallback<MouseDownEvent> mouseDown;
                public EventCallback<MouseUpEvent> mouseUp;
                public EventCallback<MouseMoveEvent> mouseMove;
                public int index;
                public string itemRootName;
                public string itemRootClass;
            }

            /// <summary>
            /// <para>The lists that has drag elements.</para>
            /// <para>Key1: A list that can be reordered.</para>
            /// <para>Key2: The child element of list (Key1) that can be dragged.</para>
            /// <para>Value: <see cref="DragElement"/>, the logical object that represents an element that can be dragged.</para>
            /// </summary>
            static readonly
                Dictionary<VisualElement, Dictionary<VisualElement, DragElement>> lists = new
                Dictionary<VisualElement, Dictionary<VisualElement, DragElement>>();

            #region Registration

            public static void RegisterList(VisualElement list, string dragButtonName = null, string dragButtonClass = null, string itemRootName = null, string itemRootClass = null)
            {

                if (list == null)
                    return;
                if (EditorApplication.isCompiling)
                    return;

                if (string.IsNullOrWhiteSpace(dragButtonName) && string.IsNullOrWhiteSpace(dragButtonClass))
                    throw new ArgumentException($"Either {nameof(dragButtonName)} or {nameof(dragButtonClass)} must be set!");

                if (string.IsNullOrWhiteSpace(itemRootName) && string.IsNullOrWhiteSpace(itemRootClass))
                    throw new ArgumentException($"Either {nameof(itemRootName)} or {nameof(itemRootClass)} must be set!");

                var dragElements = lists.Set(list, lists.GetValue(list) ?? new Dictionary<VisualElement, DragElement>());

                var i = -1;
                list.Query(className: itemRootClass, name: itemRootName).ForEach(item =>
                {

                    //Get or create logical drag element object
                    var dragElement = dragElements.Set(item, dragElements.GetValue(item) ?? new DragElement());

                    //Find drag button
                    var button = item.Q(name: dragButtonName, className: dragButtonClass);

                    var mouseDown = new EventCallback<MouseDownEvent>((MouseDownEvent e) =>
                    {
                        if (e.button == 0 && e.modifiers == EventModifiers.None)
                            StartDrag(dragElement, e);
                    });

                    //Unregister old callback, if we already registered this element before
                    if (dragElement.mouseDown != null)
                        button.UnregisterCallback(dragElement.mouseDown);
                    button.RegisterCallback(mouseDown);


                    dragElement.button = button;
                    dragElement.mouseDown = mouseDown;
                    dragElement.list = list;
                    dragElement.item = item;
                    dragElement.index = i += 1;
                    dragElement.itemRootClass = itemRootClass;
                    dragElement.itemRootName = itemRootName;

                });

            }

            public static void UnregisterList(VisualElement list)
            {

                if (lists == null || !lists.ContainsKey(list))
                    return;

                var items = lists.GetValue(list).Values;

                foreach (var item in lists.GetValue(list).Values.Where(i => i.mouseDown != null))
                    item.button.UnregisterCallback(item.mouseDown);

                _ = lists.Remove(list);

            }

            public static void UnregisterListAll()
            {
                foreach (var list in lists.ToArray())
                    UnregisterList(list.Key);
            }

            #endregion

            static void StartDrag(DragElement element, MouseDownEvent e)
            {
                if (currentDragElement == null && CanDrag(element))
                {
                    OnDragStarted?.Invoke(element);
                    offset = e.localMousePosition.y;
                    currentDragElement = element;
                }
            }

            static void StopDrag()
            {

                if (currentDragElement != null)
                {

                    if (_isOutsideOfDeadzone)
                    {

                        CleanUp(currentDragElement);

                        if (currentDragElement.index == newIndex)
                            OnDragCancel?.Invoke(currentDragElement);
                        else
                            OnDragEnded?.Invoke(currentDragElement, newIndex);

                    }

                    isUp = false;
                    offset = 0;
                    currentDragElement = null;
                    newIndex = -1;
                    mouseDownPos = null;
                    _isOutsideOfDeadzone = false;

                }
            }

            static VisualElement currentDropZone;
            static VisualElement CreateDropZone(float height)
            {

                currentDropZone?.RemoveFromHierarchy();

                currentDropZone = new VisualElement();
                currentDropZone.style.height = height;
                currentDropZone.style.backgroundColor = Color.gray;

                return currentDropZone;

            }

            static bool CanDrag(DragElement element) =>
                element.list.childCount > 1;

            static bool isUp;
            static VisualElement overlay;
            static void Setup(DragElement element)
            {

                element.list.Children().ElementAt(element.index).RemoveFromHierarchy();
                rootVisualElement.Add(element.item);

                element.item.style.position = Position.Absolute;
                element.item.style.top = Event.current.mousePosition.y - offset - 3;

                foreach (var style in element.list.GetStyles())
                    element.item.styleSheets.Add(style);

                element.item.style.backgroundColor = Utility.VisualElementExtensions.DefaultBackgroundColor;
                element.item.style.width = element.list.resolvedStyle.width;
                element.item.style.marginLeft = element.item.style.marginRight = element.list.worldBound.xMin;
                element.item.style.height = element.item.resolvedStyle.height;

                element.item.Query("draggable-hidden").ForEach(e => e.EnableInClassList("hidden", true));

                overlay?.RemoveFromHierarchy();
                overlay = new VisualElement() { name = "block-input-overlay" };
                rootVisualElement.Add(overlay);

            }

            static void CleanUp(DragElement element)
            {
                overlay?.RemoveFromHierarchy();
                foreach (var item in rootVisualElement.Children().ToArray())
                {
                    if (item.name == element.itemRootName || item.ClassListContains(element.itemRootClass))
                        item.RemoveFromHierarchy();
                }
            }

            static Vector2 prevMousePos;
            static Vector2? mouseDownPos;
            static bool _isOutsideOfDeadzone;
            static void OnGUI()
            {

                if (currentDragElement == null)
                    return;

                if (!mouseDownPos.HasValue)
                    mouseDownPos = Event.current.mousePosition;

                var isOutsideOfDeadZone = (Event.current.mousePosition - mouseDownPos.Value).magnitude > 2;
                if (isOutsideOfDeadZone && !_isOutsideOfDeadzone)
                {
                    Setup(currentDragElement);
                    _isOutsideOfDeadzone = isOutsideOfDeadZone;
                    return;
                }
                else if (!isOutsideOfDeadZone)
                    return;

                //Check if mouse is moving up or down, since we want different behavior for drop zone depending on this
                var delta = Event.current.mousePosition - prevMousePos;
                if (delta.y != 0)
                    isUp = delta.y < 0;

                //Move element
                var element = currentDragElement.item;
                element.style.position = Position.Absolute;
                element.style.top = Event.current.mousePosition.y - offset - 3;


                //Ensure that we stay inside bounds of list
                var yMin = currentDragElement.list.worldBound.yMin;
                var yMax = currentDragElement.list.worldBound.yMax;

                if (element.style.top.value.value < yMin - element.resolvedStyle.height)
                    element.style.top = yMin - element.resolvedStyle.height;
                if (element.style.top.value.value > yMax)
                    element.style.top = yMax;


                //Get index under mouse position
                var elements = currentDragElement.list.Query(className: currentDragElement.itemRootClass, name: currentDragElement.itemRootName).ToList();
                var index = isUp
                    ? elements.FindIndex(e => element.resolvedStyle.top + element.resolvedStyle.height < e.worldBound.center.y)
                    : elements.FindIndex(e => element.resolvedStyle.top + (element.resolvedStyle.height * 2) < e.worldBound.center.y);

                if (index == -1)
                    index = elements.Count;

                if (index != newIndex)
                {

                    //Put element where item would go, as a preview
                    var dropZone = CreateDropZone(currentDragElement.item.style.height.value.value);
                    if (index < elements.Count && index >= 0)
                        currentDragElement.list.Insert(index, dropZone);
                    else
                        currentDragElement.list.Add(dropZone);

                }

                newIndex = index;
                prevMousePos = Event.current.mousePosition;

            }

        }

        static void OnDragStarted(DragAndDropReorder.DragElement element)
        {
            OnReorderStart(element);
        }

        static void OnDragEnded(DragAndDropReorder.DragElement element, int newIndex)
        {
            OnReorderEnd(element, newIndex);
            OnDragCancel(element);
        }

        static void OnDragCancel(DragAndDropReorder.DragElement element)
        {
            Reload();
        }

        #endregion
        #region Tabs

        //Invoke OnLostFocus() on current tab
        void OnLostFocus() =>
            InvokeTab();

        //Invoke OnFocus() on current tab
        public override void OnFocus()
        {
            base.OnFocus();
            _ = InvokeTab();
        }

        #region Footer

        public class FooterItem
        {

            public VisualElement element { get; private set; }
            public bool left { get; private set; }

            public static FooterItem Create() =>
                new FooterItem();

            public FooterItem OnLeft()
            {
                left = true;
                return this;
            }

            public FooterItem OnRight()
            {
                left = false;
                return this;
            }

            public FooterItem Button(string text, Action click, string tooltip = "", Action<Button> setup = null) =>
                Element<Button>(text, tooltip: tooltip, setup: b => { b.clickable.clicked += click; b.AddToClassList("newButton"); setup?.Invoke(b); });

            public FooterItem Element<T>(string text = "", Action<T> setup = null, string tooltip = "") where T : VisualElement, new()
            {
                element = new T() { tooltip = tooltip };
                if (element is TextElement el)
                    el.text = text;
                setup?.Invoke((T)element);
                return this;
            }

            public FooterItem Hidden() =>
                Visible(false);

            public FooterItem Visible(bool visible = true)
            {
                element?.EnableInClassList("hidden", !visible);
                return this;
            }

        }

        private FooterItem[] FooterButtons =>
            (FooterItem[])InvokeTab() ?? Array.Empty<FooterItem>();

        void SetupFooter()
        {

            var buttons = FooterButtons;
            var footer = rootVisualElement.Q("footer");
            var footerLeft = rootVisualElement.Q("footer-left");
            var footerRight = rootVisualElement.Q("footer-right");

            footer.EnableInClassList("hidden", !buttons.Any());
            rootVisualElement.Q("content").style.marginBottom = buttons.Any() ? footer.style.height : 0;

            footerLeft.Clear();
            footerRight.Clear();

            foreach (var button in buttons.Where(b => b.element != null))
                if (button.left)
                    footerLeft.Add(button.element);
                else
                    footerRight.Add(button.element);

        }

        #endregion

        static void OnReorderStart(DragAndDropReorder.DragElement element) => InvokeTab(nameof(OnReorderStart), element);
        static void OnReorderEnd(DragAndDropReorder.DragElement element, int newIndex) => InvokeTab(nameof(OnReorderEnd), element, newIndex);

        public static void ReopenTab()
        {
            if (window is SceneManagerWindow w && w)
                w.SetTab(w.tab);
        }

        static Tab savedTab;
        public static void RestoreTab()
        {
            if (window is SceneManagerWindow w && w)
                w.SetTab(savedTab);
        }

        public static void OpenTab(Tab tab)
        {
            if (window)
                window.SetTab(tab);
        }

        void GenerateTabHeader()
        {

            var tabHeader = rootVisualElement.Q<VisualElement>("tabs");
            if (tabHeader == null)
                return;

            tabHeader.Clear();

            //We need to manually reset the other tabs, so let's just loop through them all
            foreach (var t in Enum.GetValues(typeof(Tab)))
            {

                if (!tabs[(Tab)t].showInheader)
                    continue;

                var enabled = tab == (Tab)t;
                var tabButton = new ToolbarToggle();
                tabHeader.Add(tabButton);

                tabButton.AddToClassList("tab-button");
                tabButton.text = ObjectNames.NicifyVariableName(t.ToString());
                tabButton.SetValueWithoutNotify(enabled);

                if (enabled)
                    tabButton.AddToClassList("selected");

                _ = tabButton.RegisterValueChangedCallback(e =>
                {
                    if (e.newValue)
                    {
                        SetTab((Tab)t);
                        Reload();
                    }
                    else
                        tabButton.SetValueWithoutNotify(true);
                });

            }

        }

        /// <summary>Set tab as active.</summary>
        void SetTab(Tab tab)
        {

            var scrollView = rootVisualElement?.Q<ScrollView>();
            if (scrollView == null)
                return;

            scrollView.verticalScroller.valueChanged += (value) =>
                scroll = value;

            if (this.tab != tab)
                scroll = 0;

            if (BuildPipeline.isBuildingPlayer)
                return;

            if (tab == Tab.Welcome && Setup.ASM.isSetup)
                tab = Tab.Scenes;

            if (!Profile.current)
            {
                if (tab != Tab.NoProfile)
                    savedTab = tab;
                tab = Tab.NoProfile;
            }

            if (!Setup.ASM.isSetup)
                tab = Tab.Welcome;

            DragAndDropReorder.UnregisterListAll();

            GenerateTabHeader();

            //Disable existing tab and enable new
            _ = InvokeTab(nameof(OnDisable));

            //Set content to tab
            var content = rootVisualElement.Q<VisualElement>("tab-content");
            LoadContent(tabs[tab].path, content);

            if (tab != Tab.Welcome)
            {
                minSize = new Vector2(400, 200);
                maxSize = new Vector2(1000, 1000);
            }

            this.tab = tab;
            _ = InvokeTab(nameof(OnEnable));

            RefreshReviewPrompt();
            SetupFooter();

            scrollView.verticalScroller.value = scroll;

        }

        /// <summary>
        /// <para>Invokes the static method on the current <see cref="Tab"/>.</para>
        /// <para>For example, when called from <see cref="OnFocus"/>, OnFocus() will be called on the current tab.</para>
        /// </summary>
        static object InvokeTab([CallerMemberName] string caller = "")
        {
            return window is SceneManagerWindow w && w
                ? InvokeTab(caller, w.rootVisualElement.Q<VisualElement>("tab-content"))
                : null;
        }

        static object InvokeTab(string name, params object[] param)
        {

            var w = window;
            if (!w)
                return null;

            var method = w.tabs[w.tab].type?.GetMethod(name);
            var property = w.tabs[w.tab].type?.GetProperty(name);
            if (method == null && property == null)
                return null;

            if (method?.GetParameters()?.Select(p => p?.ParameterType)?.SequenceEqual((param ?? Array.Empty<object>())?.Select(p => p?.GetType())) ?? false)
                return method?.Invoke(null, param);
            else if (!method?.GetParameters()?.Any() ?? false)
                return method?.Invoke(null, null);

            return property?.GetValue(null);

        }

        #endregion
        #region Save

        public static void Save(ScriptableObject so = null, bool updateBuildSettings = true)
        {

            if (!Profile.current)
                return;

            if (so == null)
                so = Profile.current;

            EditorUtility.SetDirty(so);

            if (focusedWindow == SceneOverviewWindow.window && SceneOverviewWindow.window is SceneOverviewWindow sceneOverview && sceneOverview)
                sceneOverview.titleContent = new GUIContent("Scene Overview*");
            else if (window is SceneManagerWindow w && w)
                w.titleContent = new GUIContent("Scene Manager*");

            if (updateBuildSettings)
                BuildUtility.UpdateSceneList();

        }

        static void OnAssetsSave(string[] paths)
        {

            if (paths.Length == 1 && paths[0] == ASMSettings.Local.path.Replace("\\", "/"))
                return;

            if (window is SceneManagerWindow w && w)
                w.titleContent = new GUIContent("Scene Manager");

            if (SceneOverviewWindow.window is SceneOverviewWindow w1 && w1)
                w1.titleContent = new GUIContent("Scene Overview");

            BuildUtility.UpdateSceneList();
            SceneManager.settings.local.Save();

        }

        #endregion
        #region Selection

        internal static class Selection
        {

            #region Static

            #region Clear selection on blank area click / clear after context menu closes

            /// <summary>Clear selection when gui returns to window. This is used to clear selection when context menu closes.</summary>
            public static void ClearWhenGUIReturns() =>
                clearOnNextGUI = true;

            static bool ignoreNextClear;
            public static void DidSelectItem()
            {
                ignoreNextClear = true;
            }

            static bool clearOnNextGUI;
            static void RegisterContentClick()
            {

                OnGUIEvent += OnGUI;

                void OnGUI()
                {
                    if (clearOnNextGUI)
                    {
                        clearOnNextGUI = false;
                        Clear();
                    }
                }

                SceneManagerWindow.window.rootVisualElement.UnregisterCallback<MouseDownEvent>(Click);
                SceneManagerWindow.window.rootVisualElement.RegisterCallback<MouseDownEvent>(Click);

                void Click(MouseDownEvent e)
                {

                    if (e.button == 0 && !ignoreNextClear)
                        Clear();
                    ignoreNextClear = false;

                }

            }

            #endregion
            #region Collection / scene / tag helpers

            public static IEnumerable<SceneCollection> collections => list.Where(i => !i.Key.scene.HasValue && i.Value).Select(i => i.Key.collection);
            public static IEnumerable<(SceneCollection collection, int scene)> scenes => list.Where(i => i.Key.scene.HasValue && i.Value).Select(i => (i.Key.collection, i.Key.scene.Value));
            public static IEnumerable<SceneTag> tags => list.Where(i => i.Key.tag != null && i.Value).Select(i => i.Key.tag);

            /// <summary>Gets whatever a collection or scene is selected.</summary>
            /// <remarks>To only check if collection is selected, just leave <paramref name="scene"/> as <see langword="null"/>.</remarks>
            public static bool IsSelected(SceneCollection collection, int? scene = null) =>
                IsSelected(items.Keys.FirstOrDefault(s => s.collection == collection && s.scene == scene));

            /// <summary>Gets whatever a tag is selected.</summary>
            public static bool IsSelected(SceneTag tag) =>
                IsSelected(items.Keys.FirstOrDefault(s => tag != null && s.tag?.id == tag?.id));

            /// <summary>Select a collection or scene.</summary>
            /// <remarks>To only select collection, just leave <paramref name="scene"/> as <see langword="null"/>.</remarks>
            public static void Select(SceneCollection collection, int? scene = null, bool selected = true)
            {

                var selector = items.Keys.FirstOrDefault(i => i.collection == collection && i.scene == scene);
                if (selector != null)
                    Select(selector, selected);

            }

            /// <summary>Select a tag.</summary>
            public static void Select(SceneTag tag, bool selected = true)
            {

                var selector = items.Keys.FirstOrDefault(i => tag != null && i.tag?.id == tag?.id);
                if (selector != null)
                    Select(selector, selected);

            }

            /// <summary>Unselect a collection or scene.</summary>
            /// <remarks>To only unselect collection, just leave <paramref name="scene"/> as <see langword="null"/>.</remarks>
            public static void Unselect(SceneCollection collection, int? scene = null) =>
                Select(collection, scene, selected: false);

            /// <summary>Unselect a tag.</summary>
            public static void Unselect(SceneTag tag) =>
                Select(tag, selected: false);

            #endregion

            public static event Action OnSelectionChanged;

            static Dictionary<Manipulator, bool> items = new Dictionary<Manipulator, bool>();

            static ReadOnlyDictionary<Manipulator, bool> m_list;
            public static ReadOnlyDictionary<Manipulator, bool> list => m_list ?? (m_list = new ReadOnlyDictionary<Manipulator, bool>(items));

            static bool IsSelected(Manipulator selector) =>
                items.GetValue(selector);

            static void Select(Manipulator selector, bool selected)
            {
                DidSelectItem();
                items[selector] = selected;
                SetSelectionVisual(selector.selectionVisual ?? selector.target, selected);
                OnSelectionChanged?.Invoke();
            }

            static void Unselect(Manipulator selector) =>
                Select(selector, selected: false);

            /// <summary>Unselects all items.</summary>
            public static void Clear()
            {
                foreach (var item in items.Keys.ToArray())
                    Unselect(item);
            }

            /// <summary>Clears selection and tracked objects.</summary>
            public static void Reset()
            {
                Clear();
                items.Clear();
            }

            static void SetSelectionVisual(VisualElement element, bool selected) =>
                element?.EnableInClassList("selected", selected);

            #endregion
            #region Instance

            public class Manipulator : MouseManipulator
            {

                public VisualElement selectionVisual { get; private set; }
                public SceneCollection collection { get; private set; }
                public int? scene { get; private set; }
                public SceneTag tag { get; private set; }

                public Manipulator(SceneTag tag, VisualElement selectionVisual = null)
                {

                    RegisterContentClick();

                    this.tag = tag;
                    this.selectionVisual = selectionVisual;

                }

                public Manipulator(SceneCollection collection, int? scene = null, VisualElement selectionVisual = null)
                {

                    RegisterContentClick();

                    this.collection = collection;
                    this.scene = scene;
                    this.selectionVisual = selectionVisual;

                }

                protected override void RegisterCallbacksOnTarget()
                {

                    target.RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);

                    var existing = items.Keys.FirstOrDefault(i => i.collection == collection && i.scene == scene);
                    var selected = items.GetValue(existing);
                    if (existing != null)
                        _ = items.Remove(existing);

                    items.Add(this, selected);
                    Select(this, selected);

                }

                protected override void UnregisterCallbacksFromTarget()
                {

                    target.UnregisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);

                    Unselect(this);
                    _ = items.Remove(this);

                }

                void OnMouseDown(MouseDownEvent e)
                {

                    var isDown = e.ctrlKey || e.commandKey;

                    if (!isDown)
                    {
                        Clear();
                        return;
                    }

                    if (!isDown && !IsSelected(this))
                        Clear();

                    var select = e.button == 1 || !IsSelected(this);
                    Select(this, select);

                }

            }

            #endregion

        }

        #endregion
        #region Rounded corner helper

        internal static class RoundedCornerHelper
        {

            static readonly List<(VisualElement header, VisualElement body)[]> lists = new List<(VisualElement header, VisualElement body)[]>();

            public static void Add((VisualElement header, VisualElement body)[] list) => lists.Add(list);

            public static void Update()
            {
                foreach (var list in lists.ToArray())
                    Update(list);
            }

            static void Update((VisualElement header, VisualElement body)[] list)
            {

                Reset(list);

                list.FirstOrDefault().header?.AddToClassList("first");
                list.FirstOrDefault().body?.AddToClassList("first");
                list.LastOrDefault().header?.AddToClassList("last");
                list.LastOrDefault().body?.AddToClassList("last");

                (VisualElement header, VisualElement body) prev = default;
                foreach (var item in list)
                {

                    if (prev.header?.ClassListContains("expanded") ?? false)
                        item.header.AddToClassList("first");

                    if (item.header?.ClassListContains("expanded") ?? false)
                        item.body.AddToClassList("last");

                    prev = item;

                }

            }

            static void Reset((VisualElement header, VisualElement body)[] list)
            {

                if (list.All(item => item == default))
                    _ = lists.Remove(list);

                foreach (var (header, body) in list.ToArray())
                {
                    header?.RemoveFromClassList("first");
                    header?.RemoveFromClassList("last");
                    body?.RemoveFromClassList("last");
                    body?.RemoveFromClassList("last");
                }

            }

        }

        #endregion

    }

}

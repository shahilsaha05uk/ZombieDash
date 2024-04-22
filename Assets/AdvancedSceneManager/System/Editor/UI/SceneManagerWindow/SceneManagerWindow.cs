using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AdvancedSceneManager.Core;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Models.Internal;
using AdvancedSceneManager.Utility;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    [InitializeInEditor]
    /// <summary>The scene manager window provides the front-end for Advanced Scene Manager.</summary>
    public partial class SceneManagerWindow : EditorWindow
    {

        [SerializeField] private VisualTreeAsset rootView = default;

        HeaderView header { get; } = new();
        FooterView footer { get; } = new();
        PopupView popups { get; } = new();
        NotificationView notifications { get; } = new();
        UndoView undo { get; } = new();
        CollectionListView collections { get; } = new();

        readonly INotificationPopup[] notificationPopups = new INotificationPopup[]
        {
            new ImportScenePopup(),
            new InvalidScenePopup(),
            new UntrackedScenePopup(),
            new ImportedBlacklistedScenePopup(),
            new BadPathScenePopup(),
            new EditorCoroutinesNotification(),
        };

        internal static new VisualElement rootVisualElement { get; private set; }
        internal static SceneManagerWindow window { get; private set; }

        public static event System.Action onOpen;
        public static event System.Action onClose;
        public static event System.Action onFocus;

        Dictionary<ViewModel, VisualElement> sections;

        void CreateGUI()
        {

            titleContent = new GUIContent("Scene Manager");

            minSize = new(466, 230);

            window = this;
            rootVisualElement = base.rootVisualElement;
            EnsureViewsAreReferenced();

            SetupProfileBinding();
            SetupRootView();
            SetupSections();

            SetupDevMenu();

            ApplyAppearanceSettings();
            UpdateProfileElements();

            onOpen?.Invoke();

            //Fix issue where scrollbars do not work sometimes

            rootVisualElement.RegisterCallback<WheelEvent>(e =>
            {
                if (e.target is VisualElement element && element.GetFirstAncestorOfType<ScrollView>() is ScrollView scrollView && scrollView.verticalScrollerVisibility != ScrollerVisibility.Hidden)
                {
                    var offset = scrollView.scrollOffset;
                    offset.y += e.delta.y;
                    scrollView.scrollOffset = offset;
                }
            });

            SceneManager.settings.user.PropertyChanged += (s, e) => ApplyAppearanceSettings();

        }

        #region Window

        static SceneManagerWindow() =>
            App.onUninstall += Close;

        public static new void Close()
        {
            if (window)
                ((EditorWindow)window).Close();
        }

        [MenuItem("File/Scene Manager... %#m", priority = 205)]
        [MenuItem("Window/Advanced Scene Manager/Scene Manager", priority = 3030)]
        static void Open() => GetWindow<SceneManagerWindow>();

        public void OnEnable() =>
            SceneManager.OnInitialized(() =>
            {

                if (window)
                    foreach (var popup in window.notificationPopups)
                        popup.ReloadNotification();

                sections?.Keys.ForEach(s => s.OnEnable());

            });

        public void OnDisable()
        {
            SceneManager.OnInitialized(() => sections?.Keys.ForEach(s => s.OnDisable()));
            onClose?.Invoke();
        }

        public static new void Focus()
        {
            if (window)
                ((EditorWindow)window).Focus();
        }

        public void OnFocus() =>
            SceneManager.OnInitialized(() =>
            {

                if (!SceneManager.isInitialized)
                    return;

                Assets.CleanupAndSave();

                foreach (var popup in notificationPopups)
                    popup.ReloadNotification();

                sections?.Keys.ForEach(s => s.OnFocus());

                onFocus?.Invoke();

            });

        public void OnLostFocus() =>
            SceneManager.OnInitialized(() => sections?.Keys.ForEach(s => s.OnLostFocus()));

        #endregion
        #region Setup

        void SetupProfileBinding()
        {

            OnProfileChanged();
            Profile.onProfileChanged += OnProfileChanged;

            void OnProfileChanged()
            {

                if (Profile.current && Profile.serializedObject is not null)
                    rootVisualElement.Bind(Profile.serializedObject);
                else
                    rootVisualElement.Unbind();

                UpdateProfileElements();

            }

        }

        readonly List<VisualElement> profileElements = new();
        public void BindEnabledToProfile(VisualElement element) =>
            profileElements.Add(element);

        void UpdateProfileElements() =>
            profileElements.ForEach(e => e.SetEnabled(Profile.current));

        void SetupSections()
        {

            sections = new()
            {
                { popups, rootVisualElement.Q("popup-overlay") },
                { notifications, rootVisualElement.Q("list-notifications") },
                { undo, rootVisualElement.Q("list-undo") },
                { collections, rootVisualElement.Q("section-collections") },
            };

            sections.ForEach(section =>
            {
                section.Key.element = section.Value;
                section.Key.OnCreateGUI(section.Value);
            });

        }

        void SetupRootView()
        {

            if (!rootView)
            {
                EnsureViewsAreReferenced();
                if (!rootView)
                {
                    UnityEngine.Debug.LogWarning("The scene manager window could not initialize due to null root template. Please try and restart unity.");
                    return;
                }
            }

            var template = rootView.Instantiate();
            template.style.height = new(new Length(100, LengthUnit.Percent));
            rootVisualElement.Add(template);

            header.OnCreateGUI(rootVisualElement);
            footer.OnCreateGUI(rootVisualElement);

            SceneManager.OnInitialized(() =>
            {
                foreach (var popup in notificationPopups)
                    popup.ReloadNotification();
            });

        }

        void ApplyAppearanceSettings()
        {
            header.ApplyAppearanceSettings(rootVisualElement.Q("header"));
            sections.ForEach(section => section.Key.ApplyAppearanceSettings(section.Value));
            footer.ApplyAppearanceSettings(rootVisualElement.Q("footer"));
        }

        #region Dev menu

        void SetupDevMenu()
        {

            var rootPath = "Assets/AdvancedSceneManager/System/Editor/UI/SceneManagerWindow";
            var viewPath = rootPath + "/Views";
            var viewModelPath = rootPath + "/ViewModels";

            rootVisualElement.Q("button-menu").ContextMenu(e =>
            {

                e.menu.AppendAction("View ASM folder...", _ => ShowFolder(ASMPath()));
                e.menu.AppendSeparator();
                e.menu.AppendAction("View window source...", _ => ShowFolder(WindowPath()));
                e.menu.AppendSeparator();
                e.menu.AppendAction("View project settings...", _ => OpenSettingsFolder("ProjectSettings"));
                e.menu.AppendAction("View local settings...", _ => OpenSettingsFolder("UserSettings"));
                e.menu.AppendAction("View profiles...", _ => ShowFolder(ProfilePath()));
                e.menu.AppendAction("View scenes...", _ => ShowFolder(ScenePath()));
                e.menu.AppendSeparator();
                e.menu.AppendAction("View default scenes...", _ => ShowFolder(DefaultScenePath()));
                e.menu.AppendAction("View example scripts...", _ => ShowFolder(ExampleScriptPath()));

                e.menu.AppendSeparator();
                e.menu.AppendAction("View import popup...", _ => window.popups.Open<ImportScenePopup>());

            });

            string WindowPath() => ASMPath() + "/System/Editor/UI/SceneManagerWindow";
            string DefaultScenePath() => ASMPath() + "/Defaults";
            string ExampleScriptPath() => ASMPath() + "/Example scripts";
            string ProfilePath() => Assets.GetFolder<Profile>();
            string ScenePath() => Assets.GetFolder<Scene>();

            void OpenSettingsFolder(string type)
            {
                var path = Path.GetFullPath($"{Application.dataPath}/../{type}/AdvancedSceneManager.asset");
                Process.Start("explorer", $@"/select,""{path}""");
            }

            string ASMPath() => GetParentFolder(GetASMAssemblyDefinitionPath());

            string GetASMAssemblyDefinitionPath() =>
                AssetDatabase.FindAssets("t:asmdef").
                    Select(AssetDatabase.GUIDToAssetPath).
                    First(path => path.EndsWith("AdvancedSceneManager.asmdef"));

            string GetParentFolder(string path) =>
                Directory.GetParent(path).FullName.Replace("\\", "/").Replace(Application.dataPath, "Assets");

        }

        #region Open folder in project view

        static void ShowFolder(string path) =>
            ShowFolder(AssetDatabase.LoadAssetAtPath<Object>(path).GetInstanceID());

        /// <summary>
        /// Selects a folder in the project window and shows its content.
        /// Opens a new project window, if none is open yet.
        /// </summary>
        /// <param name="folderInstanceID">The instance of the folder asset to open.</param>
        static void ShowFolder(int folderInstanceID)
        {

            // Find the internal ProjectBrowser class in the editor assembly.
            var editorAssembly = typeof(EditorApplication).Assembly;
            var projectBrowserType = editorAssembly.GetType("UnityEditor.ProjectBrowser");

            // This is the internal method, which performs the desired action.
            // Should only be called if the project window is in two column mode.
            var showFolderContents = projectBrowserType.GetMethod("ShowFolderContents", BindingFlags.Instance | BindingFlags.NonPublic);

            // Find any open project browser windows.
            var projectBrowserInstances = Resources.FindObjectsOfTypeAll(projectBrowserType);

            if (projectBrowserInstances.Length > 0)
            {
                for (int i = 0; i < projectBrowserInstances.Length; i++)
                    ShowFolderInternal(projectBrowserInstances[i], showFolderContents, folderInstanceID);
            }
            else
            {
                var projectBrowser = OpenNewProjectBrowser(projectBrowserType);
                ShowFolderInternal(projectBrowser, showFolderContents, folderInstanceID);
            }

        }

        static void ShowFolderInternal(Object projectBrowser, MethodInfo showFolderContents, int folderInstanceID)
        {

            // Sadly, there is no method to check for the view mode.
            // We can use the serialized object to find the private property.
            var serializedObject = new SerializedObject(projectBrowser);
            var inTwoColumnMode = serializedObject.FindProperty("m_ViewMode").enumValueIndex == 1;

            if (!inTwoColumnMode)
            {
                // If the browser is not in two column mode, we must set it to show the folder contents.
                var setTwoColumns = projectBrowser.GetType().GetMethod("SetTwoColumns", BindingFlags.Instance | BindingFlags.NonPublic);
                setTwoColumns.Invoke(projectBrowser, null);
            }

            var revealAndFrameInFolderTree = true;
            showFolderContents.Invoke(projectBrowser, new object[] { folderInstanceID, revealAndFrameInFolderTree });

        }

        static EditorWindow OpenNewProjectBrowser(System.Type projectBrowserType)
        {

            var projectBrowser = GetWindow(projectBrowserType);
            projectBrowser.Show();

            // Unity does some special initialization logic, which we must call,
            // before we can use the ShowFolderContents method (else we get a NullReferenceException).
            var init = projectBrowserType.GetMethod("Init", BindingFlags.Instance | BindingFlags.Public);
            init.Invoke(projectBrowser, null);

            return projectBrowser;

        }

        #endregion
        #endregion

        #endregion
        #region Progress spinner

        VisualElement progressSpinner => rootVisualElement.Q("progress-spinner");

        public void StartProgressSpinner()
        {

            if (progressSpinner.pickingMode == PickingMode.Position)
                return;

            progressSpinner.parent.RemoveFromClassList("hidden");
            progressSpinner.parent.pickingMode = PickingMode.Position;
            progressSpinner.pickingMode = PickingMode.Position;

            progressSpinner.RotateAnimation();

        }

        public async void StopProgressSpinner()
        {

            if (progressSpinner.pickingMode == PickingMode.Ignore)
                return;

            progressSpinner.parent.AddToClassList("hidden");

            await Task.Delay(250);

            progressSpinner.parent.pickingMode = PickingMode.Ignore;
            progressSpinner.pickingMode = PickingMode.Ignore;
            progressSpinner.StopRotateAnimation();

        }

        #endregion
        #region Editor references

        //We use default editor references (in script asset inspector) to refer to our template parts.
        //This normally works, but sometimes breaks on domain reload if window is already open, the
        //references will become null for some reason. We can reassign these by just creating a new instance.
        void EnsureViewsAreReferenced()
        {

            var fields =
                GetType().
                GetFields(BindingFlags.GetField | BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).
                Where(w => w.FieldType == typeof(Object)).
                Where(f => f.GetValue(this) == null).
                ToArray();

            if (fields.Any())
            {

                var w = CreateInstance<SceneManagerWindow>();
                foreach (var field in fields)
                    if (field.GetValue(w) is VisualTreeAsset reference)
                        field.SetValue(this, reference);
                    else
                        throw new System.Exception("Unable to restore default VisualTreeAsset references.");

                DestroyImmediate(w);

            }

        }

        #endregion

    }

}

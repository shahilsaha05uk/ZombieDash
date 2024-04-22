using System;
using System.Collections.Generic;
using System.Linq;
using AdvancedSceneManager.Core;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using Lazy.Utility;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static AdvancedSceneManager.Editor.GenericPopup;

namespace AdvancedSceneManager.Editor.Window
{

    public static class SettingsTab
    {

        public static Utility.SettingsTab Settings => Utility.SettingsTab.instance;

        #region Names and tooltips for default settings

        static readonly Dictionary<string, string> names = new Dictionary<string, string>()
        {

            { nameof(Profile.startupScene), "Startup scene (*):" },
            { nameof(Profile.splashScreen), "Splash screen:" },
            { nameof(Profile.startupLoadingScreen), "Startup loading screen:" },
            { nameof(Profile.loadingScreen), "Loading screen:" },

            { nameof(ASMSettings.Local.saveActionWhenUsingASMPlayButton), "Save action when using ASM play button:" },

            { nameof(Profile.enableChangingBackgroundLoadingPriority), "Background loading priority:" },
            { nameof(Profile.backgroundLoadingPriority), "" },
            { nameof(Profile.createCameraDuringStartup), "Create camera during startup:" },
            { nameof(Profile.useDefaultPauseScreen), "Use default pause screen:" },
            { nameof(Profile.unloadUnusedAssetsForStandalone), "Unload unused assets for standalone:" },
            { nameof(Profile.checkForDuplicateSceneOperations), "Prevent duplicate scene operations:" },
            { nameof(Profile.preventSpammingEventMethods), "Prevent event methods from being spammed:" },
            { nameof(Profile.spamCheckCooldown), "Spam check cooldown (sec):" },

            { nameof(SceneManager.settings.local.displayPersistentIndicatorInHierarchy), "Display 'persistent' on scenes in hierarchy:" },
            { nameof(SceneManager.settings.local.displayCollectionTitleOnScenesInHierarchy), "Display collection titles on scenes in hierarchy:" },

            { nameof(SceneManager.settings.local.displayCollectionPlayButton), "Display collection play button:" },
            { nameof(SceneManager.settings.local.displayCollectionOpenButton) , "Display collection open button:" },
            { nameof(SceneManager.settings.local.displayCollectionAdditiveButton) , "Display collection open additive button:" },
            { nameof(SceneManager.settings.project.allowExcludingCollectionsFromBuild), "Display include in build toggle on collections:" },
            { nameof(SceneManager.settings.local.displaySceneHelperDragButton), "Show scene helper drag button:" },
            { nameof(SceneManager.settings.local.displayExtraAddCollectionButton) , "Show extra add collection menu:" },
            { nameof(SceneManager.settings.local.useSaveDialogWhenCreatingScenesFromSceneField) , "Use save dialog when creating scenes:" },
            { nameof(SceneManager.settings.local.openStartupCollectionWhenPlayingSpecificCollection) , "Open startup col. when using play col. button:" },
            { nameof(SceneManager.settings.local.displayDynamicCollections) , "Display dynamic collections:" },

            { nameof(SceneManager.settings.local.autoOpenScenesWhenCreated), "Open scenes when created using scene field:" },
            { nameof(SceneManager.settings.local.allowEditingOfBuildSettings), "Allow manual editing of build settings:" },

        };


        static readonly Dictionary<string, string> tooltips = new Dictionary<string, string>()
        {

            { nameof(Profile.startupScene), "The scene to open first in builds. Do not change this unless you know what you're doing." },
            { nameof(Profile.splashScreen), "The splash screen to play during startup." },
            { nameof(Profile.startupLoadingScreen), "The loading screen to use after splash screen (or immediately if no splash screen)." },
            { nameof(Profile.loadingScreen), "The default loading screen to use when opening collections." },

            { nameof(Profile.enableChangingBackgroundLoadingPriority), "Enable or disable ASM automatically changing background loading priority." },
            { nameof(Profile.backgroundLoadingPriority), "Enable or disable ASM automatically changing background loading priority." },
            { "AssetPath", "The path to store assets in by default.\n\nNote that the assets can always be moved after being created, this only effects where assets are created.\n\nAssets do not need to be in a resources folder." },

            { nameof(Profile.createCameraDuringStartup), "Create a camera automatically during startup." },
            { nameof(Profile.useDefaultPauseScreen), "Use the default pause screen. Which can be opened by pressing escape. If input system is installed and enabled, then the start button on a controller will also work." },
            { nameof(Profile.unloadUnusedAssetsForStandalone), "Unload unused assets when a standalone scene is opened or closed." },
            { nameof(Profile.checkForDuplicateSceneOperations), "If true, this will prevent scene operations from running if it is attempting to open and close the exact same scenes as a currently queued / running operation." },
            { nameof(Profile.preventSpammingEventMethods), "If true, this will prevent ui buttons from spam calling Scene.OpenEvent() / Scene helper methods from the click event, for example." },
            { nameof(Profile.spamCheckCooldown), "This specifies the spam check cooldown for event methods (Scene.OpenEvent(), or most scene helper methods, for example), that the above setting uses." },

            { nameof(SceneManager.settings.local.useSaveDialogWhenCreatingScenesFromSceneField) , "Use save dialog when creating scenes. When false, a scene will be created in project view, like how a scene is normally created." },
            { nameof(SceneManager.settings.local.openStartupCollectionWhenPlayingSpecificCollection) , "When collection play button is pressed, should ASM also open the collection specified as startup?" },

            { nameof(SceneManager.settings.local.autoOpenScenesWhenCreated), "When creating a scene using a scene field, open scene automatically." },
            { nameof(SceneManager.settings.local.allowEditingOfBuildSettings), "ASM manages build settings automatically, and it is not recommended to manually modify them.\n\nFor the event when this is required though, this setting will allow manual edit of build settings." },
            { nameof(SceneManager.settings.local.displayDynamicCollections) , "Displays dynamic collections in scenes tab." },

            { Settings.DefaultHeaders.Options_Profile, "Settings that are saved in the current profile." },
            { Settings.DefaultHeaders.Options_Project, "Settings that are saved in SceneManager.settings, project wide." },
            { Settings.DefaultHeaders.Options_Local, "Settings that are saved locally, on this PC." },

        };

        #endregion
        #region UI

        static (Toggle toggle, EnumField enumField, VisualElement root) EnumToggleField(string toggleProperty, string enumProperty)
        {

            var root = new VisualElement();
            root.AddToClassList("horizontal");

            var toggle = ToggleProfile(toggleProperty);
            var enumField = Enum(enumProperty);
            enumField.style.flexGrow = 1;

            _ = toggle.RegisterValueChangedCallback(value => enumField.SetEnabled(value.newValue));
            enumField.SetEnabled(toggle.value);

            root.Add(toggle);
            root.Add(enumField);

            root.style.marginBottom = -2;

            return (toggle, enumField, root);

        }

        static Toggle ToggleProfile(string property) =>
            new Toggle(names.GetValue(property)).Setup(Profile.current, property, tooltip: tooltips.GetValue(property));

        static Toggle Toggle(string property, EventCallback<ChangeEvent<bool>> callback, bool defaultValue = false) =>
            new Toggle(names.GetValue(property)).Setup(callback, defaultValue, tooltip: tooltips.GetValue(property));

        static SceneField Scene(string property, string label = "", string defaultName = "") =>
            new SceneField() { labelFilter = label, defaultName = defaultName }.Setup(label: names.GetValue(property), Profile.current, property, tooltip: tooltips.GetValue(property));

        static EnumField Enum(string property, Action callback = null) =>
            new EnumField(names.GetValue(property)).Setup(Profile.current, property, callback);

        static EnumField Enum(string property, Enum defaultValue, Action<Enum> callback = null)
        {
            var element = new EnumField(names.GetValue(property));
            element.Init(defaultValue);
            _ = element.RegisterValueChangedCallback(e => callback?.Invoke(e.newValue));
            return element;
        }

        static FloatField Float(string property, float defaultValue, Action<float> callback)
        {
            var element = new FloatField();
            element.Q("unity-text-input").style.flexGrow = 0;
            element.SetValueWithoutNotify(defaultValue);
            element.tooltip = tooltips.GetValue(property);
            element.label = names.GetValue(property);
            _ = element.RegisterValueChangedCallback(e => { callback?.Invoke(e.newValue); });
            return element;
        }

        #endregion
        #region Setup

        static bool IsExpanded(string header, bool? value = null)
        {
            if (value.HasValue)
                _ = SceneManagerWindow.window.openSettingHeaders.Set(header, value.Value);
            if (SceneManagerWindow.window.openSettingHeaders.ContainsKey(header))
                return SceneManagerWindow.window.openSettingHeaders[header];
            else
                return false;
        }

        static bool isInitialized;
        public static void OnEnable(VisualElement element)
        {

            if (!isInitialized)
                AddDefaultSettings();
            isInitialized = true;

            InitializeSettings(element);

        }

        static void AddDefaultSettings()
        {

            //Profiles
            Profiles();

            //Options_Profile
            Settings.Add(ObjectField("Default profile:", "The profile to use when none has been set during editor startup / reload.", SceneManager.settings.project.defaultProfile, (p) => { SceneManager.settings.project.defaultProfile = p; SceneManager.settings.project.Save(); }), Settings.DefaultHeaders.Options);
            Settings.Add(ObjectField("Force profile:", "The profile that everyone in this project is forced to use.", SceneManager.settings.project.forceProfile, (p) => { SceneManager.settings.project.forceProfile = p; SceneManager.settings.project.Save(); }), Settings.DefaultHeaders.Options);
            Settings.Add(header: Settings.DefaultHeaders.Options_Profile, callback: Scenes());

            Settings.Add(EnumToggleField(toggleProperty: nameof(Profile.enableChangingBackgroundLoadingPriority), enumProperty: nameof(Profile.backgroundLoadingPriority)).root, header: Settings.DefaultHeaders.Options_Profile);
            Settings.Add(ToggleProfile(nameof(Profile.createCameraDuringStartup)), header: Settings.DefaultHeaders.Options_Profile);
            Settings.Add(ToggleProfile(nameof(Profile.useDefaultPauseScreen)), header: Settings.DefaultHeaders.Options_Profile);
            Settings.Add(ToggleProfile(nameof(Profile.unloadUnusedAssetsForStandalone)), header: Settings.DefaultHeaders.Options_Profile);

            //Options_SpamCheck
            Settings.Add(ToggleProfile(nameof(Profile.checkForDuplicateSceneOperations)), header: Settings.DefaultHeaders.Options_SpamChecking);
            Settings.Add(ToggleProfile(nameof(Profile.preventSpammingEventMethods)), header: Settings.DefaultHeaders.Options_SpamChecking);
            Settings.Add(Float(nameof(Profile.spamCheckCooldown), Profile.current.spamCheckCooldown, v => { Profile.current.spamCheckCooldown = v; SceneManagerWindow.Save(Profile.current); }), header: Settings.DefaultHeaders.Options_SpamChecking);

            //Options_Local
            Settings.Add(Toggle(nameof(SceneManager.settings.local.autoOpenScenesWhenCreated), e => { SceneManager.settings.local.autoOpenScenesWhenCreated = e.newValue; SceneManagerWindow.Save(updateBuildSettings: false); }, SceneManager.settings.local.autoOpenScenesWhenCreated), header: Settings.DefaultHeaders.Options_Local);
            Settings.Add(Toggle(nameof(SceneManager.settings.local.allowEditingOfBuildSettings), e => { SceneManager.settings.local.allowEditingOfBuildSettings = e.newValue; SceneManagerWindow.Save(updateBuildSettings: false); }, SceneManager.settings.local.allowEditingOfBuildSettings), header: Settings.DefaultHeaders.Options_Local);
            Settings.Add(Toggle(nameof(SceneManager.settings.local.useSaveDialogWhenCreatingScenesFromSceneField), e => { SceneManager.settings.local.useSaveDialogWhenCreatingScenesFromSceneField = e.newValue; SceneManagerWindow.Save(updateBuildSettings: false); }, SceneManager.settings.local.useSaveDialogWhenCreatingScenesFromSceneField), header: Settings.DefaultHeaders.Options_Local);
            Settings.Add(Toggle(nameof(SceneManager.settings.local.openStartupCollectionWhenPlayingSpecificCollection), e => { SceneManager.settings.local.openStartupCollectionWhenPlayingSpecificCollection = e.newValue; SceneManagerWindow.Save(updateBuildSettings: false); }, SceneManager.settings.local.openStartupCollectionWhenPlayingSpecificCollection), header: Settings.DefaultHeaders.Options_Local);
            Settings.Add(Enum(nameof(SceneManager.settings.local.saveActionWhenUsingASMPlayButton), SceneManager.settings.local.saveActionWhenUsingASMPlayButton, callback: value => { SceneManager.settings.local.saveActionWhenUsingASMPlayButton = (ASMSettings.Local.SaveAction)value; SceneManagerWindow.Save(); }), header: Settings.DefaultHeaders.Options_Local);

            //Options_Advanced
            var label = new Label("The following items are for specialized use-cases, please read tooltips before using.");
            label.style.opacity = 0.8f;
            label.style.SetMargin(bottom: 6);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.width = 400;
            Settings.Add(label, header: Settings.DefaultHeaders.Options_Advanced);
            Settings.Add(Scene(nameof(Profile.startupScene)), header: Settings.DefaultHeaders.Options_Advanced);

            //Appearance_Hierarchy
            Settings.Add(Toggle(nameof(SceneManager.settings.local.displayCollectionTitleOnScenesInHierarchy), e => { SceneManager.settings.local.displayCollectionTitleOnScenesInHierarchy = e.newValue; SceneManagerWindow.Save(updateBuildSettings: false); }, SceneManager.settings.local.displayCollectionTitleOnScenesInHierarchy), header: Settings.DefaultHeaders.Appearance_Hierarchy);
            Settings.Add(Toggle(nameof(SceneManager.settings.local.displayPersistentIndicatorInHierarchy), e => { SceneManager.settings.local.displayPersistentIndicatorInHierarchy = e.newValue; SceneManagerWindow.Save(updateBuildSettings: false); }, SceneManager.settings.local.displayPersistentIndicatorInHierarchy), header: Settings.DefaultHeaders.Appearance_Hierarchy);

            //Appearance_ScenesTab
            Settings.Add(Toggle(nameof(SceneManager.settings.local.displayExtraAddCollectionButton), e => { SceneManager.settings.local.displayExtraAddCollectionButton = e.newValue; SceneManagerWindow.Save(updateBuildSettings: false); }, SceneManager.settings.local.displayExtraAddCollectionButton), header: Settings.DefaultHeaders.Appearance_ScenesTab);
            Settings.Add(Toggle(nameof(SceneManager.settings.local.displayDynamicCollections), e => { SceneManager.settings.local.displayDynamicCollections = e.newValue; SceneManagerWindow.Save(updateBuildSettings: false); }, SceneManager.settings.local.displayDynamicCollections), header: Settings.DefaultHeaders.Appearance_ScenesTab);

            //Appearance_WindowHeader
            Settings.Add(Toggle(nameof(SceneManager.settings.local.displaySceneHelperDragButton), e => { SceneManager.settings.local.displaySceneHelperDragButton = e.newValue; SceneManagerWindow.Save(updateBuildSettings: false); }, SceneManager.settings.local.displaySceneHelperDragButton), header: Settings.DefaultHeaders.Appearance_WindowHeader);

            //Appearance_CollectionHeader
            Settings.Add(Toggle(nameof(SceneManager.settings.local.displayCollectionPlayButton), e => { SceneManager.settings.local.displayCollectionPlayButton = e.newValue; SceneManagerWindow.Save(updateBuildSettings: false); }, SceneManager.settings.local.displayCollectionPlayButton), header: Settings.DefaultHeaders.Appearance_CollectionHeader);
            Settings.Add(Toggle(nameof(SceneManager.settings.local.displayCollectionOpenButton), e => { SceneManager.settings.local.displayCollectionOpenButton = e.newValue; SceneManagerWindow.Save(updateBuildSettings: false); }, SceneManager.settings.local.displayCollectionOpenButton), header: Settings.DefaultHeaders.Appearance_CollectionHeader);
            Settings.Add(Toggle(nameof(SceneManager.settings.local.displayCollectionAdditiveButton), e => { SceneManager.settings.local.displayCollectionAdditiveButton = e.newValue; SceneManagerWindow.Save(updateBuildSettings: false); }, SceneManager.settings.local.displayCollectionAdditiveButton), header: Settings.DefaultHeaders.Appearance_CollectionHeader);
            Settings.Add(Toggle(nameof(SceneManager.settings.project.allowExcludingCollectionsFromBuild), e => { SceneManager.settings.project.allowExcludingCollectionsFromBuild = e.newValue; SceneManagerWindow.Save(SceneManager.settings.project, updateBuildSettings: false); }, SceneManager.settings.project.allowExcludingCollectionsFromBuild), header: Settings.DefaultHeaders.Appearance_CollectionHeader);

            //Assets
            InitializeAssets();

        }

        static void InitializeSettings(VisualElement element)
        {

            element.Clear();
            element.style.SetMargin(top: 6);

            var headers = Settings.settings.Keys.
                Where(h => !string.IsNullOrWhiteSpace(h)).
                OrderBy(Settings.GetHeaderOrder).
                Select(h => (originalHeader: h, header: !h.Contains("_") ? h : h.Remove(h.IndexOf("_")))).
                GroupBy(g => g.header).
                Select(g => g.First()).
                ToArray();

            var topLevelElement = new VisualElement();
            element.Add(topLevelElement);
            topLevelElement.style.SetMargin(top: 12);
            foreach (var header in Settings.settings.Keys.Where(string.IsNullOrWhiteSpace))
                InitializeSectionContent(header, topLevelElement);

            foreach ((string originalHeader, string header) in headers)
            {
                InitializeSectionBox(element, originalHeader, header, out var container);
                InitializeSectionContent(header, container);
            }

        }

        static void InitializeSectionBox(VisualElement element, string originalHeader, string header, out VisualElement container)
        {

            var box = new Box();
            element.Add(box);

            var container1 = container = new VisualElement();
            var headerContentBox = new VisualElement();
            var headerBox = new VisualElement();
            box.Add(headerBox);
            box.Add(container1);

            headerBox.style.flexDirection = FlexDirection.Row;
            headerContentBox.style.flexDirection = FlexDirection.Row;
            headerContentBox.style.SetMargin(top: -1);

            var button = new Button();
            button.style.flexGrow = 2;
            headerBox.Add(button);
            headerBox.Add(headerContentBox);
            button.style.SetPadding(12);
            button.style.SetMargin(-1);
            container1.style.SetPadding(12, top: 0);

            if (Settings.headerContent.TryGetValue(originalHeader.Split('_').First(), out var headerContent))
                foreach (var callback in headerContent)
                    headerContentBox.Add(callback);

            CoroutineUtility.Run(
                () => headerContentBox.style.height = button.resolvedStyle.height,
                when: () => !float.IsNaN(button.resolvedStyle.height));

            button.AddToClassList("header");

            ReloadExpander();
            button.clicked += () =>
            {
                _ = IsExpanded(header, !IsExpanded(header));
                ReloadExpander();
            };

            void ReloadExpander()
            {

                var isExpanded = IsExpanded(header);

                button.style.marginBottom = isExpanded ? 12 : 0;
                button.text = (isExpanded ? "▼  " : "►  ") + header;

                container1.EnableInClassList("hidden", !isExpanded);
                headerContentBox.EnableInClassList("hidden", !isExpanded);

            }

        }

        static void InitializeSectionContent(string header, VisualElement container)
        {

            var settings =
                !string.IsNullOrWhiteSpace(header)
                ? Settings.settings.Where(s => s.Key.StartsWith(header)).OrderBy(v => Settings.GetHeaderOrder(v.Key)).ToArray()
                : Settings.settings.Where(s => string.IsNullOrWhiteSpace(s.Key)).OrderBy(v => Settings.GetHeaderOrder(v.Key)).ToArray();

            foreach (var setting in settings)
            {

                if (setting.Key.Contains("_"))
                {
                    var text = new TextElement() { text = setting.Key.Substring(setting.Key.IndexOf("_") + 1) };
                    text.style.unityFontStyleAndWeight = FontStyle.Bold;
                    text.style.marginTop = 6;
                    text.style.marginBottom = 4;
                    container.Add(text);
                    text.tooltip = tooltips.GetValue(setting.Key);
                }

                foreach (var callback in setting.Value)
                {

                    if (callback is null)
                    {
                        //Spacer
                        var el = new VisualElement();
                        el.style.height = 12;
                        container.Add(el);
                    }
                    else if (callback is VisualElement el && el != null)
                    {
                        el.SetEnabled(setting.Key != Settings.DefaultHeaders.Options_Profile || Profile.current);
                        container.Add(el);
                    }

                }

            }

        }

        #endregion
        #region Sections

        #region Profiles

        static void Profiles()
        {

            VisualElement element = null;
            Reload();

            Profile.onProfileChanged -= Reload;
            Profile.onProfileChanged += Reload;

            void Reload()
            {
                Settings.Remove(element);
                Settings.Add(element = Element(), "");
            }

            VisualElement Element()
            {

                var root = new VisualElement();
                root.style.marginBottom = 12;

                root.Add(CurrentProfileField(changedCallback: () => SceneManagerWindow.Reload()).SetStyle(e => e.style.SetMargin(top: 6, bottom: 12)));

                return root;

            }

        }

        public static VisualElement CurrentProfileField(Action changedCallback = null) =>
            ProfileField("", "", Profile.current, null,
                onApply: (profile) =>
                {

                    foreach (var header in Settings.settings.Keys)
                        _ = IsExpanded(header.Contains("_") ? header.Remove(header.IndexOf("_")) : header, false);

                    Profile.SetProfile(profile, updateBuildSettings: false);
                    changedCallback?.Invoke();

                }, onMenu: (menu) =>
                {

                    GenericPopup.Open(menu, SceneManagerWindow.window, alignRight: true, offset: new Vector2(0, -3)).Refresh(
                        Item.Create("New profile", () => { AssetUtility.CreateProfileAndAssign(); SceneManagerWindow.Reload(); }),
                        Item.Create("Duplicate profile", () => { AssetUtility.DuplicateProfileAndAssign(); SceneManagerWindow.Reload(); }).WithEnabledState(HasProfile()),
                        Item.Separator,
                        Item.Create("Delete profile", () => AssetUtility.DeleteProfile(Profile.current)).WithEnabledState(HasProfile()));

                    bool HasProfile() =>
                        Profile.current;

                });

        static VisualElement ProfileField(string header, string tooltip, Profile defaultValue, Action<Profile> onChange, Action<Profile> onApply, Action<VisualElement> onMenu)
        {

            var element = new VisualElement();
            element.AddToClassList("horizontal");

            Button applyButton = null;
            ToolbarToggle menuButton = null;

            var objectField = ObjectField(header, tooltip, defaultValue, onChange: p =>
            {
                applyButton?.SetEnabled(defaultValue != p);
                menuButton?.SetEnabled(defaultValue == p);
                onChange?.Invoke(p);
            });
            objectField.style.flexGrow = 1;

            element.Add(objectField);

            if (onApply != null)
            {
                applyButton = new Button() { text = "Apply" };
                element.Add(applyButton);
                applyButton.SetEnabled(false);
                applyButton.clicked += () => onApply.Invoke((Profile)objectField.value);
            }

            if (onMenu != null)
            {

                menuButton = new ToolbarToggle() { text = "⋮" };
                menuButton.AddToClassList("StandardButton");
                menuButton.name = "Settings-New-Profile-Button";
                element.Add(menuButton);
                _ = menuButton.RegisterValueChangedCallback(e => onMenu.Invoke(menuButton));

            }

            return element;

        }

        static ObjectField ObjectField<T>(string header, string tooltip, T defaultValue, Action<T> onChange) where T : UnityEngine.Object
        {

            var field = new ObjectField
            {
                label = header,
                tooltip = tooltip,
                objectType = typeof(T)
            };
            field.SetValueWithoutNotify(defaultValue);
            _ = field.RegisterValueChangedCallback(e => onChange?.Invoke((T)e.newValue));
            field.style.width = new StyleLength(StyleKeyword.Initial);

            return field;

        }

        #endregion
        #region Scenes

        static VisualElement Scenes()
        {

            var root = new VisualElement();
            root.style.marginBottom = 12;

            root.Add(Scene(nameof(Profile.splashScreen), "ASM: SplashScreen"));
            root.Add(Scene(nameof(Profile.startupLoadingScreen), label: "LoadingScreen"));
            root.Add(Scene(nameof(Profile.loadingScreen), label: "LoadingScreen"));

            return root;

        }

        #endregion
        #region Assets

        static void InitializeAssets()
        {

            var hasBlacklistChanges = false;

            var blacklist = GetBlacklist();

            Profile.onProfileChanged += () => blacklist = GetBlacklist();

            BlacklistUtility.BlacklistModule GetBlacklist() =>
                Profile.current && Profile.current.blacklist != null
                    ? Profile.current.blacklist.Clone()
                    : new BlacklistUtility.BlacklistModule();

            Vector2 assetPathScroll = default;
            string assetPath = null;

            Settings.Add(Element(), Settings.DefaultHeaders.Assets);

            VisualElement Element()
            {

                return new IMGUIContainer(() =>
                {

                    var size = EditorStyles.boldLabel.fontSize;
                    EditorStyles.boldLabel.fontSize = 16;

                    OnGUI_AssetPath();
                    EditorGUILayout.Space(22);
                    OnGUI_Blacklist();
                    EditorGUILayout.Space(22);
                    OnGUI_AssetRefreshTriggers();

                    EditorStyles.boldLabel.fontSize = size;

                });

            }

            void OnGUI_AssetPath()
            {

                const string prefix = "Assets/";
                const string suffix = "/Resources/AdvancedSceneManager";

                if (assetPath == null)
                    assetPath = Relative(AssetRef.path);

                GUILayout.BeginHorizontal();
                GUILayout.Label("Asset path:", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                GUI.enabled = Relative(AssetRef.path) != assetPath;
                if (GUILayout.Button("Apply")) Apply();
                if (GUILayout.Button("Cancel")) Cancel();
                GUI.enabled = true;

                GUILayout.EndHorizontal();

                assetPathScroll = GUILayout.BeginScrollView(assetPathScroll);

                assetPath = GUILayout.TextField(assetPath);

                GUILayout.BeginHorizontal();

                GUILayout.FlexibleSpace();
                var c = GUI.color;
                GUI.color = Color.gray;
                var content = new GUIContent(prefix + assetPath + suffix);
                GUILayout.Label(content);
                GUI.color = c;

                GUILayout.EndHorizontal();
                GUILayout.EndScrollView();

                EditorGUILayout.Space();

                void Apply() => AssetRef.Move(prefix + assetPath + suffix);
                void Cancel() => assetPath = Relative(AssetRef.path);

                string Relative(string path) =>
                    path.Replace(prefix, "").Replace(suffix, "");

            }

            void OnGUI_Blacklist()
            {

                if (!Profile.current)
                    return;

                GUI.enabled = !Application.isPlaying;

                GUILayout.BeginHorizontal();
                GUILayout.Label("Blacklist:", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                GUI.enabled = hasBlacklistChanges && !Application.isPlaying;
                if (GUILayout.Button("Apply")) Apply();
                if (GUILayout.Button("Cancel")) Cancel();
                GUI.enabled = !Application.isPlaying;

                GUILayout.EndHorizontal();

                EditorGUILayout.Space();

                var (didDirty, isInfoExpanded, height) = BlacklistUtility.DrawGUI(blacklist);
                if (didDirty)
                    hasBlacklistChanges = true;

                GUI.enabled = true;

                void Apply()
                {
                    hasBlacklistChanges = false;
                    Profile.current.m_blacklist = blacklist;
                    Profile.current.Save();
                    if (SceneManager.settings.local.assetRefreshTriggers.HasFlag(ASMSettings.Local.AssetRefreshTrigger.BlacklistChanged))
                        AssetUtility.Refresh(evenIfInPlayMode: false, immediate: true);
                }

                void Cancel()
                {
                    hasBlacklistChanges = false;
                    blacklist = Profile.current.blacklist;
                }

            }

            void OnGUI_AssetRefreshTriggers()
            {

                GUILayout.Label("Asset refresh triggers:", EditorStyles.boldLabel);
                GUILayout.Label("The following toggles can disable certain triggers for asset refresh, this means asset refresh won't run as often, which will result in a experiance with less interruptions, but can sometimes require manual refresh.\n\nThese settings are stored locally, they won't be synced to team members.", EditorStyles.wordWrappedLabel);
                EditorGUILayout.Space();

                Toggle(ASMSettings.Local.AssetRefreshTrigger.SceneCreated, "Scene created");
                Toggle(ASMSettings.Local.AssetRefreshTrigger.SceneRemoved, "Scene removed");
                Toggle(ASMSettings.Local.AssetRefreshTrigger.SceneMoved, "Scene moved");
                EditorGUILayout.Space();
                Toggle(ASMSettings.Local.AssetRefreshTrigger.ProfileChanged, "Profile changed");
                Toggle(ASMSettings.Local.AssetRefreshTrigger.BlacklistChanged, "Blacklist changed");
                Toggle(ASMSettings.Local.AssetRefreshTrigger.DynamicCollectionsChanged, "Dynamic collections changed");

                void Toggle(ASMSettings.Local.AssetRefreshTrigger value, string label)
                {

                    EditorGUI.BeginChangeCheck();
                    var enabled = EditorGUILayout.ToggleLeft(" " + label, SceneManager.settings.local.assetRefreshTriggers.HasFlag(value));

                    if (EditorGUI.EndChangeCheck())
                        if (enabled)
                            SceneManager.settings.local.assetRefreshTriggers |= value;
                        else
                            SceneManager.settings.local.assetRefreshTriggers &= ~value;

                }

            }

        }

        #endregion

        #endregion

    }

}

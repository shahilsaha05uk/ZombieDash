#pragma warning disable CS0649 // Field is not assigned to
#pragma warning disable CS0067 // Event is not used

using AdvancedSceneManager.Utility;
using UnityEngine;
using System;
using AdvancedSceneManager.Core;
using System.Linq;
using System.Reflection;
using System.IO;
using AdvancedSceneManager.Callbacks;

#if UNITY_EDITOR
using UnityEditor;
using AdvancedSceneManager.Editor.Utility;
#endif

namespace AdvancedSceneManager.Models
{

    /// <summary>Settings relating to ASM.</summary>
    /// <remarks>Usage: <see cref="SceneManager.settings"/>.</remarks>
    public class ASMSettings : ScriptableObject
    {

        /// <summary>Used to make <see cref="SceneManager.settings"/> a bit more intuitive.</summary>
        public class SettingsProxy
        {

#if UNITY_EDITOR
            /// <summary>The local asm settings, not synced to source.</summary>
            /// <remarks>Only available in editor.</remarks>
            public Local local { get; } = new Local();
#endif

            /// <summary>The project-wide asm settings.</summary>
            public ASMSettings project => AssetRef.instance.settings;

            /// <summary>The profile-wide asm settings.</summary>
            public Profile profile => Profile.current;

        }

        #region Local

#if UNITY_EDITOR

        /// <summary>Contains settings that are stored locally, that aren't synced to source control.</summary>
        /// <remarks>Only available in editor.</remarks>
        [Serializable]
        public class Local
        {

            #region Appearance

            /// <summary>Specifies whatever review prompt should be shown.</summary>
            public bool displayReviewPrompt = true;

            /// <summary>Specifies whatever legacy upgrade prompt should be shown.</summary>
            public bool displayLegacyUpgradePrompt = true;

            /// <summary>Specifies whatever collection play button should be visible.</summary>
            public bool displayCollectionPlayButton = true;

            /// <summary>Specifies whatever collection open button should be visible.</summary>
            public bool displayCollectionOpenButton = true;

            /// <summary>Specifies whatever collection open additive button should be visible.</summary>
            public bool displayCollectionAdditiveButton = true;

            /// <summary>Specifies whatever extra add collection button should be visible.</summary>
            public bool displayExtraAddCollectionButton = true;

            /// <summary>Specifies whatever scene helper button should be visible.</summary>
            public bool displaySceneHelperDragButton = true;

            /// <summary>Specifies whatever the persistent indicator should be visible on persistent scenes in the hierarchy.</summary>
            public bool displayPersistentIndicatorInHierarchy = true;

            /// <summary>Specifies whatever the associated collection name should be visible on scenes in the hierarchy.</summary>
            public bool displayCollectionTitleOnScenesInHierarchy = true;

            /// <summary>Specifies whatever the dynamic collections should be visible in scenes tab.</summary>
            public bool displayDynamicCollections = true;

            #endregion
            #region Functional

            [SerializeField] internal bool isBuildMode;
            [SerializeField] internal bool respectDoNotOpenTag = true;
            [SerializeField] internal bool isCallbackUtilityEnabled;
            [SerializeField] internal Runtime.Setup sceneSetup;
            [SerializeField] internal SerializableDictionary<string, PersistentSceneInEditorUtility.OpenInEditorSetting> editorPersistentScenes;
            [SerializeField] internal bool inGameToolbarInEditor;

            [SerializeField] internal string sceneManagerWindow;
            [SerializeField] internal string callbackUtilityWindow;
            [SerializeField] internal string sceneOverviewWindow;

            [SerializeField] internal SerializableDateTime lastPatchCheck;

            /// <summary>Specifies the active profile in editor.</summary>
            public string activeProfile;

            /// <summary>Specifies whatever manually editing build settings is allowed.</summary>
            /// <remarks>Could cause issues, please remember enabling this option if you do.</remarks>
            public bool allowEditingOfBuildSettings;

            /// <summary>Specifies whatever scenes created from scene manager window should open in heirarchy.</summary>
            public bool autoOpenScenesWhenCreated;

            /// <summary>Specifies whatever save dialog should be used when creating scenes directly from scene manager window.</summary>
            public bool useSaveDialogWhenCreatingScenesFromSceneField;

            /// <summary>When <see langword="true"/>: opens the first found collection that a scene is contained in when opening an SceneAsset in editor.</summary>
            public bool openAssociatedCollectionOnSceneAssetOpen;

            /// <summary>Specifies what ASM should do when entering play mode using ASM play button, and there are unsaved changes.</summary>
            public enum SaveAction
            {
                DoNothing, Save, Prompt
            }

            /// <summary>Specifies what ASM should do when entering play mode using ASM play button, and there are unsaved changes.</summary>
            public SaveAction saveActionWhenUsingASMPlayButton = SaveAction.Prompt;

            /// <summary>Specifies asset refresh trigger flags.</summary>
            [Flags]
            public enum AssetRefreshTrigger
            {

                /// <summary>Specifies that asset refresh should never automatically run.</summary>
                None = 0,

                /// <summary>Specifies that asset refresh should run when a scene is created.</summary>
                SceneCreated = 1,

                /// <summary>Specifies that asset refresh should run when a scene is removed.</summary>
                SceneRemoved = 2,

                /// <summary>Specifies that asset refresh should run when a scene is moved.</summary>
                SceneMoved = 4,

                /// <summary>Specifies that asset refresh should run when active profile is change.</summary>
                ProfileChanged = 8,

                /// <summary>Specifies that asset refresh should run when blaclist is changed.</summary>
                BlacklistChanged = 16,

                /// <summary>Specifies that asset refresh should run when dynamic collections are changed.</summary>
                DynamicCollectionsChanged = 32

            }

            static AssetRefreshTrigger assetRefreshTrigger_all = (AssetRefreshTrigger)Enum.GetValues(typeof(AssetRefreshTrigger)).Cast<int>().Sum();

            /// <summary>Specifies asset refresh triggers as a flag enum.</summary>
            public AssetRefreshTrigger assetRefreshTriggers = assetRefreshTrigger_all;

            /// <summary>Specifies whatever startup collection will open before the specified collection when entering play mode using collection play button.</summary>
            public bool openStartupCollectionWhenPlayingSpecificCollection = true;

            #endregion
            #region Setup

            #region Update

            bool isUpdating = false;
            void Update()
            {

                if (isUpdating)
                    return;
                isUpdating = true;

                if (File.Exists(path))
                    return;

                foreach (var field in typeof(Local).GetFields(BindingFlags.GetField | BindingFlags.SetField | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    if (GetOld(field.Name, field.FieldType, out var value))
                        field.SetValue(this, value);

                Save();

                isUpdating = false;

            }

            const string Prefix = "AdvancedSceneManager.";
            /// <summary>Gets a value from local storage.</summary>
            private bool GetOld(string key, Type type, out object value)
            {

                value = null;
                if (!key.StartsWith(Prefix))
                    key = Prefix + key;

                if (!PlayerPrefs.HasKey(key))
                    return false;

                if (type == typeof(bool))
                    value = (PlayerPrefs.GetInt(key) == 1);
                else if (type == typeof(int))
                    value = PlayerPrefs.GetInt(key);
                else if (typeof(Enum).IsAssignableFrom(type))
                    value = PlayerPrefs.GetInt(key);
                else if (type == typeof(float))
                    value = PlayerPrefs.GetFloat(key);
                else if (type == typeof(string))
                    value = PlayerPrefs.GetString(key);
                else if (type.GetCustomAttribute<SerializableAttribute>() != null)
                    value = JsonUtility.FromJson(PlayerPrefs.GetString(key), type);

                PlayerPrefs.DeleteKey(key);

                return value != null;

            }

            #endregion

            internal const string path = "UserSettings/AdvancedSceneManagerSettings.json";

            /// <summary>Reloads local settings from persistent storage.</summary>
            public void Reload()
            {

                if (!File.Exists(path))
                    Update();
                else
                {
                    var json = File.ReadAllText(path);
                    JsonUtility.FromJsonOverwrite(json, this);
                }

            }

            /// <summary>Occurs when local settings are saved.</summary>
            public Action onSave;

            /// <summary>Saves the local settings to persistent storage.</summary>
            public void Save()
            {

                if (!AssetRef.isInitialized)
                    return;

                Directory.GetParent(path).Create();
                if (Profile.current)
                    activeProfile = Profile.current.name;

                var json = JsonUtility.ToJson(this);
                File.WriteAllText(path, json);
                onSave?.Invoke();

            }

            #endregion

        }

#endif

        #endregion
        #region Project wide

        [Serializable]
        public class CustomData : SerializableDictionary<string, string>
        { }

        [Serializable]
        public class SceneData : SerializableDictionary<string, CustomData>
        { }

        [SerializeField] private CustomData customData = new CustomData();
        [SerializeField] internal Runtime.StartProps m_startProps;
        [SerializeField] internal Profile m_defaultProfile;
        [SerializeField] internal Profile m_forceProfile;
        [SerializeField] private Profile m_buildProfile;
        [SerializeField] private Color m_unitySplashScreenColor = Color.black;
        [SerializeField] private bool m_inGameToolbarEnabled = true;
        [SerializeField] private bool m_inGameToolbarExpandedByDefault = false;
        [SerializeField] private bool m_allowExcludingCollectionsFromBuild = false;
        [SerializeField] internal SceneData sceneData = new SceneData();

        /// <summary>Gets custom data.</summary>
        public bool GetCustomData(string key, out string value) =>
            customData.TryGetValue(key, out value);

        /// <summary>Gets custom data.</summary>
        public string GetCustomData(string key)
        {
            if (customData.ContainsKey(key))
                return customData[key];
            else
                return null;
        }

        /// <summary>Sets custom data.</summary>
        public void SetCustomData(string key, string value)
        {
            _ = customData.Set(key, value);
            Save();
        }

        /// <summary>Clears custom data.</summary>
        public void ClearCustomData(string key)
        {
            if (customData.Remove(key))
                Save();
        }

        /// <summary>Specifies whatever collections can be excluded from build.</summary>
        /// <remarks>When <see langword="true"/>, a toggle will be shown in scene manager window.</remarks>
        public bool allowExcludingCollectionsFromBuild
        {
            get => m_allowExcludingCollectionsFromBuild;
            set => m_allowExcludingCollectionsFromBuild = value;
        }

        /// <summary>The profile to use when none is set.</summary>
        public Profile defaultProfile
        {
            get => m_defaultProfile;
            set => m_defaultProfile = value;
        }

        /// <summary>The profile to force everyone in this project to use.</summary>
        public Profile forceProfile
        {
            get => m_forceProfile;
            set => m_forceProfile = value;
        }

        /// <summary>The profile to use during build.</summary>
        public Profile buildProfile => m_buildProfile;

        /// <summary>This is the color of the unity splash screen, used to make fade from splash screen to asm smooth, this is set before building. <see cref="Color.black"/> is used when the unity splash screen is disabled.</summary>
        public Color buildUnitySplashScreenColor => m_unitySplashScreenColor;

        /// <summary>Enables or disables <see cref="InGameToolbarUtility"/> in builds.</summary>
        public bool inGameToolbarEnabled
        {
            get => m_inGameToolbarEnabled;
            set => m_inGameToolbarEnabled = value;
        }

        /// <summary>Gets or sets whatever <see cref="InGameToolbarUtility"/> in should be expanded by default.</summary>
        public bool inGameToolbarExpandedByDefault
        {
            get => m_inGameToolbarExpandedByDefault;
            set => m_inGameToolbarExpandedByDefault = value;
        }

#if UNITY_EDITOR

        /// <summary>Sets the build profile.</summary>
        public void SetBuildProfile(Profile profile)
        {
            if (m_buildProfile != profile)
            {
                m_buildProfile = profile;
                Save();
            }
        }

#endif

        #endregion
        #region Scriptable object

        //Don't allow renaming from UnityEvent
        /// <inheritdoc cref="UnityEngine.Object.name"/>
        public new string name
        {
            get => base.name;
            internal set => base.name = value;
        }

        /// <summary>Saves the scriptable object after modifying.</summary>
        /// <remarks>Only available in editor.</remarks>
        public void Save()
        {
#if UNITY_EDITOR
            ScriptableObjectUtility.Save(this);
#endif
        }

        /// <summary>Mark scriptable object as dirty after modifying.</summary>
        /// <remarks>Only available in editor.</remarks>
        public void MarkAsDirty()
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }


        #endregion
        #region Auto

#if UNITY_EDITOR

        internal void Initialize() =>
            UpdateSplashScreenColor();

        static void UpdateSplashScreenColor()
        {
            var color = PlayerSettings.SplashScreen.show ? PlayerSettings.SplashScreen.backgroundColor : Color.black;
            if (color != SceneManager.settings.project.m_unitySplashScreenColor)
            {
                SceneManager.settings.project.m_unitySplashScreenColor = PlayerSettings.SplashScreen.show ? PlayerSettings.SplashScreen.backgroundColor : Color.black;
                SceneManager.settings.project.Save();
            }
        }

        class Postprocessor : AssetPostprocessor
        {
            void OnPreprocessAsset() =>
                EditorApplication.delayCall += () =>
                {
                    SceneManager.OnInitialized(() =>
                    {
                        if (assetPath == "ProjectSettings/ProjectSettings.asset")
                            UpdateSplashScreenColor();
                    });
                };

        }

#endif

        #endregion

    }

}

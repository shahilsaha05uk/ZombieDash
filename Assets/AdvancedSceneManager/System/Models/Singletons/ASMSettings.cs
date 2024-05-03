using UnityEngine;
using AdvancedSceneManager.Utility;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AdvancedSceneManager.Core;
using AdvancedSceneManager.Models.Enums;
using AdvancedSceneManager.Models.Utility;
using System.Collections.Generic;
using System.Collections;
using System;
using AdvancedSceneManager.Setup;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
using AdvancedSceneManager.Editor.Utility;
#endif

namespace AdvancedSceneManager.Models
{

    /// <summary>Contains the core of ASM assets. Contains <see cref="projectSettings"/> and <see cref="assets"/></summary>
    /// <remarks>Only available in editor.</remarks>
    [ASMFilePath("ProjectSettings/AdvancedSceneManager.asset")]
    public class ASMSettings : ASMScriptableSingleton<ASMSettings>, INotifyPropertyChanged
    {

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
            PropertyChanged?.Invoke(this, new(propertyName));

#if UNITY_EDITOR
        void OnValidate() =>
            OnInitialized(SceneImportUtility.Notify);
#endif

        #endregion
        #region Properties

        #region Helper classes

        [Serializable]
        public class CustomDataDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
        {

            [SerializeField] private SerializableDictionary<TKey, TValue> dict = new();

            public TValue this[TKey key]
            {
                get => dict[key];
                set => dict[key] = value;
            }

            /// <summary>Gets custom data.</summary>
            public bool Get(TKey key, out TValue value) =>
               dict.TryGetValue(key, out value);

            /// <summary>Gets custom data.</summary>
            public TValue Get(TKey key) =>
                dict.ContainsKey(key)
                ? dict[key]
                : default;

            /// <summary>Sets custom data.</summary>
            public void Set(TKey key, TValue value)
            {
                _ = dict.Set(key, value);
                SceneManager.settings.project.Save();
            }

            /// <summary>Clears custom data.</summary>
            public void Clear(TKey key)
            {
                if (dict.Remove(key))
                    SceneManager.settings.project.Save();
            }

            /// <summary>Gets if the key exists.</summary>
            public bool ContainsKey(TKey key) =>
                dict.ContainsKey(key);

            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => dict.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => dict.GetEnumerator();

        }

        [Serializable]
        public class CustomData : CustomDataDictionary<string, string>
        { }

        [Serializable]
        public class SceneData : CustomDataDictionary<string, CustomData>
        { }

        #endregion

        #region AssetRef

        [Header("Assets")]
        [SerializeField] internal List<Profile> m_profiles = new();
        [SerializeField] internal List<Scene> m_scenes = new();
        [SerializeField] internal List<SceneCollection> m_collections = new();
        [SerializeField] internal List<SceneCollectionTemplate> m_collectionTemplates = new();

        [SerializeField] internal ASMSceneHelper m_sceneHelper;
        [SerializeField] internal string m_fallbackScenePath;

        #endregion
        #region Profiles

        [Header("Profiles")]
        [SerializeField] internal Profile m_defaultProfile;
        [SerializeField] internal Profile m_forceProfile;

        [SerializeField] private Profile m_buildProfile;

        /// <summary>The profile to use when none is set.</summary>
        public Profile defaultProfile
        {
            get => m_defaultProfile;
            set { m_defaultProfile = value; OnPropertyChanged(); }
        }

        /// <summary>The profile to force everyone in this project to use.</summary>
        public Profile forceProfile
        {
            get => m_forceProfile;
            set { m_forceProfile = value; OnPropertyChanged(); }
        }

        /// <summary>The profile to use during build.</summary>
        public Profile buildProfile => m_buildProfile;

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
        #region Spam check

        [Header("Spam Check")]
        [SerializeField] private bool m_checkForDuplicateSceneOperations = true;
        [SerializeField] private bool m_preventSpammingEventMethods = false;
        [SerializeField] private float m_spamCheckCooldown = 0.5f;

        /// <summary>By default, ASM checks for duplicate scene operations, since this is usually caused by mistake, but this will disable that.</summary>
        public bool checkForDuplicateSceneOperations
        {
            get => m_checkForDuplicateSceneOperations;
            set { m_checkForDuplicateSceneOperations = value; OnPropertyChanged(); }
        }

        /// <summary>By default, ASM will prevent spam calling event methods (i.e. calling Scene.Open() from a button press), but this will disable that.</summary>
        public bool preventSpammingEventMethods
        {
            get => m_preventSpammingEventMethods;
            set { m_preventSpammingEventMethods = value; OnPropertyChanged(); }
        }

        /// <summary>Sets the default cooldown for <see cref="SpamCheck"/>.</summary>
        public float spamCheckCooldown
        {
            get => m_spamCheckCooldown;
            set { m_spamCheckCooldown = value; OnPropertyChanged(); }
        }

        #endregion
        #region Netcode

#if NETCODE

        [Header("Netcode")]
        [SerializeField] private bool m_isNetcodeValidationEnabled = true;

        /// <summary>Specifies whatever ASM should validate scenes in netcode.</summary>
        public bool isNetcodeValidationEnabled
        {
            get => m_isNetcodeValidationEnabled;
            set { m_isNetcodeValidationEnabled = value; OnPropertyChanged(); }
        }

#endif

        #endregion
        #region Scenes

        [Header("Scenes")]
        [SerializeField] internal List<string> m_blacklist = new();
        [SerializeField] internal List<string> m_whitelist = new();
        [SerializeField] internal SceneData sceneData = new();

        [SerializeField] private bool m_enableCrossSceneReferences;
        [SerializeField] public SceneImportOption m_sceneImportOption = SceneImportOption.Manual;
        [SerializeField] private bool m_allowExcludingCollectionsFromBuild = false;
        [SerializeField] private bool m_reverseUnloadOrderOnCollectionClose = true;

        /// <summary>Gets or sets whatever cross-scene references should be enabled.</summary>
        /// <remarks>Experimental.</remarks>
        public bool enableCrossSceneReferences
        {
            get => m_enableCrossSceneReferences;
            set { m_enableCrossSceneReferences = value; OnPropertyChanged(); }
        }

        /// <summary>Gets or sets when to automatically import scenes.</summary>
        public SceneImportOption sceneImportOption
        {
            get => m_sceneImportOption;
            set { m_sceneImportOption = value; OnPropertyChanged(); }
        }

        /// <summary>Specifies whatever collections can be excluded from build.</summary>
        /// <remarks>When <see langword="true"/>, a toggle will be shown in scene manager window.</remarks>
        public bool allowExcludingCollectionsFromBuild
        {
            get => m_allowExcludingCollectionsFromBuild;
            set { m_allowExcludingCollectionsFromBuild = value; OnPropertyChanged(); }
        }

        /// <summary>Specifies whatever collections should unload scenes in the reverse order.</summary>
        public bool reverseUnloadOrderOnCollectionClose
        {
            get => m_reverseUnloadOrderOnCollectionClose;
            set { m_reverseUnloadOrderOnCollectionClose = value; OnPropertyChanged(); }
        }

        #endregion
        #region ASM info

        [Header("ASM")]
        [SerializeField] internal App.Props m_startProps;

        [SerializeField] private string m_assetPath = "Assets/Settings/AdvancedSceneManager";
        [SerializeField] private CustomData m_customData = new();

        /// <summary>Specifies the path where profiles and imported scenes should be generated to.</summary>
        public string assetPath
        {
            get => m_assetPath;
            set { m_assetPath = value; OnPropertyChanged(); }
        }

        /// <summary>Specifies custom data.</summary>
        public CustomData customData => m_customData;

        #endregion
        #region Splash screen color

        [Header("Splash screen")]
        [SerializeField] private Color m_unitySplashScreenColor = Color.black;

        /// <summary>This is the color of the unity splash screen, used to make the transition from unity splash screen to ASM smooth, this is set before building. <see cref="Color.black"/> is used when the unity splash screen is disabled.</summary>
        public Color buildUnitySplashScreenColor => m_unitySplashScreenColor;

#if UNITY_EDITOR

        [InitializeInEditorMethod]
        static void OnLoad() =>
            SceneManager.OnInitialized(() =>
                BuildUtility.preBuild += (e) => SceneManager.settings.project.UpdateSplashScreenColor());

        void UpdateSplashScreenColor()
        {

            var color =
                PlayerSettings.SplashScreen.show
                ? PlayerSettings.SplashScreen.backgroundColor
                : Color.black;

            if (color != m_unitySplashScreenColor)
            {
                m_unitySplashScreenColor = color;
                Save();
            }

        }

#endif

        #endregion
        #region Locking

        [Header("Locking")]
        [SerializeField] private bool m_allowSceneLocking = true;
        [SerializeField] private bool m_allowCollectionLocking = true;

        /// <summary>Specifies whatever asm will allow locking scenes.</summary>
        public bool allowSceneLocking
        {
            get => m_allowSceneLocking;
            set { m_allowSceneLocking = value; OnPropertyChanged(); }
        }

        /// <summary>Specifies whatever asm will allow locking collections.</summary>
        public bool allowCollectionLocking
        {
            get => m_allowCollectionLocking;
            set { m_allowCollectionLocking = value; OnPropertyChanged(); }
        }

        #endregion
        #region Runtime

        [Header("Runtime")]
        [SerializeField] private SceneCollection m_openCollection;
        [SerializeField] private List<SceneCollection> m_additiveCollections = new();

        internal SceneCollection openCollection
        {
            get
            {
#if UNITY_EDITOR
                return SceneManager.settings.user.m_openCollection;
#else
                return m_openCollection;
#endif
            }
            set
            {
#if UNITY_EDITOR             

                if (!m_openCollection)
                {
                    m_openCollection = null;
                    Save();
                }

                SceneManager.settings.user.m_openCollection = value;
                SceneManager.settings.user.Save();

#else
                m_openCollection = value; 
                Save(); 
#endif
                OnPropertyChanged();
            }
        }

        internal IEnumerable<SceneCollection> openAdditiveCollections
        {
            get
            {
#if UNITY_EDITOR
                return SceneManager.settings.user.m_additiveCollections;
#else
                return m_additiveCollections;
#endif
            }
        }

        internal void AddAdditiveCollection(SceneCollection collection)
        {
#if UNITY_EDITOR
            SceneManager.settings.user.m_additiveCollections.Add(collection);
#else
            m_additiveCollections.Add(collection);
#endif
        }

        internal void RemoveAdditiveCollection(SceneCollection collection)
        {
#if UNITY_EDITOR
            SceneManager.settings.user.m_additiveCollections.Remove(collection);
#else
            m_additiveCollections.Remove(collection);
#endif
        }

        internal void ClearAdditiveCollections()
        {
#if UNITY_EDITOR
            SceneManager.settings.user.m_additiveCollections.Clear();
#else
            m_additiveCollections.Clear();
#endif
        }

        #endregion
        #region IsFirstStart

        [SerializeField] private bool m_isFirstStart;
        [SerializeField] private bool m_boolCheckForLegacyAssets = true;

        internal bool isFirstStart
        {
            get => m_isFirstStart;
            set => m_isFirstStart = value;
        }

        /// <summary>Gets or sets if the check to determine whatever legacy mode should be enabled or not, is enabled.</summary>
        public bool checkForLegacyAssets
        {
            get => m_boolCheckForLegacyAssets;
            set { m_boolCheckForLegacyAssets = value; OnPropertyChanged(); }
        }

        #endregion

        #endregion
        #region Initialize

        static bool isSwitchingToLegacyMode;
        static readonly List<Action> callbacks = new();
        /// <summary>Runs the callback when ASMSettings has initialized.</summary>
        public static void OnInitialized(Action action)
        {

            if (isSwitchingToLegacyMode)
                return;

#if UNITY_EDITOR

            if (SceneManager.settings.project && SceneManager.settings.project.checkForLegacyAssets && ASMInfo.IsLegacySetup())
            {

                isSwitchingToLegacyMode = true;

                if (EditorUtility.DisplayDialog("Upgrading from ASM 1.9...", "Your project was previously using 1.9, or you still have some lingering files.\n\nChoosing to upgrade will remove remaining files.\n\nNote that ASM 2.0 will always require you to re-setup ASM profiles and collections when upgrading from 1.9.", "Switch to legacy mode", "Upgrade to 2.0"))
                {
                    ScriptingDefineUtility.Set("ASM_LEGACY");
                    CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
                }
                else
                    ASMInfo.CleanupLegacyAssets();

                return;
            }

#endif

            if (isInitialized)
                action.Invoke();
            else
                callbacks.Add(action);

            Initialize();

        }

        void OnEnable()
        {

            isInitialized = true;

            foreach (var callback in callbacks)
                callback.Invoke();
            callbacks.Clear();

        }

        internal static bool isInitialized;
        static bool hasInitialized;
        static void Initialize()
        {

            if (hasInitialized)
                return;
            hasInitialized = true;

            _ = instance;

        }

        #endregion

    }

}

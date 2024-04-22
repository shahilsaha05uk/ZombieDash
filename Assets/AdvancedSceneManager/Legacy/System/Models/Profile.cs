using System.ComponentModel;
using System.Runtime.CompilerServices;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

using Object = UnityEngine.Object;
using Lazy.Utility;
using System;

#if UNITY_EDITOR
using UnityEditor;
using AdvancedSceneManager.Editor.Utility;
#endif

namespace AdvancedSceneManager.Models
{

    /// <summary>A profile, contains settings, collections and scenes.</summary>
    public class Profile : ScriptableObject, IASMObject
    {

        public static Profile[] FindAll() =>
            SceneManager.assets.profiles?.ToArray() ?? Array.Empty<Profile>();

        public static Profile Find(Func<Profile, bool> predicate) =>
            FindAll().FirstOrDefault(predicate);

        public static Profile Find(string name) =>
            FindAll().FirstOrDefault(p => p.name == name);

        void OnEnable() =>
            UpgradeToDynamicCollections();

        #region IASMObject

        /// <inheritdoc cref="Object.name"/>
        /// <remarks>See also: <typeparamref name="T"/>.</remarks>
        public new string name =>
            this ? base.name : "(null)";

#if UNITY_EDITOR
        public event PropertyChangedEventHandler PropertyChanged;
#endif

        internal void OnPropertyChanged([CallerMemberName] string name = "")
        {
#if UNITY_EDITOR
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            MarkAsDirty();
#endif
        }

        public void OnPropertyChanged() =>
            OnPropertyChanged("");

        /// <summary>Mark scriptable object as dirty after modifying.</summary>
        /// <remarks>No effect in build.</remarks>
        public void MarkAsDirty()
        {
#if UNITY_EDITOR
            if (this && AssetDatabase.LoadAssetAtPath<Profile>(AssetDatabase.GetAssetPath(this)) is Object o)
                EditorUtility.SetDirty(o);
#endif
        }

        bool IASMObject.Match(string name) =>
            this.name == name;

        #endregion
        #region Prefix

        internal const char ZeroWidthSpace = '​';
        internal static readonly string OldPrefixDelimiter = ZeroWidthSpace + " - " + ZeroWidthSpace;
        internal static readonly string PrefixDelimiter = " - ";

        /// <summary>Gets the prefix that is used on collections in this profile.</summary>
        /// <remarks>This would be <see cref="name"/> + <see cref="PrefixDelimiter"/>.</remarks>
        internal string prefix => name + PrefixDelimiter;

        internal static string RemovePrefix(string name)
        {

            if (name.Contains(OldPrefixDelimiter))
                name = name.Substring(name.IndexOf(OldPrefixDelimiter) + OldPrefixDelimiter.Length);

            if (name.Contains(PrefixDelimiter))
                name = name.Substring(name.IndexOf(PrefixDelimiter) + PrefixDelimiter.Length);

            return name;

        }

        #endregion
        #region Current

        internal static Profile FindProfile(string name) =>
            Find(p => p.name == name);

#if UNITY_EDITOR

        /// <summary>Gets if profile or scenes have lost their references.</summary>
        /// <remarks>Only available in editor.</remarks>
        internal static bool IsStateInvalid()
        {

            //Check scenes
            if (current && current.collections.
                Where(c => c).
                Any(c => (c.scenes?.All(s => !s) ?? false) && (c.m_scenes?.Any(s => AssetDatabase.LoadAssetAtPath<SceneAsset>(s)) ?? false)))
                return true;

            return false;

        }

        /// <summary>Gets the build profile.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static Profile buildProfile => SceneManager.settings.project.buildProfile;

        /// <summary>Gets the default profile.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static Profile defaultProfile => SceneManager.settings.project.defaultProfile;

        /// <summary>Gets the force profile.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static Profile forceProfile => SceneManager.settings.project.forceProfile;

        static Profile m_profile;
        /// <summary>Gets the currently active profile.</summary>
        /// <remarks>Setter only available in editor.</remarks>
        public static Profile current => m_profile;

        /// <summary>Sets the profile to be used by ASM.</summary>
        public static void SetProfile(Profile profile, bool updateBuildSettings = true)
        {

            m_profile = profile;
            SceneManager.settings.local.activeProfile = profile ? profile.name : "";
            SceneManager.settings.local.Save();

            if (Application.isBatchMode)
                Debug.Log("#UCB Profile '" + SceneManager.settings.local.activeProfile + "' set!");

            if (profile && !BuildPipeline.isBuildingPlayer && !Application.isBatchMode)
                SceneManager.settings.project.SetBuildProfile(profile);

            DynamicCollectionUtility.UpdateDynamicCollections(updateBuildSettings: false);
            if (SceneManager.settings.local.assetRefreshTriggers.HasFlag(ASMSettings.Local.AssetRefreshTrigger.ProfileChanged))
                AssetUtility.Refresh(evenIfInPlayMode: false, immediate: true);

            if (updateBuildSettings && !EditorApplication.isPlayingOrWillChangePlaymode)
                BuildUtility.UpdateSceneList();

            onProfileChanged?.Invoke();

        }

        /// <summary>Occurs when <see cref="Profile.current"/> changes.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static event Action onProfileChanged;

#else
        public static Profile current => SceneManager.settings.project.buildProfile;
#endif

        #endregion
        #region Collection and scene lists

        [SerializeField] internal List<SceneCollection> m_collections = new List<SceneCollection>();

        /// <summary>Gets the collections contained within this profile.</summary>
        public IEnumerable<SceneCollection> collections =>
            m_collections;

        /// <summary>Gets the scenes managed by this profile.</summary>
        /// <remarks>Includes both collection and standalone scenes.</remarks>
        public IEnumerable<Scene> scenes => collections.
            Where(c => c).
            SelectMany(c => c.AllScenes()).
            Concat(dynamicCollections.SelectMany(c => c.scenes).Select(s => Scene.Find(s))).
            Concat(specialScenes).
            Where(s => s).
            Distinct();

        /// <summary>Gets default loading screen, splash screen and startup loading screen.</summary>
        public IEnumerable<Scene> specialScenes =>
            new[] { loadingScreen, splashScreen, startupLoadingScreen }.
            Where(s => s).
            Distinct();

        /// <summary>Gets default loading screen, splash screen and startup loading screen.</summary>
        public IEnumerable<string> specialScenePaths =>
            new[] { m_loadingScreen, m_splashScreen, m_startupLoadingScreen }.
            Where(s => !string.IsNullOrEmpty(s)).
            Distinct();

        /// <inheritdoc cref="scenes"/>
        public IEnumerable<string> scenePaths => collections.
            Where(c => c).
            SelectMany(c => c.AllScenePaths()).
            Concat(dynamicCollections.SelectMany(c => c.scenes)).
            Concat(specialScenePaths).
            Where(s => !string.IsNullOrEmpty(s)).
            Distinct();

        /// <summary>Gets the collections that will be opened on startup.</summary>
        /// <remarks>If no collection is explicitly defined to be opened during startup, then the first available collection in list will be returned.</remarks>
        public IEnumerable<SceneCollection> StartupCollections()
        {

            var collections = m_collections.Where(c => c && c.isIncluded && (c.startupOption == CollectionStartupOption.Open || c.startupOption == CollectionStartupOption.OpenAsPersistent)).ToArray();
            if (collections.Any())
                foreach (var c in collections)
                    yield return c;
            else if (this.collections.FirstOrDefault(c => c && c.isIncluded && c.startupOption == CollectionStartupOption.Auto) is SceneCollection collection && collection)
                yield return collection;

        }

#if UNITY_EDITOR

        /// <summary>Create a collection and add it to this profile.</summary>
        /// <remarks>Only available in editor.</remarks>
        public SceneCollection CreateCollection(string name, Action<SceneCollection> initializeBeforeSave = null) =>
            AssetUtility.Create(name, profile: this, initializeBeforeSave);

        /// <summary>Adds a collection to this profile.</summary>
        /// <remarks>Only available in editor.</remarks>
        public void Add(SceneCollection collection) =>
            AssetUtility.AddCollectionToProfile(collection, profile: this);

        /// <summary>Removes a collection from this profile.</summary>
        /// <remarks>Only available in editor.</remarks>
        public void Remove(SceneCollection collection)
        {

            if (collection && m_collections.Remove(collection))
            {

                try
                {

                    var assetPath = AssetDatabase.GetAssetPath(collection);
                    if (!assetPath.EndsWith("/removed/" + collection.name + ".asset"))
                    {

                        var folder = AssetUtility.CollectionFolder(this) + "/removed";
                        var path = folder + "/" + collection.name + ".asset";
                        EditorFolderUtility.EnsureFolderExists(folder);

                        if (AssetDatabase.LoadAssetAtPath<Object>(path) is Object obj && obj)
                            if (!AssetDatabase.DeleteAsset(path))
                                throw new Exception("Could not remove collection. Please try again, or make sure that there aren't any already removed collections with the same name inside the profile collection folder.");

                        var str = AssetDatabase.MoveAsset(assetPath, path);
                        if (!string.IsNullOrEmpty(str))
                            throw new Exception(str);

                    }

                    ArrayUtility.Add(ref m_removedCollections, collection);
                    Save();

                }
                catch (Exception e)
                {
                    if (collection)
                        m_collections.Add(collection);
                    UnityEngine.Debug.LogError(e);
                }

            }

        }

        /// <summary>Restores a collection that has been removed.</summary>
        public void Restore(SceneCollection collection)
        {

            if (!collection || !removedCollections.Contains(collection))
                return;

            var assetPath = AssetDatabase.GetAssetPath(collection);
            var str = AssetDatabase.MoveAsset(assetPath, AssetUtility.CollectionFolder(this) + "/" + collection.name + ".asset");
            if (!string.IsNullOrEmpty(str))
                throw new Exception(str);

            ArrayUtility.Remove(ref m_removedCollections, collection);
            AssetUtility.AddCollectionToProfile(collection, this);


        }

        /// <summary>Clear removed collections.</summary>
        public void ClearRemovedCollections()
        {
            ArrayUtility.Clear(ref m_removedCollections);
            Save();
        }

#endif
        #endregion
        #region Properties

        [SerializeField] private SceneCollection[] m_removedCollections = Array.Empty<SceneCollection>();
        [SerializeField] internal string m_startupScene;
        [SerializeField] internal string m_loadingScreen;
        [SerializeField] internal string m_splashScreen;
        [SerializeField] private bool m_useDefaultPauseScreen = true;
        [SerializeField] private bool m_includeFadeLoadingScene = true;
        [SerializeField] internal string[] m_standalone = Array.Empty<string>();
        [SerializeField] private string m_startupLoadingScreen;
        [SerializeField] private ThreadPriority m_backgroundLoadingPriority = ThreadPriority.BelowNormal;
        [SerializeField] private bool m_enableChangingBackgroundLoadingPriority;
        [SerializeField] internal TagList m_tags = new TagList();
        [SerializeField] private bool m_createCameraDuringStartup = true;
        [SerializeField] private bool m_unloadUnusedAssetsForStandalone = true;
        [SerializeField] private bool m_checkForDuplicateSceneOperations = true;
        [SerializeField] private bool m_preventSpammingEventMethods = false;
        [SerializeField] private float m_spamCheckCooldown = 0.5f;
        [SerializeField] internal List<DynamicCollection> m_dynamicCollections = new List<DynamicCollection>();
        [SerializeField] internal List<string> m_dynamicCollectionPaths = new List<string>();

#if UNITY_EDITOR

        [SerializeField] internal BlacklistUtility.BlacklistModule m_blacklist = new BlacklistUtility.BlacklistModule();

        /// <summary>The collections that have been removed from this profile. These collections will be included in build, unless cleared out from edit collection menu or <see cref="ClearRemovedCollections"/>, but they will not be included when determing the scenes that should be included in build.</summary>
        /// <remarks>Only available in editor.</remarks>
        public SceneCollection[] removedCollections => m_removedCollections;

#endif

        /// <summary>The startup scene.</summary>
        public Scene startupScene
        {
            get => Scene.Find(m_startupScene);
            set { m_startupScene = value ? value.path : ""; OnPropertyChanged(); }
        }

        /// <summary>The loading screen to use during startup.</summary>
        public Scene startupLoadingScreen
        {
            get => Scene.Find(m_startupLoadingScreen);
            set { m_startupLoadingScreen = value ? value.path : ""; OnPropertyChanged(); }
        }

        /// <summary>The default loading screen.</summary>
        public Scene loadingScreen
        {
            get => Scene.Find(m_loadingScreen);
            set { m_loadingScreen = value ? value.path : ""; OnPropertyChanged(); }
        }

        /// <summary>The splash screen.</summary>
        public Scene splashScreen
        {
            get => Scene.Find(m_splashScreen);
            set { m_splashScreen = value ? value.path : ""; OnPropertyChanged(); }
        }

        /// <summary>Enables the default pause screen.</summary>
        /// <remarks>Has no effect while in play mode.</remarks>
        public bool useDefaultPauseScreen
        {
            get => m_useDefaultPauseScreen;
            set { m_useDefaultPauseScreen = value; OnPropertyChanged(); }

        }

        /// <summary>Enables the fade loading scene.</summary>
        /// <remarks>Has no effect while in play mode.</remarks>
        public bool includeFadeLoadingScene
        {
            get => m_includeFadeLoadingScene;
            set { m_includeFadeLoadingScene = value; OnPropertyChanged(); }

        }

        /// <summary><see cref="Application.backgroundLoadingPriority"/> setting is not saved, and must be manually set every time build or editor starts, this property persists the value and automatically sets it during startup.</summary>
        public ThreadPriority backgroundLoadingPriority
        {
            get => m_backgroundLoadingPriority;
            set { m_backgroundLoadingPriority = value; OnPropertyChanged(); }

        }

        /// <summary>Enable or disable ASM automatically changing <see cref="Application.backgroundLoadingPriority"/>.</summary>
        public bool enableChangingBackgroundLoadingPriority
        {
            get => m_enableChangingBackgroundLoadingPriority;
            set { m_enableChangingBackgroundLoadingPriority = value; OnPropertyChanged(); }
        }

        /// <summary>Enable or disable ASM automatically creating a camera during startup.</summary>
        public bool createCameraDuringStartup
        {
            get => m_createCameraDuringStartup;
            set { m_createCameraDuringStartup = value; OnPropertyChanged(); }
        }

        /// <summary>Enable or disable ASM calling <see cref="Resources.UnloadUnusedAssets"/> after standalone scenes has been opened or closed.</summary>
        public bool unloadUnusedAssetsForStandalone
        {
            get => m_unloadUnusedAssetsForStandalone;
            set { m_unloadUnusedAssetsForStandalone = value; OnPropertyChanged(); }
        }

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

        /// <summary>Sets the default cooldown for <see cref="Utility.SpamCheck"/>.</summary>
        public float spamCheckCooldown
        {
            get => m_spamCheckCooldown;
            set { m_spamCheckCooldown = value; OnPropertyChanged(); }
        }

        /// <summary>The layers defined in the tags tab in the scene manager window.</summary>
        public SceneTag[] tagDefinitions = new SceneTag[4] { SceneTag.Default, SceneTag.Persistent, SceneTag.PersistIfPossible, SceneTag.DoNotOpen };

        /// <summary>The paths of which all scenes should be included in build, as a dynamic collection.</summary>
        /// <remarks>These are only evaluated and used in <see cref="BuildUtility"/>.</remarks>
        public string[] dynamicCollectionPaths => m_dynamicCollectionPaths.ToArray();

#if UNITY_EDITOR
        /// <summary>The blacklist settings.</summary>
        /// <remarks>Only available in editor.</remarks>
        public BlacklistUtility.BlacklistModule blacklist => m_blacklist;
#endif

        #endregion
        #region Order

        /// <summary>Returns the order of this collection.</summary>
        public int Order(SceneCollection collection) =>
            collection && collections != null && collections.Contains(collection)
            ? collections.Select((c, i) => (c, i)).FirstOrDefault(c => c.c == collection).i
            : -1;

#if UNITY_EDITOR

        /// <summary>Returns and/or sets the order of this collection in the scene manager window.</summary>
        /// <remarks>Cannot use in build.</remarks>
        public int Order(SceneCollection collection, int? newIndex = null)
        {

            if (m_collections == null)
                m_collections = new List<SceneCollection>();

            if (newIndex.HasValue)
            {

                _ = m_collections.Remove(collection);

                if (!newIndex.HasValue || newIndex.Value == -1)
                {
                    newIndex = collections.Count() - 1;
                    if (newIndex < 0)
                        newIndex = 0;
                }

                if (newIndex > collections.Count())
                    m_collections.AddRange(Enumerable.Repeat<SceneCollection>(null, newIndex.Value - collections.Count()).ToArray());

                m_collections.Insert(newIndex.Value, collection);

            }

            return Order(collection);

        }

#endif

        #endregion
        #region Dynamic collections

        void UpgradeToDynamicCollections()
        {

            if (!this)
                return;

#if UNITY_EDITOR
            if (m_standalone.Any())
            {
                foreach (var scene in m_standalone)
                    Add(scene);
                m_standalone = null;
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            }
#endif

        }

        /// <summary>Gets the dynamic collections specified on this profile.</summary>
        public DynamicCollection[] dynamicCollections
        {
            get
            {
                _ = standalone; //Make sure we have standalone in list 
                return m_dynamicCollections.ToArray();
            }
        }

        /// <summary>Gets the standalone scenes added to this profile.</summary>
        public DynamicCollection standalone
        {
            get
            {

                if (m_dynamicCollections.Count == 0 || !m_dynamicCollections[0].isStandalone)
                {
                    var collection = m_dynamicCollections.FirstOrDefault(c => c.isStandalone) ?? new DynamicCollection();
                    _ = m_dynamicCollections.RemoveAll(c => c.isStandalone);
                    m_dynamicCollections.Insert(0, collection);
#if UNITY_EDITOR
                    Save();
#endif
                }

                return m_dynamicCollections[0];

            }
        }

#if UNITY_EDITOR

        /// <summary>Sets the scenes for a dynamic collection. This clears existing scenes.</summary>
        /// <param name="isAuto">Use this to indicate that this collection was set using the dynamic collection ui in settings.</param>
        /// <remarks>Only available in editor.</remarks>
        internal void Set(string collectionTitle, string[] scenes, bool updateBuildSettings = true, bool save = true, bool isAuto = false)
        {

            Clear(collectionTitle, save: false);
            m_dynamicCollections.Add(new DynamicCollection() { title = collectionTitle, scenes = scenes.ToList(), isAuto = isAuto });

            if (save)
                Save(updateBuildSettings);

        }

        /// <summary>Adds or removed the scene, depending on <paramref name="setValue"/>.</summary>
        /// <param name="setValue">If <see langword="true"/>: add to dynamic collection. If <see langword="false"/>: remove from dynamic collection.</param>
        /// <remarks>Only available in editor.</remarks>
        internal void Set(Scene scene, bool setValue, string collectionTitle = "", bool updateBuildSettings = true)
        {
            if (setValue)
                Add(scene, collectionTitle, updateBuildSettings);
            else
                Remove(scene, collectionTitle, updateBuildSettings);
        }

        /// <summary>Adds or removed the scene, depending on <paramref name="setValue"/>.</summary>
        /// <param name="setValue">If <see langword="true"/>: add to dynamic collection. If <see langword="false"/>: remove from dynamic collection.</param>
        /// <remarks>Only available in editor.</remarks>
        internal void Set(string scene, bool setValue, string collectionTitle = "", bool updateBuildSettings = true)
        {
            if (setValue)
                Add(scene, collectionTitle, updateBuildSettings);
            else
                Remove(scene, collectionTitle, updateBuildSettings);
        }

        /// <summary>Adds a scene to a dynamic collection on this profile.</summary>
        /// <remarks>Only available in editor.</remarks>
        public void Add(Scene scene, string collectionTitle = "", bool updateBuildSettings = true)
        {
            if (scene)
                Add(scene.path, collectionTitle, updateBuildSettings);
        }

        /// <remarks>Only available in editor.</remarks>
        void Add(string scene, string collectionTitle = "", bool updateBuildSettings = true)
        {

            if (collectionTitle is null)
                collectionTitle = "";

            var collection = m_dynamicCollections.FirstOrDefault(c => c.title == collectionTitle);
            if (collection == null)
                m_dynamicCollections.Add(collection = new DynamicCollection() { title = collectionTitle });

            if (!collection.scenes.Contains(scene))
                collection.scenes.Add(scene);

            EditorUtility.SetDirty(this);

            if (!EditorApplication.isPlayingOrWillChangePlaymode && updateBuildSettings)
            {
                CoroutineUtility.Run(AssetDatabase.SaveAssets, after: 0.1f);
                BuildUtility.UpdateSceneList();
            }

        }

        /// <summary>Removes the scene from the specified dynamic collection.</summary>
        /// <remarks>Only available in editor.</remarks>
        public void Remove(Scene scene, string collectionTitle = "", bool updateBuildSettings = true)
        {
            if (scene)
                Remove(scene.path, collectionTitle, updateBuildSettings);
        }

        /// <remarks>Only available in editor.</remarks>
        void Remove(string scene, string collectionTitle = "", bool updateBuildSettings = true)
        {

            if (string.IsNullOrWhiteSpace(scene))
                return;

            if (collectionTitle is null)
                collectionTitle = "";

            var collection = m_dynamicCollections.FirstOrDefault(c => c.title == collectionTitle)?.scenes.Remove(scene);

            EditorUtility.SetDirty(this);

            if (!EditorApplication.isPlayingOrWillChangePlaymode && updateBuildSettings)
            {
                CoroutineUtility.Run(AssetDatabase.SaveAssets, after: 0.1f);
                BuildUtility.UpdateSceneList();
            }

        }

        /// <summary>Clears a dynamic collection.</summary>
        /// <remarks>Only available in editor.</remarks>
        public void Clear(string collectionTitle, bool save = true, bool updateBuildSettings = true)
        {
            _ = m_dynamicCollections.RemoveAll(c => c.title == collectionTitle);
            if (save)
                Save(updateBuildSettings);
        }

        void Save(bool updateBuildSettings = true)
        {

            EditorUtility.SetDirty(this);
#if UNITY_2019
            CoroutineUtility.Run(AssetDatabase.SaveAssets, after: 0.1f);
#else
            CoroutineUtility.Run(() => AssetDatabase.SaveAssetIfDirty(this), after: 0.1f);
#endif

            if (updateBuildSettings && !EditorApplication.isPlayingOrWillChangePlaymode)
                BuildUtility.UpdateSceneList();

        }

#endif

        /// <summary>Gets if the scene is added in a dynamic collection.</summary>
        public bool IsSet(string key, string scene) =>
            m_dynamicCollections.FirstOrDefault(c => c.title == key)?.scenes?.Contains(scene) ?? false;

        /// <summary>Gets if the scene is added in a dynamic collection.</summary>
        public bool IsSet(string scene, bool includeStandalone = true) =>
            includeStandalone
            ? m_dynamicCollections.Any(c => c.scenes.Contains(scene))
            : m_dynamicCollections.Any(c => c.title != "" && c.scenes.Contains(scene));

        #endregion

    }

}

﻿using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using AdvancedSceneManager.Utility;
using AdvancedSceneManager.Models.Internal;
using AdvancedSceneManager.Models.Utility;
using AdvancedSceneManager.Models.Enums;
using System.Runtime.CompilerServices;

#if UNITY_EDITOR
using UnityEditor;
using AdvancedSceneManager.Editor.Utility;
#endif

namespace AdvancedSceneManager.Models
{

    /// <summary>A profile, contains settings, collections.</summary>
    public class Profile : ASMModel
    {

        #region Editor helpers

#if UNITY_EDITOR

        /// <summary>Gets the cached <see cref="SerializedObject"/> for the current profile.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static SerializedObject serializedObject { get; private set; }

        void OnEnable()
        {
            if (serializedObject is null && SceneManager.profile == this)
                serializedObject = new(this);
        }

        protected override void OnValidate()
        {
            UpdatePrefix();
            base.OnValidate();
            if (current == this)
                EditorApplication.delayCall += BuildUtility.UpdateSceneList;
        }

        public override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);
            if (propertyName is nameof(splashScreen) or nameof(loadingScreen))
                BuildUtility.UpdateSceneList();
        }

#endif

        #endregion
        #region Prefix

        internal override void OnNameChanged() => UpdatePrefix();

        void UpdatePrefix()
        {
#if UNITY_EDITOR
            collections.ForEach(c => c.SetPrefix(prefix));
#endif
        }

        internal const string PrefixDelimiter = " - ";

        /// <summary>Gets the prefix that is used on collections in this profile.</summary>
        /// <remarks>This would be <see cref="name"/> + <see cref="PrefixDelimiter"/>.</remarks>
        internal string prefix => name + PrefixDelimiter;

        internal static string RemovePrefix(string name)
        {

            if (name.Contains(PrefixDelimiter))
                name = name[(name.IndexOf(PrefixDelimiter) + PrefixDelimiter.Length)..];

            return name;

        }

        #endregion
        #region Current

        /// <summary>Gets the currently active profile.</summary>
        public static Profile current =>
#if UNITY_EDITOR
            SceneManager.settings.user ? SceneManager.settings.user.activeProfile : null;
#else
            SceneManager.settings.project ? SceneManager.settings.project.buildProfile : null;
#endif

#if UNITY_EDITOR

        /// <summary>Gets the build profile.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static Profile buildProfile => SceneManager.settings.project.buildProfile;

        /// <summary>Gets the default profile.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static Profile defaultProfile => SceneManager.settings.project.defaultProfile;

        /// <summary>Gets the force profile.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static Profile forceProfile => SceneManager.settings.project.forceProfile;

        /// <summary>Occurs when <see cref="Profile.current"/> changes.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static event Action onProfileChanged;

        /// <summary>Sets the profile to be used by ASM.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static void SetProfile(Profile profile, bool updateBuildSettings = true)
        {

            serializedObject = profile ? new(profile) : null;

            SceneManager.settings.user.activeProfile = profile;
            SceneManager.settings.user.Save();

            if (EditorUtility.IsPersistent(profile) && !BuildPipeline.isBuildingPlayer && !Application.isBatchMode)
                SceneManager.settings.project.SetBuildProfile(profile);

            if (updateBuildSettings)
                BuildUtility.UpdateSceneList();

            onProfileChanged?.Invoke();

        }

#endif

        #endregion
        #region Fields

        [SerializeField] private Scene m_startupScene;
        [SerializeField] private Scene m_loadingScreen;
        [SerializeField] private Scene m_splashScreen;
        [SerializeField] private Scene m_startupLoadingScreen;
        [SerializeField] private ThreadPriority m_backgroundLoadingPriority = ThreadPriority.BelowNormal;
        [SerializeField] private bool m_enableChangingBackgroundLoadingPriority;
        [SerializeField] private bool m_createCameraDuringStartup = true;
        [SerializeField] private bool m_unloadUnusedAssetsForStandalone = true;

        [SerializeField] private List<SceneCollection> m_collections = new();
        [SerializeField] private List<DynamicCollection> m_dynamicCollections = new();
        [SerializeField] private List<ISceneCollection> m_removedCollections = new();
        [SerializeField] private StandaloneCollection m_standaloneDynamicCollection = new();

        #endregion
        #region Properties

        /// <summary>Gets the scenes managed by this profile.</summary>
        /// <remarks>Includes both collection and standalone scenes.</remarks>
        public IEnumerable<Scene> scenes =>
            allCollections.
            Where(HasValue).
            Where(IsIncluded).
            SelectMany(AllScenes).
            Concat(specialScenes).
            Where(s => s).
            Distinct();

        bool HasValue(ISceneCollection collection) =>
            collection is ScriptableObject c
            ? c
            : collection is not null;

        bool IsIncluded(ISceneCollection collection) =>
            collection is not SceneCollection c || c.isIncluded;

        IEnumerable<Scene> AllScenes(ISceneCollection collection) =>
            collection is SceneCollection c
            ? c.allScenes
            : (collection?.scenes ?? Array.Empty<Scene>());

        /// <summary>Gets the collections contained within this profile.</summary>
        public IEnumerable<SceneCollection> collections => m_collections;

        /// <summary>Gets the dynamic collections contained within this profile.</summary>
        public IEnumerable<DynamicCollection> dynamicCollections => m_dynamicCollections;

        /// <summary>Gets the standalone scenes contained within this profile.</summary>
        public StandaloneCollection standaloneScenes => m_standaloneDynamicCollection;

        /// <summary>Gets the scenes flagged to open on startup.</summary>
        public IEnumerable<Scene> startupScenes => standaloneScenes.Where(s => s && s.openOnStartup);

        /// <summary>Gets all removed collections in this profile.</summary>
        /// <remarks>Removed collections still exist until deleted, and may be manually opened, but they will not be listed in <see cref="collections"/> or <see cref="dynamicCollections"/>.</remarks>
        public IEnumerable<ISceneCollection> removedCollections => m_removedCollections;

        /// <summary>Gets <see cref="collections"/>, <see cref="standaloneScenes"/>, <see cref="dynamicCollections"/>.</summary>
        public IEnumerable<ISceneCollection> allCollections =>
            collections.Cast<ISceneCollection>().Concat(standaloneScenes).Concat(dynamicCollections.Cast<ISceneCollection>());

        /// <summary>Gets default loading screen, splash screen and startup loading screen.</summary>
        /// <remarks><see langword="null"/> is filtered out.</remarks>
        public IEnumerable<Scene> specialScenes =>
            new[] { loadingScreen, splashScreen, startupLoadingScreen }.
            Where(s => s).
            Distinct();

        /// <summary>Gets the collections that will be opened on startup.</summary>
        /// <remarks>If no collection is explicitly defined to be opened during startup, then the first available collection in list will be returned.</remarks>
        public IEnumerable<SceneCollection> startupCollections
        {
            get
            {

                var collections = m_collections.Where(c => c && c.isIncluded && c.startupOption == CollectionStartupOption.Open).ToArray();
                if (collections.Length > 0)
                    foreach (var c in collections)
                        yield return c;

                else if (this.collections.FirstOrDefault(c => c && c.isIncluded && c.startupOption == CollectionStartupOption.Auto) is SceneCollection collection && collection)
                    yield return collection;

            }
        }

        /// <summary>The startup scene.</summary>
        public Scene startupScene
        {
            get => m_startupScene;
            set { m_startupScene = value; OnPropertyChanged(); }
        }

        /// <summary>The loading screen to use during startup.</summary>
        public Scene startupLoadingScreen
        {
            get => m_startupLoadingScreen;
            set { m_startupLoadingScreen = value; OnPropertyChanged(); }
        }

        /// <summary>The default loading screen.</summary>
        public Scene loadingScreen
        {
            get => m_loadingScreen;
            set { m_loadingScreen = value; OnPropertyChanged(); }
        }

        /// <summary>The splash screen.</summary>
        public Scene splashScreen
        {
            get => m_splashScreen;
            set { m_splashScreen = value; OnPropertyChanged(); }
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

        #endregion
        #region Methods

        /// <summary>Gets whatever the specified collection is a startup collection.</summary>
        public bool IsStartupCollection(SceneCollection collection) =>
            startupCollections.Contains(collection);

        /// <summary>Gets the index of the specified collection.</summary>
        public int IndexOf(SceneCollection collection) => m_collections.IndexOf(collection);

        /// <summary>Gets the index of the specified collection.</summary>
        public int IndexOf(DynamicCollection collection) => m_dynamicCollections.IndexOf(collection);

        /// <summary>Gets whatever this profile contains the specified collection.</summary>
        public bool Contains(ISceneCollection collection, bool checkRemoved = false) =>
            m_collections.Contains(collection) || m_dynamicCollections.Contains(collection) || standaloneScenes == collection || (checkRemoved && removedCollections.Contains(collection));

        /// <summary>Finds all collection that the scene is included in. Includes dynamic collections.</summary>
        public IEnumerable<ISceneCollection> FindCollections(Scene scene) =>
            scene
            ? allCollections.Where(c => c.Contains(scene))
            : Enumerable.Empty<ISceneCollection>();

        #region Create / Remove

#if UNITY_EDITOR

        #region Profile

        #region Default properties

        InputButton escape => new() { path = "/Keyboard/escape", name = "escape" };
        InputButton tab => new() { path = "/Keyboard/tab", name = "start" };
        InputButton start => new() { path = "/Keyboard/start", name = "tab" };
        InputButton select => new() { path = "/Keyboard/select", name = "select" };

        const string defaultCollectionDescription =
                "These are scenes that ASM provides out-of-the-box as a convenience, these are listed " +
                "here to make sure they are included in build by default." +
                "\n\n" +
                "If you aren't using any of these, you may remove this dynamic collection.";

        void ImportDefaultScenes()
        {
            var collection = CreateDynamicCollection("Assets/AdvancedSceneManager/Defaults", "ASM Defaults");
            collection.description = defaultCollectionDescription;
        }

        internal void SetDefaults()
        {
            loadingScreen = SceneManager.assets.defaults.fadeScreen;
            startupLoadingScreen = SceneManager.assets.defaults.fadeScreen;
            splashScreen = SceneManager.assets.defaults.splashScreen;
        }

        void AddDefaultCollections()
        {
            AddCollection("Startup (persistent)", true);
            AddCollection("Main menu");
        }

        void AddCollection(string title, bool openAsPersistent = false)
        {
            var collection = CreateCollection(title);
            collection.startupOption = CollectionStartupOption.Open;
            collection.openAsPersistent = openAsPersistent;
            collection.Save();
        }

        void AddDefaultBindingScenes()
        {

#if INPUTSYSTEM
            AddDefaultBindingScene(SceneManager.assets.defaults.pauseScreen, escape, start);
            AddDefaultBindingScene(SceneManager.assets.defaults.inGameToolbar, tab, select);
            Save();
#endif

        }

        void AddDefaultBindingScene(Scene scene, params InputButton[] buttons)
        {

            if (!scene)
                return;

            standaloneScenes.Add(scene);
            standaloneScenes.GetBinding(scene).SetButtons(InputBindingInteractionType.Toggle, buttons);

        }

        #endregion

        /// <summary>Creates a new profile, with default scenes and collections.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static Profile Create(string name)
        {

            var profile = CreateInternal<Profile>(name);
            profile.ImportDefaultScenes();
            profile.SetDefaults();

            //Needs to be added to AssetRef before AddDefaultCollection()
            Assets.Add(profile);

            profile.AddDefaultCollections();
            profile.AddDefaultBindingScenes();

            return profile;
        }

        /// <summary>Creates a new empty profile.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static Profile CreateEmpty(string name, bool useDefaultSpecialScenes = true)
        {

            var profile = CreateInternal<Profile>(name);

            if (useDefaultSpecialScenes)
                profile.SetDefaults();

            Assets.Add(profile);

            return profile;

        }

        /// <summary>Deletes the specified profile.</summary>
        public static void Delete(Profile profile)
        {

            if (current == profile)
                SetProfile(null);

            Assets.Remove(profile);

        }

        #endregion
        #region Collection

        /// <summary>Creates a new collection with title 'New collection'.</summary>
        public void CreateCollection() =>
            CreateCollection(out _);

        public const string NewCollectionTitle = "New collection";

        /// <summary>Creates a new collection with title 'New collection'.</summary>
        public void CreateCollection(out SceneCollection collection) =>
            collection = CreateCollection(NewCollectionTitle);

        /// <summary>Create a collection and add it to this profile.</summary>
        /// <remarks>Only available in editor.</remarks>
        public SceneCollection CreateCollection(string title)
        {

            var collection = CreateInternal<SceneCollection>(title);
            collection.SetTitleAfterCreation(prefix, title);

            if (!collection)
                throw new InvalidOperationException("Something went wrong creating collection.");

            AssetDatabase.AddObjectToAsset(collection, this);
            Assets.Add(collection);
            m_collections.Add(collection);

            return collection;

        }

        /// <summary>Create a collection from a template.</summary>
        /// <remarks>Only available in editor.</remarks>
        public SceneCollection CreateCollection(SceneCollectionTemplate template)
        {

            if (!template)
                throw new ArgumentNullException(nameof(template));

            var collection = CreateInternal<SceneCollection>(template.title);
            var id = collection.id;
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(template), collection);
            collection.m_id = id;
            AssetDatabase.AddObjectToAsset(collection, this);
            Assets.Add(collection);
            m_collections.Add(collection);

            return collection;

        }

        /// <summary>Removes a collection from this profile. This adds it to undo.</summary>
        /// <remarks>Only available in editor.</remarks>
        public void Remove(ISceneCollection collection)
        {

            if (collection is not StandaloneCollection && Contains(collection))
            {

                if (collection is DynamicCollection dc)
                    _ = m_dynamicCollections.Remove(dc);
                else if (collection is SceneCollection sc)
                    _ = m_collections.Remove(sc);

                m_removedCollections.Add(collection);
                Save();

            }

        }

        /// <summary>Restores a collection that has been removed.</summary>
        /// <remarks>Only available in editor.</remarks>
        public void Restore(ISceneCollection collection)
        {

            if (m_removedCollections.Remove(collection))
            {

                if (collection is DynamicCollection dc)
                    m_dynamicCollections.Add(dc);
                else if (collection is SceneCollection sc)
                    m_collections.Add(sc);

                Save();

            }

        }

        /// <summary>Deletes a collection from this profile. This does not add it to undo.</summary>
        /// <remarks>Only available in editor.</remarks>
        public void Delete(ISceneCollection collection)
        {

            if (m_removedCollections.Remove(collection) && collection is SceneCollection c)
            {
                AssetDatabase.RemoveObjectFromAsset(c);
                Assets.Remove(c);
            }

        }

        /// <summary>Clear removed collections.</summary>
        /// <remarks>Only available in editor.</remarks>
        public void ClearRemovedCollections()
        {

            foreach (var collection in removedCollections.OfType<SceneCollection>())
            {
                AssetDatabase.RemoveObjectFromAsset(collection);
                Assets.Remove(collection);
            }

            m_removedCollections.Clear();
            Save();

        }

        /// <summary>Clear <see cref="collections"/>, <see cref="dynamicCollections"/>, <see cref="removedCollections"/>.</summary>
        /// <remarks>Only available in editor.</remarks>
        public void ClearCollections()
        {

            foreach (var collection in collections)
            {
                AssetDatabase.RemoveObjectFromAsset(collection);
                Assets.Remove(collection);
            }

            m_collections.Clear();
            m_dynamicCollections.Clear();
            m_removedCollections.Clear();

            Save();

        }

        #endregion
        #region DynamicCollection

        /// <summary>Creates a dynamic collection.</summary>
        public void CreateDynamicCollection() =>
            CreateDynamicCollection("", "New dynamic collection");

        /// <summary>Creates a dynamic collection with the specified path.</summary>
        public DynamicCollection CreateDynamicCollection(string path, string title = null)
        {

            var collection = new DynamicCollection
            {
                title = title,
                path = path
            };

            m_dynamicCollections.Add(collection);
            Save();

            return collection;

        }

        /// <summary>Deletes the dynamic collection with the specified path.</summary>
        public void DeleteDynamicCollection(string path)
        {

            if (dynamicCollections.FirstOrDefault(c => c.path == path) is DynamicCollection collection)
            {
                _ = m_dynamicCollections.Remove(collection);
                Save();
            }

        }

        #endregion

#endif

        #endregion

        #endregion
        #region Find

        /// <summary>Gets 't:AdvancedSceneManager.Models.Profile', the string to use in <see cref="AssetDatabase.FindAssets(string)"/>.</summary>
        public readonly static string AssetSearchString = "t:" + typeof(Profile).FullName;

        /// <summary>Finds the profile with the specified name or id.</summary>
        public static Profile Find(string q) =>
            SceneManager.assets.profiles.Find(q);

        /// <summary>Finds the profile with the specified name or id.</summary>
        public static bool TryFind(string q, out Profile profile) =>
            SceneManager.assets.profiles.TryFind(q, out profile);

        #endregion

        public override string ToString(int indent) =>
            base.ToString(indent) + $"Profile: {name}\n\n{string.Join("\n", collections.Select(c => c.ToString(indent + 1)))}";

    }

}

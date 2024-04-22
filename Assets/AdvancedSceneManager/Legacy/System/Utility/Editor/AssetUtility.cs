#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AdvancedSceneManager.Core;
using AdvancedSceneManager.Editor.Window;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace AdvancedSceneManager.Editor.Utility
{

    /// <summary>Provides utility functions for working with assets.</summary>
    /// <remarks>Only available in editor.</remarks>
    public static class AssetUtility
    {

#pragma warning disable CS0067 //Event is unused

        public static event Action onAssetsChanged;
        public static event Action onAssetsCleared;
        public static event Action<string[]> onAssetsSaved;

        #region AssetsSaved

        /// <summary>Provides an event that is called when <see cref="AssetModificationProcessor"/>.OnWillSaveAssets(string[] paths) is called.</summary>
        /// <remarks>Only available in editor.</remarks>
        class AssetsSavedUtility : UnityEditor.AssetModificationProcessor
        {

            static string[] OnWillSaveAssets(string[] paths)
            {
                onAssetsSaved?.Invoke(paths);
                return paths;
            }

        }

        #endregion
        #region AssetRefreshUtility proxy

        /// <summary>If <see langword="false"/>, then assets will not be refreshed, this will mean that no Scene ScriptableObject will be created when a SceneAsset added, and a Scene will also not be removed when its associated SceneAsset is removed.</summary>
        public static bool allowAssetRefresh { get; set; } = true;

        /// <summary>Get if ASM is refreshing assets.</summary>
        public static bool isRefreshing { get; set; } //Set by AssetRefreshUtility

        internal static event Action<(bool full, bool immediate)> OnRefreshRequest;

        /// <summary>Requests ASM to perform an asset refresh.</summary>
        public static void Refresh() =>
            Refresh(evenIfInPlayMode: false, immediate: false);

        /// <summary>Requests ASM to perform an asset refresh.</summary>
        public static void Refresh(bool evenIfInPlayMode, bool immediate) =>
            OnRefreshRequest?.Invoke((evenIfInPlayMode, immediate));

        #endregion
        #region AssetDatabase.DisallowAutoRefresh helper

        static readonly List<object> keys = new List<object>();

        /// <summary>Calls <see cref="AssetDatabase.DisallowAutoRefresh"/>, but uses keys instead of a counter.</summary>
        public static void DisallowAutoRefresh(object key)
        {
            if (!keys.Contains(key))
            {
                keys.Add(key);
                if (keys.Count == 1)
                    AssetDatabase.DisallowAutoRefresh();
            }
        }

        /// <summary>Calls <see cref="AssetDatabase.AllowAutoRefresh"/>, but uses keys instead of a counter.</summary>
        public static void AllowAutoRefresh(object key)
        {
            if (keys.Remove(key) && keys.Count == 0)
                AssetDatabase.AllowAutoRefresh();
        }

        #endregion
        #region Ignore

        static internal List<string> ignore = new List<string>();

        /// <summary>Make ASM asset refresh ignore the scene at the specified path.</summary>
        public static void Ignore(string path)
        {
            if (!ignore.Contains(path))
                ignore.Add(path);
        }

        /// <summary>Gets if the scene should be ignored by ASM asset refresh.</summary>
        public static bool IsIgnored(string path) => ignore.Contains(path);

        #endregion
        #region Profile

        /// <summary>Duplicates active profile and assigns it as active.</summary>
        public static void DuplicateProfileAndAssign()
        {
            var profile = DuplicateProfile();
            if (profile)
                Profile.SetProfile(profile);
        }

        /// <summary>Creates a new profile and assigns it as active.</summary>
        public static void CreateProfileAndAssign(bool promptBlacklist = true)
        {
            var profile = CreateProfile(name: null, promptBlacklist);
            if (profile)
                Profile.SetProfile(profile);
        }

        /// <summary>Duplicates the active profile.</summary>
        public static Profile DuplicateProfile()
        {

            if (!Profile.current)
                return null;

            var profile = CreateProfile(() => Object.Instantiate(Profile.current), promptBlacklist: false);
            if (!profile)
                return null;

            var i = 0f;

            profile.m_collections.Clear();
            foreach (var collection in Profile.current.collections)
            {
                i += 1;
                EditorUtility.DisplayProgressBar("Duplicating profile...", "", i / Profile.current.collections.Count());
                var c = Object.Instantiate(collection);
                Add(c, profile, false);
                _ = AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(c), c.name.Replace("(Clone)", ""));
            }

            EditorUtility.ClearProgressBar();

            return profile;

        }

        /// <summary>Create a new profile.</summary>
        public static Profile CreateProfile(string name = null, bool promptBlacklist = true)
        {

            var profile = CreateProfile(() => ScriptableObject.CreateInstance<Profile>(), name, promptBlacklist);

            if (!profile)
                return null;

            profile.loadingScreen = LoadingScreenUtility.fade;
            profile.startupLoadingScreen = LoadingScreenUtility.fade;

            profile.m_dynamicCollectionPaths.Add("Assets/AdvancedSceneManager/Defaults");
            _ = SceneCollectionUtility.Create("Startup (persistent)", profile).startupOption = CollectionStartupOption.OpenAsPersistent;
            _ = SceneCollectionUtility.Create("Main menu", profile).startupOption = CollectionStartupOption.Open;

            AssetRef.instance.Add(profile);

            return profile;

        }

        static Profile CreateProfile(Func<Profile> create, string name = null, bool promptBlacklist = true)
        {

            BlacklistUtility.BlacklistModule blacklist = null;
            if (name == null || promptBlacklist)
                if (!CreateProfilePrompt(ref name, promptBlacklist, out blacklist))
                    return null;

            var path = GetDefaultAssetPath<Profile>() + "/" + name + ".asset";
            var obj = create?.Invoke();

            if (promptBlacklist)
                obj.m_blacklist = blacklist;

            EditorFolderUtility.EnsureFolderExists(Path.GetDirectoryName(path));

            AssetDatabase.CreateAsset(obj, path);
            AssetDatabase.ImportAsset(path);

            return obj;

        }

        static bool CreateProfilePrompt(ref string name, bool promptBlacklist, out BlacklistUtility.BlacklistModule blacklist)
        {

            blacklist = null;
            if (promptBlacklist)
                blacklist = Profile.current ? Profile.current.blacklist.Clone() : new BlacklistUtility.BlacklistModule();

            var window = EditorWindow.CreateInstance<EditorWindow>();
            window.titleContent = new GUIContent("Create profile...");

            var scroll = new ScrollView();
            scroll.style.flexWrap = Wrap.Wrap;

            var cancelButton = new Button() { text = "Cancel" };
            var doneButton = new Button() { text = "Done" };
            var footer = new VisualElement();
            var spacer = new VisualElement();

            footer.Add(cancelButton);
            footer.Add(spacer);
            footer.Add(doneButton);

            var profileGUI = new CreateProfileGUI(doneButton, "", blacklist, hideBlacklist: !promptBlacklist);

            scroll.Add(profileGUI.element);
            window.rootVisualElement.Add(scroll);
            window.rootVisualElement.Add(footer);
            profileGUI.element.style.marginBottom = profileGUI.element.style.marginLeft = profileGUI.element.style.marginRight = profileGUI.element.style.marginTop = 22;

            spacer.AddToClassList("spacer");
            footer.name = "tab-nav";
            cancelButton.AddToClassList("visible");
            doneButton.AddToClassList("visible");
            window.rootVisualElement.styleSheets.Add(profileGUI.style);

            EditorApplication.delayCall += () =>
            {
                var maxSize = window.maxSize;
                var minSize = window.minSize;
                window.maxSize = new Vector2(800, profileGUI.element.resolvedStyle.height + 60);
                window.minSize = new Vector2(400, profileGUI.element.resolvedStyle.height + 60);
                window.maxSize = new Vector2(800, 2000);
            };

            bool? result = null;
            cancelButton.clicked += () => { result = false; window.Close(); };
            doneButton.clicked += () => { result = true; window.Close(); };

            window.ShowModal();
            if (result == true)
                name = profileGUI.profileName;

            return result ?? false;

        }

        public static void DeleteProfile(Profile profile)
        {
            if (Profile.current == profile)
                Profile.SetProfile(null);
            AssetRef.instance.Remove(profile);
            _ = AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(profile));
        }

        class OnAssetsChanged : UnityEditor.AssetModificationProcessor
        {

            static AssetDeleteResult OnWillDeleteAsset(string asset, RemoveAssetOptions __)
            {

                var profile = AssetDatabase.LoadAssetAtPath<Profile>(asset);
                if (profile)
                {

                    if (!Prompt())
                        return AssetDeleteResult.FailedDelete;

                    _ = AssetDatabase.DeleteAsset(CollectionFolder(profile));

                }

                return AssetDeleteResult.DidNotDelete;

            }

            static bool Prompt() =>
                EditorUtility.DisplayDialog("Deleting profile...", "Profile is about to be deleted, this will also delete all associated collections, are you sure?", "Yes", "No", DialogOptOutDecisionType.ForThisSession, "ASM.PromptDeleteCollections");

        }

        #endregion
        #region Asset path

        /// <summary>Gets the path where ASM stores its assets.</summary>
        public static string assetPath => AssetRef.path;

        /// <summary>Gets the default path for <typeparamref name="T"/>.</summary>
        public static string GetDefaultAssetPath<T>()
        {
            var path = assetPath.TrimEnd('/') + '/';
            return path + typeof(T).Name + "s";
        }

        static string GetPath<T>(T obj, Profile profile = null) where T : ScriptableObject, IASMObject =>
            GetPath(obj, obj.name, profile);

        static string GetPath<T>(T obj, string name, Profile profile = null) where T : Object, IASMObject
        {
            if (obj is SceneCollection collection)
            {
                if (!profile)
                    profile = collection.FindProfile();
                if (!profile)
                    return "";
                return GetPath(obj, name, GetDefaultAssetPath<SceneCollection>() + "/" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(profile)), profile.collections.Where(c => c && c != collection).Select(c => ((ScriptableObject)c).name).ToArray());
            }
            else if (obj is Scene scene)
                return GetPath(obj, name, GetDefaultAssetPath<Scene>(), SceneManager.assets.allScenes.Where(s => s && s != scene).Select(s => s.name).ToArray());
            return "";
        }

        static string GetPath<T>(T obj, string name, string path, string[] names) where T : Object, IASMObject
        {
            if (obj == null)
                return "";
            name = ObjectNames.GetUniqueName(names, name);
            obj.name = name;
            return path + "/" + name + ".asset";
        }

        /// <summary>Gets the path to a profile collection folder.</summary>
        public static string CollectionFolder(Profile profile) =>
            GetDefaultAssetPath<SceneCollection>() + "/" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(profile));

        #endregion
        #region Remove

        /// <summary>Removes the asset.</summary>
        public static void Remove<T>(T obj) where T : ScriptableObject, IASMObject
        {

            if (obj == null)
                return;

            var path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrWhiteSpace(path))
                return;

            _ = AssetDatabase.DeleteAsset(path);

            AssetRef.instance.Remove(obj);
            if (obj is SceneCollection c)
                foreach (var profile in Profile.FindAll())
                    if (profile.collections.Contains(c))
                    {
                        profile.Remove(c);
                        profile.OnPropertyChanged();
                    }

            AssetDatabase.Refresh();
            onAssetsChanged?.Invoke();

        }

        /// <summary>Remove all null refs from <see cref="collections"/> and <see cref="scenes"/>.
        static internal void Cleanup()
        {

            foreach (var profile in SceneManager.assets.profiles)
                if (profile.m_collections.RemoveAll(c => !c) > 0)
                    profile.MarkAsDirty();

            AssetRef.instance.Cleanup();

        }

        /// <summary>Clear assets.</summary>
        public static void Clear()
        {

            allowAssetRefresh = false;
            var key = new object();
            DisallowAutoRefresh(key);

            foreach (var asset in SceneManager.assets.allCollections.Cast<ScriptableObject>().Concat(SceneManager.assets.allScenes))
                _ = AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(asset));

            foreach (var profile in Profile.FindAll())
                _ = AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(profile));

            AssetRef.instance.Clear();

            AllowAutoRefresh(key);
            allowAssetRefresh = true;
            AssetDatabase.Refresh();
            onAssetsChanged?.Invoke();

        }

        #endregion
        #region Add

        /// <summary>Adds the asset.</summary>
        public static void Add<T>(T obj, Profile profile = null, bool import = true) where T : ScriptableObject, IASMObject
        {

            if (!profile)
                profile = Profile.current;

            var path = GetPath(obj, profile);
            if (path == "")
                throw new Exception("Collection was not associated with a profile, please manually add it to one.");

            EditorFolderUtility.EnsureFolderExists(Path.GetDirectoryName(path));

            AssetDatabase.CreateAsset(obj, path);

            obj = AssetDatabase.LoadAssetAtPath<T>(path);

            if (obj is SceneCollection c && c)
            {
                profile.m_collections.Add(c);
                profile.Save();
            }

            AssetRef.instance.Add(obj);
            onAssetsChanged?.Invoke();

        }

        /// <summary>Adds the collection to the profile.</summary>
        /// <remarks>This removes collection from profile, if already associated with one.</remarks>
        public static void AddCollectionToProfile(SceneCollection collection, Profile profile)
        {

            foreach (var p in Profile.FindAll())
                if (p.collections.Contains(collection))
                {
                    p.Remove(collection);
                    p.OnPropertyChanged();
                }

            if (!AssetDatabase.Contains(collection))
                Add(collection, profile);
            else
            {
                profile.m_collections.Add(collection);
                profile.Save();
            }

        }

        /// <summary>Adds the <see cref="SceneAsset"/> to asm. Returns existing <see cref="Scene"/> if already exist.</summary>
        /// <remarks>Returns <see langword="null"/> if scene has been added to <see cref="Ignore(string)"/>.</remarks>
        public static Scene Add(SceneAsset asset, bool ignoreBlacklist = false)
        {

            var path = AssetDatabase.GetAssetPath(asset);
            if (ignore.Contains(path) || (!ignoreBlacklist && BlacklistUtility.IsBlocked(path)))
                return null;

            var scene = Scene.Find(path);
            if (scene)
                return scene;

            var id = AssetDatabase.AssetPathToGUID(path);
            scene = Create<Scene>(Path.GetFileNameWithoutExtension(path), s => s.UpdateAsset(id, path));

            return scene;

        }

        /// <summary>Create and add an asset.</summary>
        public static T Create<T>(string name, Action<T> initializeBeforeSave = null) where T : ScriptableObject, IASMObject =>
            Create(name, profile: null, initializeBeforeSave);

        /// <summary>Create and add an asset.</summary>
        public static T Create<T>(string name, Profile profile = null, Action<T> initializeBeforeSave = null) where T : ScriptableObject, IASMObject
        {

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            if (!profile)
                profile = Profile.current;
            if (!profile && typeof(T) == typeof(SceneCollection))
                return null;

            var obj = ScriptableObject.CreateInstance<T>();
            if (obj is SceneCollection collection)
            {
                obj.name = profile.prefix + name;
                collection.m_title = name;
            }
            else
                obj.name = name;

            initializeBeforeSave?.Invoke(obj);
            Add(obj, profile);

            return obj;

        }

        #endregion
        #region Rename

        /// <summary>Renames the <see cref="Scene"/> or <see cref="SceneCollection"/>.</summary>
        public static void Rename<T>(T obj, string newName) where T : ScriptableObject, IASMObject
        {

            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentNullException(nameof(newName));

            allowAssetRefresh = false;

            if (obj is SceneCollectionTemplate template)
            {
                obj.name = newName;
                template.MarkAsDirty();
            }
            else if (obj is SceneCollection collection && collection.FindProfile() is Profile profile)
            {
                if (!profile.removedCollections.Contains(collection))
                    _ = RenameAsset(collection, profile.prefix + newName);
                collection.m_title = newName;
                EditorUtility.SetDirty(collection);
                AssetDatabase.SaveAssets();
            }
            else if (obj is Scene scene)
                _ = RenameAsset(scene, newName);

            bool RenameAsset(Object o, string name)
            {

                var error = AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(o), name);
                if (string.IsNullOrEmpty(error))
                {
                    EditorUtility.SetDirty(o);
                    if (obj is IASMObject p)
                        p.OnPropertyChanged();
                    return true;
                }
                else
                {
                    Debug.LogError(error);
                    return false;
                }

            }

            allowAssetRefresh = true;

        }

        #endregion

    }

}
#endif

#pragma warning disable CS0414

using Object = UnityEngine.Object;
using Scene = AdvancedSceneManager.Models.Scene;
using UnityEngine;
using UnityEngine.Serialization;
using AdvancedSceneManager.Models;
using System.Collections.Generic;
using System;
using Lazy.Utility;
using System.Linq;
using System.IO;
using AdvancedSceneManager.Utility;


#if UNITY_EDITOR
using AdvancedSceneManager.Editor.Utility;
using UnityEditor;
#endif

namespace AdvancedSceneManager.Core
{

    class AssetRef : ScriptableObject
    {

        [SerializeField] private string m_path;
        [SerializeField, FormerlySerializedAs("profiles")] private Profile[] m_profiles = Array.Empty<Profile>();
        [SerializeField, FormerlySerializedAs("scenes")] private Scene[] m_scenes = Array.Empty<Scene>();
        [SerializeField, FormerlySerializedAs("collections")] private SceneCollection[] m_collections = Array.Empty<SceneCollection>();

        [SerializeField] private ASMSettings m_settings;
        [SerializeField] private CollectionManager m_collectionManager;
        [SerializeField] private StandaloneManager m_standaloneManager;
        [SerializeField] private Utility.ASM m_sceneHelper;

        public IEnumerable<Profile> profiles => m_profiles;
        public IEnumerable<SceneCollection> collections => m_collections;
        public IEnumerable<Scene> scenes => m_scenes;

        public ASMSettings settings => m_settings;
        public CollectionManager collectionManager => m_collectionManager;
        public StandaloneManager standaloneManager => m_standaloneManager;
        public Utility.ASM sceneHelper => m_sceneHelper;

        static AssetRef m_instance;
        public static AssetRef instance => m_instance;

        public IEnumerable<Object> allAssets =>
            profiles.OfType<Object>().Concat(scenes).Concat(collections).Concat(new Object[] { settings, collectionManager, standaloneManager, sceneHelper, instance }).Where(o => o);

        public static bool isInitialized { get; private set; }

        void OnValidate() =>
            m_path = path;

        void OnEnable()
        {

            m_instance = this;

            Initialize(ref instance.m_settings);
            Initialize(ref instance.m_collectionManager);
            Initialize(ref instance.m_standaloneManager);
            Initialize(ref instance.m_sceneHelper);

#if !UNITY_EDITOR
            if (!profiles.Any())
                m_profiles = Resources.FindObjectsOfTypeAll<Profile>();

            if (!collections.Any())
                m_collections = Resources.FindObjectsOfTypeAll<SceneCollection>();

            if (!scenes.Any())
                m_scenes = Resources.FindObjectsOfTypeAll<Scene>();
#endif
#if UNITY_EDITOR
            PlayerSettings.SetPreloadedAssets(PlayerSettings.GetPreloadedAssets().Concat(allAssets).Distinct().ToArray());
#endif

            isInitialized = true;

        }

        public static void OnInitialized(Action action)
        {

#if UNITY_EDITOR
            UpgradeToLegacy();
            EditorApplication.delayCall += () => Initialize(ref m_instance);
#else
            Initialize(ref m_instance);
#endif
            CoroutineUtility.Run(action, when: () => isInitialized);
        }

        static void UpgradeToLegacy()
        {

            //Profile and scenes have had their script id regenerated,
            //due to duplicate ids (no clue why only them), so we need to fix the pointers

            UpgradeFiles("/Profiles/", "ed0f1fcb14f10114ca58b525ad780b5b", "973474d86aafab0469d005cf9a3fb394");
            UpgradeFiles("/Scenes/", "143365c1c80370a48a4c89e0b158bf44", "7043c523924cd2d45a74263baee0c32c");
            UpgradeFiles("/Defaults/Imported/Scene/", "143365c1c80370a48a4c89e0b158bf44", "7043c523924cd2d45a74263baee0c32c");

            void UpgradeFiles(string folder, string originalID, string newID)
            {
                var files = Directory.GetFiles("Assets", "*.asset", SearchOption.AllDirectories).Where(path => path.Replace("\\", "/").Contains(folder)).ToArray();
                foreach (var file in files)
                {

                    var originalLine = "m_Script: {fileID: 11500000, guid: " + originalID + ", type: 3}";
                    var newLine = "m_Script: {fileID: 11500000, guid: " + newID + ", type: 3}";
                    var text = File.ReadAllText(file);

                    if (text.Contains(originalLine))
                    {
                        text = text.Replace(originalLine, newLine);
                        File.WriteAllText(file, text);
                    }

                }
            }

        }

        #region Add / Remove

#if UNITY_EDITOR

        public void Add<T>(params T[] obj) where T : Object, IASMObject
        {
            if (obj is Profile[] profiles)
                Add(ref m_profiles, profiles);
            else if (obj is SceneCollection[] collections)
                Add(ref m_collections, collections);
            else if (obj is Scene[] scenes)
                Add(ref m_scenes, scenes);
        }

        public void Remove<T>(params T[] obj) where T : Object, IASMObject
        {
            if (obj is Profile[] profiles)
                Remove(ref m_profiles, profiles);
            else if (obj is SceneCollection[] collections)
                Remove(ref m_collections, collections);
            else if (obj is Scene[] scenes)
                Remove(ref m_scenes, scenes);
        }

        void Add<T>(ref T[] list, params T[] item) where T : Object =>
            Do<T>(ref list, (l) => l.Concat(item));

        void Remove<T>(ref T[] list, params T[] item) where T : Object =>
            Do(ref list, (l) => l.Except(item));

        void Do<T>(ref T[] list, Func<IEnumerable<T>, IEnumerable<T>> action) where T : Object
        {

            var savedList = list?.ToArray();

            IEnumerable<T> newList = (list ?? Array.Empty<T>());
            newList = action.Invoke(newList);
            newList = newList.Distinct().Where(o => o).OrderBy(o => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(o)));

            if (!newList.SequenceEqual(savedList))
            {
                list = newList.ToArray();
                EditorUtility.SetDirty(this);
            }

        }

        public void Cleanup()
        {
            Do(ref m_profiles, (l) => l);
            Do(ref m_collections, (l) => l);
            Do(ref m_scenes, (l) => l);
        }

        public void Clear()
        {
            m_profiles = null;
            m_collections = null;
            m_scenes = null;
            Cleanup();
        }

#endif

        #endregion
        #region Singletons

        static string defaultPath = "Assets/Settings/AdvancedSceneManager/Resources/AdvancedSceneManager";
        public static string path
        {
            get
            {

#if UNITY_EDITOR
                if (!instance || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(instance)))
                    return defaultPath;
#endif

                if (!instance)
                    return defaultPath;
                else
                {

#if UNITY_EDITOR
                    var path = Directory.GetParent(AssetDatabase.GetAssetPath(instance)).FullName;
                    path = "Assets" + path.Remove(0, Application.dataPath.Length).Replace('\\', '/');
                    return path;
#else
                    return instance.m_path.Replace('\\', '/');
#endif

                }

            }
        }

        #region Move

#if UNITY_EDITOR

        /// <summary>Move all ASM assets to a new folder.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static void Move(string path)
        {

            if (AssetDatabase.IsValidFolder(path))
                if (!EditorUtility.DisplayDialog("Moving assets...", "The specified folder is not empty, do you wish to clear it before moving?", ok: "Cancel", cancel: "Yes"))
                    _ = AssetDatabase.DeleteAsset(path);
                else
                    return;

            var originalPath = AssetRef.path;

            var key = new object();
            AssetDatabaseUtility.DisallowAutoRefresh(key);

            EditorFolderUtility.EnsureFolderExists(path);

            Move(originalPath + "/Scenes", path + "/Scenes");
            Move(originalPath + "/SceneCollections", path + "/SceneCollections");
            Move(originalPath + "/Collections", path + "/Collections");
            Move(originalPath + "/Profiles", path + "/Profiles");
            Move(originalPath + "/SceneData", path + "/SceneData");
            Move(originalPath + "/AdvancedSceneManager.unity", path + "/AdvancedSceneManager.unity");

            MoveSingleton(instance.settings);
            MoveSingleton(instance.collectionManager);
            MoveSingleton(instance.standaloneManager);
            MoveSingleton(instance.sceneHelper);
            MoveSingleton(instance);

            AssetDatabaseUtility.AllowAutoRefresh(key);

            void MoveSingleton<T>(T instance) where T : ScriptableObject =>
                Move(AssetDatabase.GetAssetPath(instance), path + "/" + GetName<T>() + ".asset");

            void Move(string from, string to)
            {
                if (AssetDatabase.IsValidFolder(from) || AssetDatabase.LoadAssetAtPath<Object>(from))
                {
                    var str = AssetDatabase.MoveAsset(from, to);
                    if (!string.IsNullOrEmpty(str))
                        throw new Exception(str);
                }
            }

        }

#endif

        #endregion
        #region Initialize

        static void Initialize<T>(ref T field) where T : ScriptableObject
        {

            //Debug.Log($"Initializing {GetName<T>()}.");
            var isPersistent = true;
#if UNITY_EDITOR
            isPersistent = EditorUtility.IsPersistent(field);
#endif

            if (field && isPersistent)
            {
                //Debug.Log($"{GetName<T>()} already initialized.");
                return;
            }

            if (LoadFromResources(out field) || LoadFromAssetDatabase(out field))
            {
                Save();
                //Debug.Log($"{GetName<T>()} loaded.");
            }
            else if (!Application.isBatchMode)
            {

#if UNITY_EDITOR

                if (AssetDatabase.LoadAssetAtPath<Object>(path + "/" + GetName<T>() + ".asset"))
                {
                    //Debug.Log($"Attempting to create {GetName<T>()}, but an asset already existed at path.");
                }
                else
                {
                    field = CreateInstance<T>();
                    EditorFolderUtility.EnsureFolderExists(path);
                    AssetDatabase.CreateAsset(field, $"{path}/{GetName<T>()}.asset");
                    Save();
                    //Debug.Log($"{GetName<T>()} created.");
                }

#endif

            }

        }

        static bool LoadFromResources<T>(out T obj) where T : ScriptableObject
        {
            var resources = Resources.LoadAll<T>("AdvancedSceneManager");
            obj = resources.FirstOrDefault();
            return obj;
        }

        static bool LoadFromAssetDatabase<T>(out T obj) where T : ScriptableObject
        {
#if UNITY_EDITOR
            obj = AssetDatabase.LoadAssetAtPath<T>($"{path}/{GetName<T>()}.asset");
            return obj;
#else
            obj = null;
            return false;
#endif
        }

        static void Save()
        {
#if UNITY_EDITOR
            if (m_instance && EditorUtility.IsPersistent(m_instance))
                m_instance.Save();
#endif
        }

        static string GetName<T>()
        {
            if (typeof(T) == typeof(Utility.ASM))
                return "SceneHelper";
            else
                return typeof(T).Name;
        }

        #endregion

        #endregion

    }

}

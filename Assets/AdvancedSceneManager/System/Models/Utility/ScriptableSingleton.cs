//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Reflection;
//using AdvancedSceneManager.Utility;
//using UnityEngine;
//using Lazy.Utility;

//#if UNITY_EDITOR
//using AdvancedSceneManager.Editor.Utility;
//using UnityEditor;
//using UnityEditor.Build;
//using UnityEditor.Build.Reporting;
//using UnityEditorInternal;
//#endif

//namespace AdvancedSceneManager.Models
//{

//#if UNITY_EDITOR

//    public class ScriptableSingletonBuildStep : IPreprocessBuildWithReport
//    {

//        public static bool isBuilding { get; private set; }

//        public int callbackOrder => 0;

//        public static readonly List<ScriptableObject> objToPersist = new();
//        public static void Add<T>(T obj) where T : ScriptableObject =>
//            objToPersist.Add(obj);

//        public void OnPreprocessBuild(BuildReport report)
//        {

//            isBuilding = true;

//            foreach (var obj in objToPersist.NonNull().ToArray())
//                Move(obj);

//        }

//        static ScriptableSingletonBuildStep() =>
//            BuildUtility.postBuild += _ => Cleanup();

//        const string Folder = "Assets/ASMBuild";

//        static void Move(ScriptableObject obj)
//        {

//            if (!obj)
//                return;

//            if (AssetDatabase.Contains(obj))
//                return;

//            if (Application.isBatchMode)
//                Debug.Log($"#UCB Preparing '{obj.name}' for build.");

//            var relativePath = obj.GetType().GetCustomAttribute<FilePathAttribute>()?.filepath;
//            if (string.IsNullOrEmpty(relativePath))
//                throw new InvalidOperationException("Could not prepare '" + obj.name + "' for build.");

//            var path = $"{Folder}/Resources/{relativePath}";

//            var s = Directory.GetParent(path).FullName.ConvertToUnixPath();

//            obj.hideFlags = HideFlags.None;
//            obj.Save();
//            AssetDatabaseUtility.CreateFolder(s);
//            AssetDatabase.CreateAsset(obj, path);

//            AddToPreloadedAssets(obj);
//            Log(obj, relativePath);
//            AssetDatabase.SaveAssets();

//        }

//        static void Log(ScriptableObject obj, string relativePath)
//        {

//            var o = Resources.Load(relativePath.Replace(".asset", ""), obj.GetType());
//            var isMoved = o && o.GetType() == obj.GetType();
//            var isAddedToPreloadedAssets = PlayerSettings.GetPreloadedAssets().Any(o => o && o.GetType() == obj.GetType());

//            if (isMoved && isAddedToPreloadedAssets)
//                Debug.Log($"'{obj.name}' successfully prepared for build.");
//            else
//                throw new InvalidOperationException($"Could not prepare '{obj.name}' for build. Unknown error.");

//        }

//        static void Cleanup()
//        {

//            AssetDatabase.DeleteAsset(Folder);
//            RemoveFromPreloadedAssets();
//            isBuilding = false;

//            ASMSettings.Reinitialize();
//            ASMUserSettings.Reinitialize();

//        }

//        static void AddToPreloadedAssets(ScriptableObject obj)
//        {
//            var assets = PlayerSettings.GetPreloadedAssets().Where(o => o && o.GetType() != obj.GetType());
//            PlayerSettings.SetPreloadedAssets(assets.Concat(new[] { obj }).ToArray());
//        }

//        static void RemoveFromPreloadedAssets()
//        {
//            var assets = PlayerSettings.GetPreloadedAssets().NonNull().Except(objToPersist).ToArray();
//            PlayerSettings.SetPreloadedAssets(assets.ToArray());
//            AssetDatabase.SaveAssets();
//        }
//    }

//#endif

//    [AttributeUsage(AttributeTargets.Class)]
//    public class FilePathAttribute : Attribute
//    {
//        public enum Location
//        {
//            ProjectSettings,
//            UserSettings
//        }

//        private string m_FilePath;
//        private string m_RelativePath;
//        internal Location m_Location;

//        public string filepath
//        {
//            get
//            {
//                if (m_FilePath == null && m_RelativePath != null)
//                {
//                    m_FilePath = CombineFilePath(m_RelativePath, m_Location);
//                    m_RelativePath = null;
//                }

//                return m_FilePath;
//            }
//        }

//        public FilePathAttribute(Location location, string relativePath)
//        {
//            if (string.IsNullOrEmpty(relativePath))
//            {
//                throw new ArgumentException("Invalid relative path (it is empty)");
//            }

//            m_RelativePath = relativePath;
//            m_Location = location;
//        }

//        private static string CombineFilePath(string relativePath, Location location)
//        {
//            if (relativePath[0] == '/')
//            {
//                relativePath = relativePath.Substring(1);
//            }

//            return $"{location}/{relativePath}";
//        }
//    }

//    public abstract class ScriptableSingleton<T> : ScriptableObject where T : ScriptableObject
//    {

//        public static T instance { get; private set; }
//        public static bool isInitialized => instance;

//        static readonly List<Action> callbacks = new();
//        public static void OnInitialized(Action action)
//        {
//            if (action == null)
//                return;

//            if (isInitialized)
//                CoroutineUtility.Run(action.Invoke, nextFrame: true);
//            else
//                callbacks.Add(action);

//            Initialize();
//        }

//        protected virtual void OnEnable()
//        {
//            CoroutineUtility.Run(() =>
//            {
//                foreach (var callback in callbacks)
//                    callback.Invoke();
//                callbacks.Clear();
//            }, nextFrame: true);
//        }

//        internal ScriptableSingleton()
//        {

//#if UNITY_EDITOR
//            if (ScriptableSingletonBuildStep.isBuilding)
//                return;
//#endif

//            if (instance != null)
//            {
//                //Debug.LogError("ScriptableSingleton already exists. Did you query the singleton in a constructor?");
//            }
//            else
//            {
//                instance = this as T;

//#if UNITY_EDITOR
//                if (GetLocation() == FilePathAttribute.Location.ProjectSettings)
//                    ScriptableSingletonBuildStep.Add(this);
//#endif

//            }
//        }

//        public static void Reinitialize()
//        {
//            hasInitialized = false;
//            instance = null;
//            Initialize();
//        }

//        static bool hasInitialized;
//        public static void Initialize()
//        {
//            if (hasInitialized)
//                return;
//            hasInitialized = true;

//            var filePath = GetFilePath();
//            if (!string.IsNullOrEmpty(filePath))
//            {
//#if UNITY_EDITOR
//                instance = InternalEditorUtility.LoadSerializedFileAndForget(filePath).FirstOrDefault() as T;
//#endif
//            }

//            if (instance == null)
//            {
//                if (!Application.isPlaying)
//                    _ = CreateInstance<T>();
//                else
//                    Debug.LogError("Could not load ScriptableSingleton.");
//            }
//        }

//        public virtual void Save()
//        {
//#if UNITY_EDITOR
//            if (Application.isPlaying)
//                return;

//            if (instance == null)
//            {
//                //Debug.LogError("Cannot save ScriptableSingleton: no instance!");
//                return;
//            }

//            string filePath = GetFilePath();
//            if (!string.IsNullOrEmpty(filePath))
//            {
//                string directoryName = Path.GetDirectoryName(filePath);
//                if (!Directory.Exists(directoryName))
//                    Directory.CreateDirectory(directoryName);

//                UnityEngine.Object[] obj = new T[1] { instance };
//                InternalEditorUtility.SaveToSerializedFileAndForget(obj, filePath, true);
//            }
//            else
//            {
//                Debug.LogWarning($"Saving has no effect. Your class '{GetType()}' is missing the FilePathAttribute. Use this attribute to specify where to save your ScriptableSingleton.\nOnly call Save() and use this attribute if you want your state to survive between sessions of Unity.");
//            }
//#endif
//        }

//        internal static FilePathAttribute.Location GetLocation() =>
//            typeof(T).GetCustomAttribute<FilePathAttribute>(true).m_Location;

//        public static string GetFilePath() =>
//            typeof(T).GetCustomAttribute<FilePathAttribute>(true).filepath;

//    }

//}

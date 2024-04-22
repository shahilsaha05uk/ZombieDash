using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
using AdvancedSceneManager.Editor.Utility;
#endif

namespace AdvancedSceneManager.Utility
{

    /// <summary>Contains utility methods for <see cref="ScriptableObject"/>.</summary>
    public static class ScriptableObjectUtility
    {

        /// <summary>Saves the <see cref="ScriptableObject"/>.</summary>
        /// <remarks>Safe to call from outside editor, but has no effect.</remarks>
        public static void Save(this ScriptableObject obj)
        {

#if UNITY_EDITOR

            if (!obj)
                return;

            EditorUtility.SetDirty(obj);

#if UNITY_2019
            AssetDatabase.SaveAssets();
#else
            AssetDatabase.SaveAssetIfDirty(obj);
#endif

#endif

        }

        #region Singleton

        static readonly Dictionary<Type, ScriptableObject> m_current = new Dictionary<Type, ScriptableObject>();

        /// <summary>Gets a singleton instance of the specified type.</summary>
        public static T GetSingleton<T>(string assetPath, string resourcesPath) where T : ScriptableObject
        {
            return
                m_current.TryGetValue(typeof(T), out var value) && value
                ? (T)value
                : FindAsset<T>(assetPath, resourcesPath);
        }

        static T LoadFromResources<T>(string resourcesPath) where T : ScriptableObject =>
            Resources.Load<T>(resourcesPath);

        static T FindAsset<T>(string assetPath, string resourcesPath) where T : ScriptableObject
        {

            if (LoadFromResources<T>(resourcesPath) is T value && value)
                return (T)m_current.Set(typeof(T), value);
            else
            {

#if UNITY_EDITOR

                var o = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (o)
                    return o;

                EditorFolderUtility.EnsureFolderExists(Path.GetDirectoryName(assetPath));

                var obj = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(obj, assetPath);
                AssetDatabase.Refresh();

                return (T)m_current.Set(typeof(T), AssetDatabase.LoadAssetAtPath<T>(assetPath));

#else
                        var so = ScriptableObject.CreateInstance<T>();
                        m_current.Set(typeof(T), so);
                        return so;
#endif

            }

        }

        #endregion

    }

}

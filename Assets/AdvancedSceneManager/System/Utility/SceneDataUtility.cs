using System;
using System.Collections.Generic;
using AdvancedSceneManager.Models;
using UnityEngine;
using scene = UnityEngine.SceneManagement.Scene;

#if UNITY_EDITOR
#endif

namespace AdvancedSceneManager.Utility
{

    /// <summary>A utility for storing scene related data. Data can only be saved to disk in editor.</summary>
    public static class SceneDataUtility
    {

        static ASMSettings.SceneData data
        {
            get
            {
                if (SceneManager.settings.project.sceneData == null)
                    SceneManager.settings.project.sceneData = new ASMSettings.SceneData();
                return SceneManager.settings.project.sceneData;
            }
        }

        #region Get

        /// <summary>Enumerates data using the specified key on all scenes that uses it.</summary>
        public static IEnumerable<(Scene scene, T data)> Enumerate<T>(string key)
        {
            foreach (var scene in data)
                if (scene.Value.Get(key, out var json) && SceneManager.assets.scenes.TryFind(scene.Key, out var targetScene) && TryDeserialize<T>(json, out var t))
                    yield return (targetScene, t);
        }

        /// <summary>Gets the value with the specified key, for the specified scene.</summary>
        public static T Get<T>(Scene scene, string key, T defaultValue = default) =>
            Get(scene ? scene.id : null, key, defaultValue);

        /// <summary>Gets the value with the specified key, for the specified scene.</summary>
        public static T Get<T>(string scene, string key, T defaultValue = default) =>
            Get<T>(scene, key, out var value)
            ? value
            : defaultValue;

        /// <summary>Gets the raw value with the specified key, for the specified scene.</summary>
        public static string GetRaw(Scene scene, string key)
        {
            if (data.ContainsKey(scene ? scene.id : null))
                return data[scene ? scene.id : null].Get(key);
            else
                return null;
        }

        static bool Get<T>(string path, string key, out T value)
        {

            value = default;

            if (path is null || !data.ContainsKey(path) || !data[path].ContainsKey(key))
                return false;

            var json = data[path][key];
            if (string.IsNullOrWhiteSpace(json))
                return false;

            return
                Type.GetTypeCode(typeof(T)) != TypeCode.Object
                ? TryConvert(json, out value)
                : TryDeserialize(json, out value);

        }

        #endregion
        #region Set

        /// <summary>Sets the value with the specified key, for the specified scene.</summary>
        /// <remarks>Changes will only be persisted in editor.</remarks>
        public static void Set<T>(Scene scene, string key, T value) =>
            Set(scene ? scene.id : null, key, value);

        /// <summary>Sets the value with the specified key, for the specified scene.</summary>
        /// <remarks>Changes will only be persisted in editor.</remarks>
        public static void Set<T>(string scene, string key, T value)
        {
            if (Convert.GetTypeCode(value) != TypeCode.Object)
                SetRaw(scene, key, Convert.ToString(value));
            else
                SetRaw(scene, key, JsonUtility.ToJson(value));
        }

        public static void SetRaw(Scene scene, string key, string value) =>
            SetRaw(scene ? scene.id : null, key, value);

        /// <summary>Sets the value with the specified key, for the specified scene. This is the direct version, all values are stores as string, which means <see cref="Get{T}(string, string, T)"/> must convert value beforehand, this method doesn't.</summary>
        /// <remarks>Changes will only be persisted in editor.</remarks>
        public static void SetRaw(string scene, string key, string value)
        {

            CheckValidArgs(scene, key);

            if (!data.ContainsKey(scene))
                data.Set(scene, new ASMSettings.CustomData());
            data[scene].Set(key, value);

        }

        /// <summary>Unsets the value with the specified key, for the specified scene.</summary>
        /// <remarks>Changes will only be persisted in editor.</remarks>
        public static void Unset(Scene scene, string key)
        {
            CheckValidArgs(scene ? scene.id : null, key);
            if (scene && data.ContainsKey(scene.id))
                data[scene.id].Clear(key);
        }

        static void CheckValidArgs(string scene, string key)
        {

            if (string.IsNullOrEmpty(scene)) throw new ArgumentNullException(nameof(scene));
            if (string.IsNullOrWhiteSpace(scene)) throw new ArgumentException(nameof(scene));

            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException(nameof(key));

        }

        #endregion
        #region Json

        static bool TryConvert<T>(object obj, out T value)
        {
            try
            {
                value = (T)Convert.ChangeType(obj, typeof(T));
                return true;
            }
            catch (Exception)
            { }
            value = default;
            return false;
        }

        static bool TryDeserialize<T>(string json, out T value)
        {
            try
            {
                value = JsonUtility.FromJson<T>(json);
                return true;
            }
            catch (Exception)
            { }
            value = default;
            return false;
        }

        #endregion

    }

}

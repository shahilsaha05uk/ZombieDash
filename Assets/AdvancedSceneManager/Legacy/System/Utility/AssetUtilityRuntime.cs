using System;
using System.Collections.Generic;
using System.Linq;
using AdvancedSceneManager.Core;
using AdvancedSceneManager.Models;
using UnityEditor;
using static AdvancedSceneManager.Utility.SceneEqualityUtility;
using scene = UnityEngine.SceneManagement.Scene;
using unityScene = UnityEngine.SceneManagement.Scene;

namespace AdvancedSceneManager.Utility
{

    /// <summary>Provides utility functions for working with assets.</summary>
    public static class AssetUtilityRuntime
    {

        #region Assets proxy

        public sealed class AssetsProxy
        {

            /// <summary>Enumerates all scenes in the project, that is managed by ASM.</summary>
            public IEnumerable<Scene> allScenes => AssetRef.instance.scenes;

            /// <summary>Enumerates all collections in the project.</summary>
            public IEnumerable<SceneCollection> allCollections => AssetRef.instance.collections;

            /// <summary>Enumerates all profiles in the project.</summary>
            public IEnumerable<Profile> profiles => AssetRef.instance.profiles;

            /// <summary>Enumerates all scenes managed by the current profile.</summary>
            public IEnumerable<Scene> scenes => Profile.current ? Profile.current.scenes : Array.Empty<Scene>();

            /// <summary>Enumerates all collections in the current profile.</summary>
            public IEnumerable<SceneCollection> collections => Profile.current ? Profile.current.collections.ToArray() : Array.Empty<SceneCollection>();

            /// <summary>Enumerates <typeparamref name="T"/>.</summary>
            /// <param name="inCurrentProfile">Specifies whatever to filter results to current profile or not. No effect when T is <see cref="Profile"/>.</param>
            public IEnumerable<T> Enumerate<T>(bool inCurrentProfile = false) where T : IASMObject =>
                Enumerate<T>(obj: default, inCurrentProfile);

            IEnumerable<T> Enumerate<T>(T obj, bool inCurrentProfile = false) where T : IASMObject
            {
                if (obj is Scene) return inCurrentProfile ? scenes.Cast<T>() : allScenes.Cast<T>();
                else if (obj is SceneCollection) return inCurrentProfile ? collections.Cast<T>() : allCollections.Cast<T>();
                else if (obj is Profile) return profiles.Cast<T>();
                return Array.Empty<T>();
            }

        }

        #endregion
        #region Find

        #region Auto list

        /// <summary>Finds the <typeparamref name="T"/> with the specified name.</summary>
        /// <param name="name">The name to search for. Path and asset id is supported for scenes.</param>
        public static T Find<T>(string name) where T : IASMObject =>
            Find(SceneManager.assets.Enumerate<T>(), name);

        /// <summary>Finds the <typeparamref name="T"/> with the specified name.</summary>
        /// <param name="name">The name to search for. Path and asset id is supported for scenes.</param>
        public static bool TryFind<T>(string name, out T result) where T : IASMObject =>
            TryFind(SceneManager.assets.Enumerate<T>(), name, out result);

        #endregion
        #region T[]

        /// <inheritdoc cref="Find{T}(string)"/>
        public static T Find<T>(this T[] list, string name) where T : IASMObject =>
            Find((IEnumerable<T>)list, name);

        /// <inheritdoc cref="Find{T}(string)"/>
        public static bool TryFind<T>(this T[] list, string name, out T result) where T : IASMObject =>
            TryFind((IEnumerable<T>)list, name, out result);

        /// <summary>Finds the open scene with the specfied unity scene.</summary>
        public static bool TryFind(this OpenSceneInfo[] list, scene scene, out OpenSceneInfo result) =>
            TryFind((IEnumerable<OpenSceneInfo>)list, scene, out result);

        /// <summary>Finds the open scene with the specfied Scene.</summary>
        public static bool TryFind(this OpenSceneInfo[] list, Scene scene, out OpenSceneInfo result) =>
            TryFind((IEnumerable<OpenSceneInfo>)list, scene, out result);

        #endregion
        #region IEnumerable<T>

        /// <inheritdoc cref="Find{T}(string)"/>
        public static T Find<T>(this IEnumerable<T> list, string name) where T : IASMObject =>
            (T)(object)list.OfType<IASMObject>().FirstOrDefault(o => o.Match(name));

        /// <inheritdoc cref="Find{T}(string)"/>
        public static bool TryFind<T>(this IEnumerable<T> list, string name, out T result) where T : IASMObject =>
            (result = (T)(object)list.OfType<IASMObject>().FirstOrDefault(o => o.Match(name))) != null;

        /// <inheritdoc cref="TryFind(OpenSceneInfo[], scene, out OpenSceneInfo)"/>
        public static OpenSceneInfo Find(this IEnumerable<OpenSceneInfo> list, scene scene) =>
            list.OfType<OpenSceneInfo>().FirstOrDefault(s => s.unityScene == scene);

        /// <inheritdoc cref="TryFind(OpenSceneInfo[], Scene, out OpenSceneInfo)"/>
        public static OpenSceneInfo Find(this IEnumerable<OpenSceneInfo> list, Scene scene) =>
            list.OfType<OpenSceneInfo>().FirstOrDefault(s => s.scene == scene);

        /// <inheritdoc cref="TryFind(OpenSceneInfo[], scene, out OpenSceneInfo)"/>
        public static bool TryFind(this IEnumerable<OpenSceneInfo> list, scene scene, out OpenSceneInfo result) =>
            (result = list.OfType<OpenSceneInfo>().FirstOrDefault(s => s.unityScene == scene)) != null;

        /// <inheritdoc cref="TryFind(OpenSceneInfo[], Scene, out OpenSceneInfo)"/>
        public static bool TryFind(this IEnumerable<OpenSceneInfo> list, Scene scene, out OpenSceneInfo result) =>
            (result = list.OfType<OpenSceneInfo>().FirstOrDefault(s => s.scene == scene)) != null;

        #endregion

        #endregion

    }

}

#region Equality

namespace AdvancedSceneManager.Models
{

    public partial class Scene : IEquatable<Scene>, IEquatable<OpenSceneInfo>, IEquatable<unityScene?>
#if UNITY_EDITOR
    , IEquatable<SceneAsset>
#endif
    {

        //[x] Scene == Scene
        //[x] Scene == OpenSceneInfo
        //[x] Scene == UnityScene?

        //[x] UnityScene? == Scene

        //[x] Scene == SceneAsset
        //[x] SceneAsset == Scene

        public static bool operator ==(Scene scene, object obj) => IsEqual(scene, obj);
        public static bool operator !=(Scene scene, object obj) => !IsEqual(scene, obj);

        public override int GetHashCode() => path?.GetHashCode() ?? 0;

        public override bool Equals(object obj) => IsEqual(obj, this);
        public bool Equals(Scene scene) => IsEqual(scene, this);
        public bool Equals(OpenSceneInfo scene) => IsEqual(scene, this);
        public bool Equals(unityScene? scene) => IsEqual(scene, this);

#if UNITY_EDITOR
        public bool Equals(SceneAsset scene) => IsEqual(scene, this);
        public static bool operator ==(Scene scene, SceneAsset sceneAsset) => IsEqual(scene, sceneAsset);
        public static bool operator !=(Scene scene, SceneAsset sceneAsset) => !IsEqual(scene, sceneAsset);
        public static bool operator ==(SceneAsset sceneAsset, Scene scene) => IsEqual(scene, sceneAsset);
        public static bool operator !=(SceneAsset sceneAsset, Scene scene) => !IsEqual(scene, sceneAsset);
#endif

    }

}

namespace AdvancedSceneManager.Core
{

    public partial class OpenSceneInfo : IEquatable<OpenSceneInfo>, IEquatable<Scene>, IEquatable<unityScene?>
#if UNITY_EDITOR
    , IEquatable<SceneAsset>
#endif
    {

        //[x] OpenSceneInfo == OpenSceneInfo
        //[x] OpenSceneInfo == Scene
        //[x] OpenSceneInfo == UnityScene?

        //[x] UnityScene? == OpenSceneInfo

        //[x] SceneAsset == OpenSceneInfo
        //[x] OpenSceneInfo == SceneAsset

        public static bool operator ==(OpenSceneInfo scene, object obj) => IsEqual(scene, obj);
        public static bool operator !=(OpenSceneInfo scene, object obj) => !IsEqual(scene, obj);

        public override int GetHashCode() => path?.GetHashCode() ?? 0;

        public override bool Equals(object obj) => IsEqual(obj, this);
        public bool Equals(Scene scene) => IsEqual(scene, this);
        public bool Equals(OpenSceneInfo scene) => IsEqual(scene, this);
        public bool Equals(unityScene? scene) => IsEqual(scene, this);

#if UNITY_EDITOR
        public bool Equals(SceneAsset scene) => IsEqual(scene, this);
#endif

    }

}

namespace AdvancedSceneManager.Utility
{

    static class SceneEqualityUtility
    {

        public static bool IsEqual(object left, object right) =>
            left is null && right is null ||
            (GetPath(left, out var l) &&
            GetPath(right, out var r) &&
            l == r);

        static bool GetPath(object obj, out string path)
        {

            if (obj is null)
                path = null;
            else if (obj is Scene scene && scene)
                path = scene.path;
            else if (obj is OpenSceneInfo osi && osi.scene)
                path = osi.scene.path;
            else if (obj is OpenSceneInfo osi2 && osi2.unityScene.HasValue)
                path = osi2.unityScene.Value.path;
            else if (obj is unityScene unityScene)
                path = unityScene.path;

#if UNITY_EDITOR
            else if (obj is SceneAsset sceneAsset && sceneAsset)
                path = AssetDatabase.GetAssetPath(sceneAsset);
#endif

            else if (obj is UnityEngine.Object o && !o)
                path = null;

            else if (obj is unityScene uScene)
                path = uScene.path;

            else
                path = null;

            return !string.IsNullOrEmpty(path);

        }

        static bool TryConvert<T>(object input, out T? output) where T : struct
        {
            if (input is T result)
            {
                output = result;
                return true;
            }
            output = default;
            return input == null;
        }

    }

}

#endregion
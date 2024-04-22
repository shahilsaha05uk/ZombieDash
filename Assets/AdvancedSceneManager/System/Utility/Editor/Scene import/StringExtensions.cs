#if UNITY_EDITOR

using System.Linq;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Models.Internal;
using AdvancedSceneManager.Utility;
using UnityEditor;

namespace AdvancedSceneManager.Editor.Utility
{

    partial class SceneImportUtility
    {

        public static class StringExtensions
        {

            /// <summary>Gets whatever the paths points to a scene that has been imported.</summary>
            public static bool IsImported(string path) =>
                 IsScene(path) && GetImportedScene(path, out _);

            /// <summary>Gets whatever this scene is blacklisted.</summary>
            public static bool IsBlacklisted(string path) =>
                Blacklist.IsBlacklisted(path) || IsDefaultScene(path) || IsLegacy(path);

            /// <summary>Gets whatever this scene is a unity test runner scene.</summary>
            public static bool IsTestScene(string path) =>
                path.StartsWith("Assets/InitTestScene");

            /// <summary>Gets whatever this is a package scene.</summary>
            public static bool IsPackageScene(string path) =>
                path.StartsWith("Packages/");

            /// <summary>Gets if this scene is the default scene.</summary>
            public static bool IsDefaultScene(string path) =>
                path.EndsWith($"/{FallbackSceneUtility.Name}.unity") || path.EndsWith("/AdvancedSceneManager.unity");

            /// <summary>Gets whatever the path points to a SceneAsset.</summary>
            public static bool IsScene(string path) =>
                path.EndsWith(".unity");

            /// <summary>Gets whatever this <see cref="SceneAsset"/> has an associated <see cref="Scene"/>.</summary>
            public static bool HasScene(string path) =>
                Assets.scenes.Any(s => s.path == path);

            /// <summary>Gets whatever this is a scene, that is available for import.</summary>
            public static bool IsValidSceneToImport(string path) =>
                IsScene(path) && !IsImported(path) && !IsBlacklisted(path) && !IsTestScene(path) && !IsPackageScene(path);

            /// <summary>Gets whatever this is a dynamic scene (its in a path managed by a dynamic collection).</summary>
            public static bool IsDynamicScene(string path) =>
                SceneManager.profile && SceneManager.profile.dynamicCollections.Any(c => path.Contains(c.path));

            /// <summary>Gets whatever this scene is a default ASM legacy scene.</summary>
            public static bool IsLegacy(string path) =>
                path.Contains("Assets/AdvancedSceneManager/1.9/System/");

        }

    }

}
#endif

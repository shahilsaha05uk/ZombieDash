using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;


#if UNITY_EDITOR
using AdvancedSceneManager.Editor.Utility;
#endif

namespace AdvancedSceneManager.Setup
{

    /// <summary>Gets version info about ASM.</summary>
    public static class ASMInfo
    {

        /// <summary>Gets the current version info.</summary>
        public static (string version, string patchNotes) GetVersionInfo()
        {
            var str = Resources.Load<TextAsset>("AdvancedSceneManager/version").text;
            var i = str.IndexOf("\n");
            var version = str.Remove(i);
            var patchNotes = str.Substring(i + 1);
            return (version, patchNotes);
        }

#if UNITY_EDITOR

        /// <summary>Gets the folder that ASM is contained within.</summary>
        public static string GetFolder()
        {
            var assemblyDefinition = AssetDatabase.FindAssets("t:asmdef").Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(path => path.EndsWith("/AdvancedSceneManager.asmdef"));
            return assemblyDefinition.Remove(assemblyDefinition.LastIndexOf("/"));
        }

#pragma warning disable CS0162 // Unreachable code detected

        static IEnumerable<string> GetLegacyAssets() =>
            Directory.GetFiles(Application.dataPath, "AssetRef.asset", SearchOption.AllDirectories).
            Select(f => f.ConvertToUnixPath()).
            Where(f => f.EndsWith("/Resources/AdvancedSceneManager/AssetRef.asset"));

        /// <summary>Gets whatever legacy is setup, this means project previously used ASM 1.9.</summary>
        public static bool IsLegacySetup()
        {
#if ASM_DEV
            return false;
#else
            return GetLegacyAssets().Any();
#endif
        }

        internal static void CleanupLegacyAssets()
        {

#if ASM_DEV
            return;
#endif

            foreach (var path in GetLegacyAssets().ToArray())
            {
                Directory.GetParent(path).Delete(true);
                AssetDatabase.Refresh();
            }

        }

#pragma warning restore CS0162 // Unreachable code detected

#endif

    }

}

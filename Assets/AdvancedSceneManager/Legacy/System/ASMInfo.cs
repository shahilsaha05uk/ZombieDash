using System.Linq;
using UnityEngine;

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
            var assemblyDefinition = UnityEditor.AssetDatabase.FindAssets("t:asmdef").
                Select(UnityEditor.AssetDatabase.GUIDToAssetPath).
                FirstOrDefault(path => path.EndsWith("/AdvancedSceneManager.asmdef"));
            return assemblyDefinition.Remove(assemblyDefinition.LastIndexOf("/"));
        }
#endif

    }

}


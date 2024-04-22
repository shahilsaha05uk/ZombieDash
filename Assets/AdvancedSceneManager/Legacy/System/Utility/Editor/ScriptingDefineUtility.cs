#if UNITY_EDITOR
using UnityEditor;

namespace AdvancedSceneManager.Editor.Utility
{

    /// <summary>Provides utility methods for managing the scripting defines / #pragmas for the project.</summary>
    public static class ScriptingDefineUtility
    {

        /// <summary>Gets the current build target.</summary>
        public static BuildTargetGroup BuildTarget =>
            EditorUserBuildSettings.selectedBuildTargetGroup;

        /// <summary>Gets the scripting defines in the project.</summary>
        public static string Enumerate() =>
            PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTarget);

        /// <summary>Gets if the scripting define is set.</summary>
        public static bool IsSet(string name) =>
            IsSet(name, out _);

        static bool IsSet(string name, out string actualString)
        {

            var defines = Enumerate();

            //We need to prevent finding substrings,
            //so lets check for 'name;' or ';name',
            //and then only check directly for name if no ';' exists (this means either zero or one defines defined)
            return defines.Contains(actualString = name + ";") ||
                defines.Contains(actualString = ";" + name) ||
                (!defines.Contains(";" + name) && defines.Contains(actualString = name));

        }

        /// <summary>Unsets the scripting define.</summary>
        public static void Unset(string name) =>
            Set(name, false);

        /// <summary>Sets the scripting define.</summary>
        /// <param name="isSet">Determines if the scripting define should be set or not.</param>
        public static void Set(string name, bool isSet = true)
        {

            var defines = Enumerate();
            var originalDefines = defines;

            if (isSet && !IsSet(name, out _))
                defines += ";" + name;
            else if (!isSet && IsSet(name, out var actualString))
                defines = defines.Replace(actualString, "");

            if (defines != originalDefines)
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTarget, defines);

        }

    }

}
#endif

#if UNITY_EDITOR

using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace AdvancedSceneManager.Editor.Utility
{

    /// <summary>Contains proxy functions for internal <see cref="EditorGUIUtility"/> functions that should have a public counterpart.</summary>
    public static class EditorGUIUtilityExt
    {

        public static void PingOrOpenAsset(Object targetObject, int clickCount)
        {
            if (clickCount == 1)
                EditorGUIUtility.PingObject(targetObject);
            else if (clickCount == 2)
            {
                _ = AssetDatabase.OpenAsset(targetObject);
                Selection.activeObject = targetObject;
            }

        }

        public static Color GetDefaultBackgroundColor() => (Color)Invoke();

        static object Invoke([CallerMemberName] string name = "", params object[] parameters) =>
            typeof(EditorGUIUtility).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(null, parameters);

    }

}
#endif

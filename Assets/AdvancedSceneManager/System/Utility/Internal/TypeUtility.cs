using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace AdvancedSceneManager.Utility
{

    static class TypeUtility
    {

        public static IEnumerable<FieldInfo> _GetFields(this Type type)
        {

            foreach (var field in type.GetFields(BindingFlags.GetField | BindingFlags.SetField | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                yield return field;

            if (type.BaseType != null)
                foreach (var field in _GetFields(type.BaseType))
                    yield return field;

        }

        public static FieldInfo FindField(this Type type, string name)
        {
            var e = _GetFields(type).GetEnumerator();
            while (e.MoveNext())
                if (e.Current.Name == name)
                    return e.Current;
            return null;
        }

#if UNITY_EDITOR
        /// <summary>Finds all assets of this type in the project, and return their paths.</summary>
        /// <remarks>Only available in the editor.</remarks>
        public static IEnumerable<string> FindAssetPaths(this Type type) =>
            AssetDatabase.FindAssets("t:" + type.FullName).Select(AssetDatabase.GUIDToAssetPath);
#endif

    }

}
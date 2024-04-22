#if ASM_PLUGIN_CROSS_SCENE_REFERENCES

using System;
using System.Collections.Generic;
using System.Reflection;

namespace AdvancedSceneManager.Plugin.Cross_Scene_References
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

    }

}
#endif
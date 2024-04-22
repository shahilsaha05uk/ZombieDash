#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;

namespace AdvancedSceneManager.Editor.Utility
{

    /// <summary>Provides methods to make using <see cref="AssetDatabase.AllowAutoRefresh"/> easier.</summary>
    public static class AssetDatabaseUtility
    {

        static readonly List<object> keys = new List<object>();
        public static void DisallowAutoRefresh(object key)
        {
            if (!keys.Contains(key))
            {
                keys.Add(key);
                if (keys.Count == 1)
                    AssetDatabase.DisallowAutoRefresh();
            }
        }

        public static void AllowAutoRefresh(object key)
        {
            if (keys.Remove(key) && keys.Count == 0)
                AssetDatabase.AllowAutoRefresh();
        }

    }

}
#endif

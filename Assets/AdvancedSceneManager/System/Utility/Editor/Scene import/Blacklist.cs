#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;

namespace AdvancedSceneManager.Editor.Utility
{

    partial class SceneImportUtility
    {

        /// <summary>Manages the blacklist.</summary>
        public static class Blacklist
        {

            /// <summary>Gets all paths that has been blacklisted.</summary>
            public static IEnumerable<string> blacklistedPaths => list;
            static List<string> list => SceneManager.settings.project.m_blacklist;

            static void Save() =>
                SceneManager.settings.project.Save();

            /// <summary>Gets if the path is blacklisted, this means that it won't show up in scene import.</summary>
            public static bool IsBlacklisted(string path)
            {

                if (!SceneManager.isInitialized)
                    return false;

                if (string.IsNullOrEmpty(path))
                    return false;

                Normalize(ref path);
                return list.Where(IsValid).Any(path.Contains);

            }

            static bool IsValid(string path) =>
                path?.StartsWith("assets/") ?? false;

            /// <summary>Gets if the blacklist contains the path.</summary>
            /// <remarks>This is works the same as regular <see cref="List{T}.Contains(T)"/>, not to be confused with <see cref="IsBlacklisted(string)"/>.</remarks>
            public static bool Contains(string path) =>
                list.Contains(Normalize(path));

            /// <summary>Gets the blacklisted path at the specified index.</summary>
            public static bool Get(int index, out string path)
            {
                path = null;
                if (list.Count > index)
                {
                    path = list[index];
                    return true;
                }
                else
                    return false;
            }

            /// <summary>Adds <paramref name="path"/> to blacklist.</summary>
            public static void Add(string path)
            {
                Normalize(ref path);
                if (!list.Contains(path))
                {
                    list.Add(path);
                    Save();
                    Notify();
                }
            }

            /// <summary>Changes the path at the specified index.</summary>
            public static void Change(int i, string newPath)
            {
                Normalize(ref newPath);
                if (list.Count > i)
                {
                    list[i] = newPath;
                    Save();
                    Notify();
                }
            }

            /// <summary>Removes <paramref name="path"/> to blacklist.</summary>
            /// <remarks>Note that this works the same as <see cref="List{T}.Remove(T)"/>.</remarks>
            public static void Remove(string path)
            {
                Normalize(ref path);
                if (list.Remove(path))
                {
                    Save();
                    Notify();
                }
            }

            /// <summary>Removes the path at the specified <paramref name="index"/> in the blacklist.</summary>
            public static void Remove(int index)
            {
                list.RemoveAt(index);
                Save();
                Notify();
            }

            /// <summary>Normalizes the path.</summary>
            public static void Normalize(ref string path) =>
                path = Normalize(path);

            /// <summary>Normalizes the path.</summary>
            public static string Normalize(string path) =>
                path.ToLower().Replace("\\", "/").Trim(' ', '/', '\\');

        }

    }

}
#endif

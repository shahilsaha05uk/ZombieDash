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
            public static IEnumerable<string> blacklistedPaths => blacklist;
            static List<string> blacklist => SceneManager.settings.project.m_blacklist;

            /// <summary>Gets all paths that has been whitelisted.</summary>
            public static IEnumerable<string> whitelistedPaths => whitelist;
            static List<string> whitelist => SceneManager.settings.project.m_whitelist;

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
                return blacklist.Where(IsValid).Any(path.Contains) || !IsWhitelisted(path);

            }

            /// <summary>Gets if the path is whitelisted, this means that it will show up in scene import.</summary>
            public static bool IsWhitelisted(string path)
            {

                if (!SceneManager.isInitialized)
                    return false;

                if (string.IsNullOrEmpty(path))
                    return false;

                if (whitelist.Count == 0)
                    return true;

                Normalize(ref path);
                return whitelist.Where(IsValid).Any(path.Contains);

            }

            static bool IsValid(string path) =>
                path?.StartsWith("assets/") ?? false;

            /// <summary>Gets if the blacklist contains the path.</summary>
            /// <remarks>This is works the same as regular <see cref="List{T}.Contains(T)"/>, not to be confused with <see cref="IsBlacklisted(string)"/>.</remarks>
            public static bool Contains(string path) =>
                blacklist.Contains(Normalize(path));

            /// <summary>Gets the blacklisted path at the specified index.</summary>
            public static bool Get(int index, out string path)
            {
                path = null;
                if (blacklist.Count > index)
                {
                    path = blacklist[index];
                    return true;
                }
                else
                    return false;
            }

            /// <summary>Adds <paramref name="path"/> to blacklist.</summary>
            public static void Add(string path)
            {
                Normalize(ref path);
                if (!blacklist.Contains(path))
                {
                    blacklist.Add(path);
                    Save();
                    Notify();
                }
            }

            /// <summary>Adds <paramref name="path"/> to blacklist.</summary>
            public static void AddToWhitelist(string path)
            {
                Normalize(ref path);
                if (!blacklist.Contains(path))
                {
                    whitelist.Add(path);
                    Save();
                    Notify();
                }
            }

            /// <summary>Changes the path at the specified index.</summary>
            public static void Change(int i, string newPath)
            {
                Normalize(ref newPath);
                if (blacklist.Count > i)
                {
                    blacklist[i] = newPath;
                    Save();
                    Notify();
                }
            }

            /// <summary>Removes <paramref name="path"/> to blacklist.</summary>
            /// <remarks>Note that this works the same as <see cref="List{T}.Remove(T)"/>.</remarks>
            public static void Remove(string path)
            {
                Normalize(ref path);
                if (blacklist.Remove(path))
                {
                    Save();
                    Notify();
                }
            }

            /// <summary>Removes <paramref name="path"/> to blacklist.</summary>
            /// <remarks>Note that this works the same as <see cref="List{T}.Remove(T)"/>.</remarks>
            public static void RemoveFromWhitelist(string path)
            {
                Normalize(ref path);
                if (whitelist.Remove(path))
                {
                    Save();
                    Notify();
                }
            }

            /// <summary>Removes the path at the specified <paramref name="index"/> in the blacklist.</summary>
            public static void Remove(int index)
            {
                blacklist.RemoveAt(index);
                Save();
                Notify();
            }

            /// <summary>Normalizes the path.</summary>
            public static void Normalize(ref string path) =>
                path = Normalize(path);

            /// <summary>Normalizes the path.</summary>
            public static string Normalize(string path) =>
                path.ToLower().Replace("\\", "/").Trim(' ');

        }

    }

}
#endif

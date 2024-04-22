#if ASM_PLUGIN_LOCKING

using System;
using System.Linq;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Utility;
using UnityEditor;
using Object = UnityEngine.Object;

namespace AdvancedSceneManager.Plugin.Locking
{

    /// <summary>
    /// <para>A utility for locking objects. This utility does not prevent editing by itself, beyond default ASM locking (scenes and collections), support must be added for this utility.</para>
    /// <para>Scenes are supported by: lock button on scenes in hierarchy and preventing scene save.</para>
    /// <para>Collections are supported by: lock button in <see cref="Editor.SceneManagerWindow"/> on collection headers and disabling ui elements in <see cref="Editor.SceneManagerWindow"/>.</para>
    /// </summary>
    public static class LockUtility
    {

        const string Key = "Locking";

        [InitializeOnLoadMethod]
        static void OnLoad()
        {

            foreach (var scene in SceneManager.assets.scenes.Where(s => s).ToArray())
                OnLockChanged(scene.path, SceneDataUtility.Get<LockInfo>(scene, Key)?.isEnabled ?? false);

            SceneLock.OnLoad();
            CollectionLock.OnLoad();
            UI.OnLoad();

        }

        //Called from Lock(), Unlock() methods
        internal static void OnLockChanged(string path, bool locked)
        {

            //Make sure SceneManagerWindow knows what collections are locked, so that all lockable ui elements can be disabled, if collection locked
            VisualElementExtensions.SetLocked(path, locked);

        }

        static LockInfo GetLock(string path)
        {

            if (string.IsNullOrEmpty(path))
                return LockInfo.Empty;

            if (SceneDataUtility.Get<LockInfo>(path, Key) is LockInfo locked)
                return locked;

            locked = new LockInfo();
            SceneDataUtility.Set(path, Key, locked);
            return locked;

        }

        #region String methods

        /// <summary>Gets if an object is locked.</summary>
        public static bool IsLocked(string path) =>
            IsLocked(path, out var _, out var _);

        /// <summary>Gets if an object is locked.</summary>
        public static bool IsLocked(string path, out string by, out string message)
        {
            var l = GetLock(path);
            by = l.by;
            message = l.message;
            return l.isEnabled;
        }

        /// <summary>Gets the tooltip string that would be displayed on buttons for example.</summary>
        public static string GetTooltipString(string path) =>
            GetLock(path).AsTooltip;

        /// <summary>Prompts to lock an object.</summary>
        public static bool PromptLock(string path)
        {

            var ((name, message), successful) = PromptNameAndMessage.Prompt();

            if (successful)
                Lock(path, name, message);

            return successful;

        }

        /// <summary>Locks an object.</summary>
        public static void Lock(string path, string name = null, string message = null)
        {
            SceneDataUtility.Set(path, Key, GetLock(path).Lock(name, message));
            OnLockChanged(path, locked: true);
        }

        /// <summary>Prompts to unlock an object.</summary>
        public static bool PromptUnlock(string path, string objName = "object")
        {

            if (!EditorUtility.DisplayDialog(
                title: "Unlocking...",
                message:
                    GetLock(path).AsTooltip + Environment.NewLine +
                    Environment.NewLine +
                    $"Are you sure you wish to unlock this {objName}?",
                ok: "Yes",
                cancel: "Cancel"))
                return false;

            Unlock(path);
            return true;

        }

        /// <summary>Unlocks an object.</summary>
        public static void Unlock(string path)
        {
            SceneDataUtility.Set(path, Key, GetLock(path).Unlock());
            OnLockChanged(path, locked: false);
        }

        #endregion
        #region Object methods / proxy

        /// <summary>Gets if an object is locked.</summary>
        public static bool IsLocked(Object obj) =>
            IsLocked(obj, out var _, out var _);

        /// <summary>Gets if an object is locked.</summary>
        public static bool IsLocked(Object obj, out string by, out string message) =>
            IsLocked(AssetDatabase.GetAssetPath(obj), out by, out message);

        /// <summary>Gets the tooltip string that would be displayed on buttons for example.</summary>
        public static string GetTooltipString(Object obj) =>
            GetTooltipString(AssetDatabase.GetAssetPath(obj));

        /// <summary>Prompts to lock an object.</summary>
        public static bool PromptLock(Object obj) =>
            PromptLock(AssetDatabase.GetAssetPath(obj));

        /// <summary>Locks an object.</summary>
        public static void Lock(Object obj, string name = null, string message = null) =>
            Lock(AssetDatabase.GetAssetPath(obj), name, message);

        /// <summary>Prompts to unlock an object.</summary>
        public static bool PromptUnlock(Object obj, string objName = "object") =>
            PromptUnlock(AssetDatabase.GetAssetPath(obj), objName);

        /// <summary>Unlocks an object.</summary>
        public static void Unlock(Object obj) =>
            Unlock(AssetDatabase.GetAssetPath(obj));

        #endregion

    }

}

#endif

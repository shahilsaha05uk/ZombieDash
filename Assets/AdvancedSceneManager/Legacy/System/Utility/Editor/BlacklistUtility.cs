#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AdvancedSceneManager.Editor.Utility
{

    /// <summary>Provides methods for working with the blacklist.</summary>
    /// <remarks>Only available in editor.</remarks>
    public static class BlacklistUtility
    {

        [Serializable]
        /// <summary>The settings of the blacklist system. A reference to the current settings may be obtained from the current profile.</summary>
        public class BlacklistModule
        {

            [NonSerialized] internal bool isWhyBlacklistOpen;
            [NonSerialized] internal bool isWarning;
            [NonSerialized] internal string message;

            /// <summary>Specifies whatever this module is a blacklist or a whitelist.</summary>
            public bool isWhitelist = false;

            /// <summary>Gets the paths of this module.</summary>
            public List<string> paths = new List<string>();

            /// <summary>Gets if this <see cref="BlacklistModule"/> is valid.</summary>
            public void GetStatus(out bool isWarning, out string message)
            {

                var scenes =
                    AssetDatabase.FindAssets("t:SceneAsset").
                    Select(AssetDatabase.GUIDToAssetPath).
                    Select(path =>
                    {
                        var isBlocked = IsBlocked(path, out var overriden);
                        return (path, isBlocked, overriden);
                    }).
                    Where(p => !p.isBlocked).
                    ToArray();

                var overrideCount = scenes.Where(s => s.overriden).Count();

                message = "";
                isWarning = false;

                if (!paths.Where(p => !string.IsNullOrWhiteSpace(p)).Any())
                {

                    message = isWhitelist
                        ? $"No paths added to whitelist. No scenes" + (overrideCount > 0 ? $" (except for {overrideCount} overriden by dynamic collections)" : "")
                        : $"No paths added to blacklist. All {scenes.Length} scenes";

                    isWarning = true;

                }
                else
                    message = scenes.Length + " scenes" + (overrideCount > 0 ? $" ({overrideCount} of which are included in dynamic collections)" : "");

                message += " will be processed.";

                this.isWarning = isWarning;
                this.message = message;

            }

            /// <summary>Clones the module.</summary>
            public BlacklistModule Clone() =>
                new BlacklistModule()
                {
                    isWhitelist = isWhitelist,
                    paths = new List<string>(paths)
                };

            /// <summary>Gets if asset is blocked for importing into ASM.</summary>
            public bool IsBlocked(string assetPath)
            {

                //Dynamic collections override blacklist
                if (SceneManager.profile && SceneManager.profile.IsSet(assetPath))
                    return false;

                var isBlocked = paths.
                    Where(path => !string.IsNullOrWhiteSpace(path)).
                    Any(path => assetPath == path || assetPath.StartsWith(path));

                return isWhitelist ? !isBlocked : isBlocked;

            }

            /// <summary>Gets if asset is blocked for importing into ASM.</summary>
            public bool IsBlocked(string assetPath, out bool isOverridenByDynamicCollection)
            {

                //Dynamic collections override blacklist
                isOverridenByDynamicCollection = SceneManager.profile && SceneManager.profile.IsSet(assetPath);
                if (isOverridenByDynamicCollection)
                    return false;

                var isBlocked = paths.
                    Where(path => !string.IsNullOrWhiteSpace(path)).
                    Any(path => assetPath == path || assetPath.StartsWith(path));

                return isWhitelist ? !isBlocked : isBlocked;

            }

        }

        /// <summary>Gets the current blacklist.</summary>
        public static BlacklistModule Blacklist => SceneManager.profile ? SceneManager.profile.blacklist : null;

        /// <summary>Gets if asset is blocked for importing into ASM.</summary>
        public static bool IsBlocked(string assetPath, out bool isOverridenByDynamicCollection)
        {
            isOverridenByDynamicCollection = false;
            return !SceneManager.profile || SceneManager.profile.blacklist.IsBlocked(assetPath, out isOverridenByDynamicCollection);
        }

        /// <summary>Gets if asset is blocked for importing into ASM.</summary>
        public static bool IsBlocked(string assetPath) =>
           !SceneManager.profile || SceneManager.profile.blacklist.IsBlocked(assetPath);

        #region GUI

        public static (bool didDirty, bool isInfoExpanded, float height) DrawGUI(BlacklistModule blacklist, string extraMessage = null)
        {

            var didDirty = false;

            var r = GUILayoutUtility.GetRect(100, 1);

            EditorGUI.BeginChangeCheck();
            DrawBlacklist(blacklist);

            bool isError = false;
            bool isWarning = false;
            string message = null;

            if (string.IsNullOrEmpty(message))
                blacklist.GetStatus(out isWarning, out message);

            if (EditorGUI.EndChangeCheck())
            {
                didDirty = true;
                blacklist.GetStatus(out isWarning, out message);
            }

            if (!string.IsNullOrEmpty(extraMessage))
                DrawInfoBox(false, true, extraMessage);
            DrawInfoBox(isError, isWarning, message);

            EditorGUILayout.Space();
            DrawInfo(blacklist, out var isInfoExpanded);

            var height = GUILayoutUtility.GetLastRect().yMax - r.yMin;

            return (didDirty, isInfoExpanded, height);

        }

        static readonly ReorderableList list = new ReorderableList(null, typeof(string), true, true, true, true);
        static void DrawBlacklist(BlacklistModule settings)
        {

            settings.isWhitelist = EditorGUILayout.Popup("Mode:", settings.isWhitelist ? 1 : 0, new[] { "Blacklist", "Whitelist" }) == 1;
            EditorGUILayout.Space();

            list.onCanRemoveCallback = (_) => true;
            list.list = settings.paths;
            list.onAddCallback = (_) => settings.paths.Add(GetCurrentPath());
            list.drawHeaderCallback = (position) => GUI.Label(position, "Folders:");
            list.drawElementCallback = (Rect position, int index, bool isActive, bool isFocused) =>
            {

                GUI.SetNextControlName("blacklist-" + index);
                settings.paths[index] = GUI.TextField(new Rect(position.x + 3, position.y + 2, position.width - 3 - 28, position.height - 4), settings.paths[index]);

                if (GUI.Button(new Rect(position.xMax - 22, position.y, 22, position.height), new GUIContent("...", "Pick folder..."), new GUIStyle(GUI.skin.button) { padding = new RectOffset(2, 2, 2, 2) }))
                {

                    var path =
                        AssetDatabase.IsValidFolder(settings.paths[index])
                        ? settings.paths[index]
                        : "Assets/";

                    path = EditorUtility.OpenFolderPanel("Pick folder", path, "");
                    if (!string.IsNullOrWhiteSpace(path))
                        settings.paths[index] = "Assets" + path.Replace(Application.dataPath, "");

                }
            };

            list.DoLayoutList();

            var name = GUI.GetNameOfFocusedControl();
            if (name.StartsWith("blacklist-"))
                list.index = int.Parse(name.Substring(name.IndexOf("-") + 1));

            EditorGUILayout.Space();

            if (Event.current.type == EventType.MouseDown)
                GUI.FocusControl("");

        }

        static bool isInfoOpen;
        static void DrawInfo(BlacklistModule blacklist, out bool isInfoExpanded)
        {

            if (isInfoOpen = EditorGUILayout.BeginFoldoutHeaderGroup(isInfoOpen, $"Why {(blacklist.isWhitelist ? "whitelist" : "blacklist")}?"))
            {

                var style = new GUIStyle(EditorStyles.wordWrappedLabel) { padding = new RectOffset(12, 12, 12, 12) };
                style.normal.background = EditorGUIUtility.whiteTexture;

                var color = GUI.backgroundColor;
                _ = ColorUtility.TryParseHtmlString("#4b4b4b", out var c);
                GUI.backgroundColor = c;

                EditorGUILayout.LabelField(
                    "In ASM we have a concept of asset refresh, this is a process that happens when a scene is " +
                    "created or removed, for example. It also happens in other situations just to cover edge cases. " +
                    "\n\n" +
                    "This process scans the project for all SceneAssets, and creates a Scene ScriptableObject from them, this is what " +
                    "enables that nice drag-and-drop functionality for scenes, that Unity severely lacks. " +
                    "\n\n" +
                    "The issue with this however, is the fact that this process is slow. We have done what we could with implementing partial " +
                    "refreshes, for example, but it is still slow. For small projects this might be fine, but the issue " +
                    "comes in when you use assets such as a world streamer, since these will to create a gazillion scenes, " +
                    "that then has to be needlessy proccessed by ASM." +
                    "\n\n" +
                    "That is the reason the blacklist / whitelist exists, so that asset " +
                    "refresh leaves those scenes alone, significantly improving refresh speed.", style);

                GUI.backgroundColor = color;

            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space();
            isInfoExpanded = isInfoOpen;

        }

        static void DrawInfoBox(bool isError, bool isWarning, string message)
        {

            var messageType = MessageType.Info;
            if (isError) messageType = MessageType.Error;
            else if (isWarning) messageType = MessageType.Warning;

            EditorGUILayout.HelpBox(message, messageType);

        }

        static string GetCurrentPath()
        {
            var projectWindowUtilType = typeof(ProjectWindowUtil);
            var getActiveFolderPath = projectWindowUtilType.GetMethod("GetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic);
            var path = (string)getActiveFolderPath.Invoke(null, null);
            return path;
        }

        #endregion

    }

}
#endif

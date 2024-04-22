#if UNITY_EDITOR

using System.Linq;
using System.Reflection;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.Utility
{

    /// <summary>Provides utility functions for working with dynamic collections.</summary>
    internal static class DynamicCollectionUtility
    {

        /// <summary>Updates dynamic collections.</summary>
        /// <remarks>This is a blocking operation.</remarks>
        public static void UpdateDynamicCollections(bool updateBuildSettings = true)
        {

            if (!Profile.current)
                return;

            var hasChanges = false;
            foreach (var path in Profile.current.dynamicCollectionPaths)
            {

                if (!AssetDatabase.IsValidFolder(path))
                    continue;

                var scenes = AssetDatabase.
                    FindAssets("t:SceneAsset", new[] { path }).
                    Select(AssetDatabase.GUIDToAssetPath).
                    ToArray();

                if (scenes.Any(s => !Profile.current.IsSet(path, s)))
                {
                    Profile.current.Set(path, scenes, false, isAuto: true);
                    hasChanges = true;
                }

            }

            foreach (var collection in Profile.current.dynamicCollections.Where(c => !c.isStandalone && !c.isASM & !Profile.current.dynamicCollectionPaths.Contains(c.title)))
                Profile.current.m_dynamicCollections.Remove(collection);

            //if (Profile.current.m_dynamicCollections.RemoveAll(c => c.isAuto && !Profile.current.m_dynamicCollectionPaths.Contains(c.title)) > 0)
            //    hasChanges = true;

            if (hasChanges)
            {
                Profile.current.Save();
                if (updateBuildSettings)
                    BuildUtility.UpdateSceneList();
            }

        }

        #region Editor

        internal static bool isInitialized;
        internal static void Initialize()
        {
            if (isInitialized)
                return;
            isInitialized = true;
            SettingsTab.instance.Add(new IMGUIContainer(OnGUI), SettingsTab.instance.DefaultHeaders.DynamicCollections);
            SettingsTab.instance.AddHeaderContent(Header(), SettingsTab.instance.DefaultHeaders.DynamicCollections);
        }

        static ReorderableList list = new ReorderableList(null, typeof(string), true, true, true, true);

        static Button applyButton;
        static VisualElement Header()
        {

            applyButton = new Button() { text = "Apply" };
            applyButton.style.height = 22;
            applyButton.style.marginLeft = applyButton.style.marginTop = applyButton.style.marginRight = applyButton.style.marginBottom = 6;
            applyButton.SetEnabled(hasChanges);

            applyButton.clicked += () =>
            {

                AssetDatabase.SaveAssets();
                UpdateDynamicCollections();

                hasChanges = false;
                applyButton.SetEnabled(false);
                if (SceneManager.settings.local.assetRefreshTriggers.HasFlag(ASMSettings.Local.AssetRefreshTrigger.DynamicCollectionsChanged))
                    AssetUtility.Refresh(evenIfInPlayMode: false, immediate: true);

            };

            return applyButton;

        }

        static GUIStyle button;
        static GUIStyle label;

        static bool hasChanges;
        static void OnGUI()
        {

            if (button == null)
                button = new GUIStyle(GUI.skin.button) { padding = new RectOffset(2, 2, 2, 2) };

            if (label == null)
                label = new GUIStyle(GUI.skin.label) { wordWrap = true };

            GUILayout.Label("ASM will ensure that the scenes under the following paths are included in build, even when not added to a collection:", label);

            GUI.enabled = !Application.isPlaying;

            EditorGUILayout.Space();

            if (!Profile.current)
                return;

            var paths = Profile.current.m_dynamicCollectionPaths;

            list.onCanRemoveCallback = (_) => true;
            list.list = Profile.current.m_dynamicCollectionPaths;
            list.onAddCallback = (_) => paths.Add(GetCurrentPath());
            list.drawHeaderCallback = (position) => GUI.Label(position, "Paths:");

            list.drawElementCallback = (Rect position, int index, bool isActive, bool isFocused) =>
            {

                GUI.SetNextControlName("dynamicCollection-" + index);
                paths[index] = GUI.TextField(new Rect(position.x + 3, position.y + 2, position.width - 3 - 28, position.height - 4), paths[index]);

                if (GUI.Button(new Rect(position.xMax - 22, position.y, 22, position.height), new GUIContent("...", "Pick folder..."), button))
                {

                    var path =
                        AssetDatabase.IsValidFolder(paths[index])
                        ? paths[index]
                        : "Assets/";

                    path = EditorUtility.OpenFolderPanel("Pick folder", path, "");
                    if (!string.IsNullOrWhiteSpace(path))
                        paths[index] = "Assets" + path.Replace(Application.dataPath, "");

                }

            };

            EditorGUI.BeginChangeCheck();
            list.DoLayoutList();

            if (EditorGUI.EndChangeCheck())
            {
                SceneManagerWindowProxy.RequestSave(Profile.current, updateBuildSettings: false);
                applyButton.SetEnabled(true);
                hasChanges = true;
            }

            var name = GUI.GetNameOfFocusedControl();
            if (name.StartsWith("dynamicCollection-"))
                list.index = int.Parse(name.Substring(name.LastIndexOf("-") + 1));

            var r = GUILayoutUtility.GetRect(Screen.width - 44, 0);
            var c = GUI.color;
            GUI.color = new Color(1, 1, 1, 0.5f);
            GUI.Label(new Rect(r.x, r.y - 20, r.width, 22), "Dynamic collections override Blacklist / whitelist.");
            GUI.color = c;

            if (Event.current.type == EventType.MouseDown)
                GUI.FocusControl("");

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

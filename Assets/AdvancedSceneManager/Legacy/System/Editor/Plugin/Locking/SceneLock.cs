#if ASM_PLUGIN_LOCKING

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Scene = UnityEngine.SceneManagement.Scene;

namespace AdvancedSceneManager.Plugin.Locking
{

    /// <summary>Responsible for adding lock buttons to scenes in heirarchy window, and preventing save on locked scenes.</summary>
    static class SceneLock
    {

        public static void OnLoad()
        {
            EditorSceneManager.sceneSaving += EditorSceneManager_sceneSaving;
            EditorSceneManager.sceneSaved += EditorSceneManager_sceneSaved;
            HierarchyGUIUtility.AddSceneGUI(OnLockButtonGUI);
            HierarchyGUIUtility.AddSceneGUI(OnWarningGUI);
            SceneHierarchyHooks.addItemsToSceneHeaderContextMenu += (menu, e) =>
            {
                menu.AddSeparator("");
                if (LockUtility.IsLocked(e.path))
                    menu.AddItem(new GUIContent("Unlock scene..."), false, () => LockUtility.PromptUnlock(e.path));
                else
                    menu.AddItem(new GUIContent("Lock scene..."), false, () => LockUtility.PromptLock(e.path));
            };
        }

        #region GUI

        static GUIStyle style;
        static GUIContent warning;
        static readonly Dictionary<string, GUIContent> toggleTooltips = new Dictionary<string, GUIContent>();

        static GUIContent GetTooltip(string path)
        {

            if (string.IsNullOrWhiteSpace(path))
                return GUIContent.none;

            if (!toggleTooltips.ContainsKey(path))
                toggleTooltips.Add(path, new GUIContent("", tooltip: LockUtility.GetTooltipString(path)));
            return toggleTooltips[path];

        }

        static bool OnLockButtonGUI(Scene scene)
        {

            var isLocked = LockUtility.IsLocked(scene.path);

            if (!UI.showButtons && !isLocked)
                return false;

            if (style == null)
                style = new GUIStyle("IN LockButton");

            if (warning == null)
                warning = new GUIContent(EditorGUIUtility.IconContent("console.warnicon.sml").image, tooltip: "This scene is locked, but has modifications, you will be asked to save scene as a new scene when saving.");

            EditorGUI.BeginChangeCheck();
            _ = GUILayout.Toggle(isLocked, GetTooltip(scene.path), style);
            if (EditorGUI.EndChangeCheck())
                _ = isLocked
                    ? LockUtility.PromptUnlock(scene.path)
                    : LockUtility.PromptLock(scene.path);

            return true;

        }

        static bool OnWarningGUI(Scene scene)
        {

            var isLocked = LockUtility.IsLocked(scene.path);
            if (isLocked && scene.isDirty)
            {
                _ = GUILayout.Button(warning, GUIStyle.none);
                return true;
            }

            return false;

        }

        #endregion
        #region Prevent save

        static readonly Dictionary<Scene, (string data, string path)> scenesToRestore = new Dictionary<Scene, (string data, string path)>();
        private static void EditorSceneManager_sceneSaving(Scene scene, string path)
        {

            path = Application.dataPath + "/" + path.Replace("Assets/", "");

            if (File.Exists(path) && LockUtility.IsLocked(scene.path) && scene.isDirty && !scenesToRestore.ContainsKey(scene))
            {

                var saveAs = EditorUtility.DisplayDialog(
                    title: "Locked scene...",
                    message:
                        $"The scene {scene.path} is locked, which means it cannot be saved." + Environment.NewLine +
                        Environment.NewLine +
                       LockUtility.GetTooltipString(scene.path),
                    ok: "Save scene as...",
                    cancel: "Cancel");

                var data = File.ReadAllText(path);
                _ = scenesToRestore.Set(scene, (data, path));

                if (saveAs)
                    SaveAs(scene);

            }

        }

        static void SaveAs(Scene scene)
        {
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            foreach (var obj in scene.GetRootGameObjects())
                EditorSceneManager.MoveGameObjectToScene(obj, newScene);
            EditorSceneManager.SaveOpenScenes();
        }

        private static void EditorSceneManager_sceneSaved(Scene scene)
        {
            if (scenesToRestore.TryGetValue(scene, out var data))
            {

                _ = scenesToRestore.Remove(scene);
                if (!File.Exists(data.path))
                    return;

                EditorApplication.delayCall += () =>
                {

                    if (!scene.isLoaded)
                        return;

                    var index = SceneUtility.GetAllOpenUnityScenes().ToList().IndexOf(scene);
                    var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

                    if (EditorSceneManager.sceneCount == 1)
                        DefaultSceneUtility.EnsureOpen();
                    CrossSceneReferenceUtilityProxy.ClearScene(scene);
                    var path = scene.path;
                    _ = EditorSceneManager.CloseScene(scene, true);

                    File.WriteAllText(data.path, data.data);
                    AssetDatabase.ImportAsset(path);

                    var newScene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

                    DefaultSceneUtility.Close();

                    //Make sure scene has same index in heirarchy as before
                    EditorSceneManager.MoveSceneBefore(newScene, EditorSceneManager.GetSceneAt(index));

                    //Reactivate previously active scene
                    if (activeScene.IsValid())
                        _ = EditorSceneManager.SetActiveScene(activeScene);
                    else
                        _ = EditorSceneManager.SetActiveScene(newScene);

                    //Ugh, this seems to be the only way to hide 'cross-scene references not supported' warning
                    //by unity that is produced some time after this code has run...
                    ClearConsole();

                };

            }
        }

        static void ClearConsole()
        {

            // This simply does "LogEntries.Clear()" the long way:
            var logEntries = Type.GetType("UnityEditor.LogEntries,UnityEditor.dll");

            var clearMethod = logEntries?.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            _ = clearMethod?.Invoke(null, null);

        }

    }

    #endregion

}

#endif

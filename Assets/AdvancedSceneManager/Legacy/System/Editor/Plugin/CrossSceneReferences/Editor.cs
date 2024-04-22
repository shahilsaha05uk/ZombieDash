#if UNITY_EDITOR && ASM_PLUGIN_CROSS_SCENE_REFERENCES

using System;
using System.Collections.Generic;
using System.Linq;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using Lazy.Utility;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using scene = UnityEngine.SceneManagement.Scene;

namespace AdvancedSceneManager.Plugin.Cross_Scene_References.Editor
{

    /// <summary>Manages editor functionality.</summary>
    public static class Editor
    {

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod]
#endif
        internal static void Initialize()
        {

            HierarchyGUIUtility.AddSceneGUI(OnSceneGUI, index: 1);
            HierarchyGUIUtility.AddGameObjectGUI(OnGameObjectGUI, index: 1);

            EditorSceneManager.preventCrossSceneReferences = false;

            AssemblyReloadEvents.afterAssemblyReload += AssemblyReloadEvents_afterAssemblyReload;

            EditorSceneManager.sceneSaving += EditorSceneManager_sceneSaving;
            EditorSceneManager.sceneSaved += EditorSceneManager_sceneSaved;
            EditorSceneManager.sceneOpened += EditorSceneManager_sceneOpened;
            EditorSceneManager.sceneClosed += EditorSceneManager_sceneClosed;
            BuildUtility.asmPreBuild += BuildEventsUtility_preBuild;

            CrossSceneReferenceUtilityProxy.clearScene += CrossSceneReferenceUtility.ResetScene;

            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            ResolveScenes();

        }

        #region Hierarchy indicator

        static GUIStyle hierarchyIconStyle;

        static GUIContent sceneHasLinksContent;
        static GUIContent sceneHasBrokenLinksContent;

        static GUIContent objHasLinksContent;
        static GUIContent objHasBrokenLinksContent;

        static void SetupGUI()
        {

            if (hierarchyIconStyle == null)
#if UNITY_2019
                hierarchyIconStyle = new GUIStyle(EditorStyles.miniButton);
#else
                hierarchyIconStyle = new GUIStyle(EditorStyles.iconButton);
#endif

            var iconName = "d_Linked";
#if UNITY_2019
            iconName = "DotFill";
#endif

            if (sceneHasLinksContent == null)
                sceneHasLinksContent = new GUIContent(EditorGUIUtility.IconContent(iconName).image, "This scene contains cross-scene references.\n\nPress to open cross-scene references debugger.");

            if (objHasLinksContent == null)
                objHasLinksContent = new GUIContent(EditorGUIUtility.IconContent(iconName).image);

            iconName = "d_Unlinked";
#if UNITY_2019
            iconName = "DotFrame";
#endif

            if (sceneHasBrokenLinksContent == null)
                sceneHasBrokenLinksContent = new GUIContent(EditorGUIUtility.IconContent(iconName).image, "One or more broken cross-scene references were found, references will not be saved until all are valid.\n\nPress to open cross-scene references debugger.");

            if (objHasBrokenLinksContent == null)
                objHasBrokenLinksContent = new GUIContent(EditorGUIUtility.IconContent(iconName).image);

        }

        static bool OnSceneGUI(scene scene)
        {

            var references = CrossSceneReferenceUtility.GetResolvedReferences(scene).ToArray();

            if (!references.Any())
                return false;

            SetupGUI();

            var content =
                references.Any(r => r.result != ResolveStatus.Succeeded)
                ? sceneHasBrokenLinksContent
                : sceneHasLinksContent;

            if (GUILayout.Button(content, hierarchyIconStyle))
                CrossSceneDebugger.Open();

            return true;

        }

        static bool OnGameObjectGUI(GameObject obj)
        {

            SetupGUI();

            OnVariable(CrossSceneReferenceUtility.GetResolvedReferences(obj));
            OnValue(CrossSceneReferenceUtility.GetResolvedReferencesValue(obj));

            return true;

        }

        static void OnVariable(IEnumerable<ResolvedCrossReference> references)
        {

            var content =
                references.Any(r => r.result != ResolveStatus.Succeeded)
                ? objHasBrokenLinksContent
                : objHasLinksContent;

            content.tooltip =
                string.Join("\n\n", references.Select(r => r.ToString())) +
                (references.Any(r => r.result != ResolveStatus.Succeeded) ? null : "\n\nPress to view linked object.");

            if (references.Any())
                if (GUILayout.Button(content, hierarchyIconStyle))
                {

                    var o = references.First().value.resolve.gameObject;

                    if (Selection.activeGameObject != o)
                        EditorGUIUtility.PingObject(o);

                    Selection.activeObject = o;

                }

        }

        static void OnValue(IEnumerable<ResolvedCrossReference> references)
        {

            var content =
                references.Any(r => r.result != ResolveStatus.Succeeded)
                ? objHasBrokenLinksContent
                : objHasLinksContent;

            content.tooltip =
                string.Join("\n\n", references.Select(r => r.ToString())) +
                (references.Any(r => r.result != ResolveStatus.Succeeded) ? null : "\n\nPress to view linked object.");

            if (references.Any())
                if (GUILayout.Button(content, hierarchyIconStyle))
                {

                    var o = references.First().variable.resolve.gameObject;

                    if (Selection.activeGameObject != o)
                        EditorGUIUtility.PingObject(o);

                    Selection.activeObject = o;

                }

        }

        #endregion
        #region Triggers / unity callbacks

        static void BuildEventsUtility_preBuild()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                foreach (var scene in SceneUtility.GetAllOpenUnityScenes().ToArray())
                    CrossSceneReferenceUtility.ResetScene(scene);
        }

        static void AssemblyReloadEvents_afterAssemblyReload() =>
            ResolveScenes();

        [PostProcessBuild]
        static void PostProcessBuild(BuildTarget target, string path) =>
            ResolveScenes();

        static void OnPlayModeChanged(PlayModeStateChange mode)
        {

            EditorSceneManager.preventCrossSceneReferences = false;

            CrossSceneReferenceUtility.ResetAllScenes();

            if (mode == PlayModeStateChange.EnteredPlayMode || mode == PlayModeStateChange.EnteredEditMode)
                ResolveScenes();

        }

        static void EditorSceneManager_sceneOpened(scene scene, OpenSceneMode mode) =>
            ResolveScenes();

        static void EditorSceneManager_sceneClosed(scene scene) =>
            ResolveScenes();

        static readonly List<string> scenesToIgnore = new List<string>();

        /// <summary>Ignores the specified scene.</summary>
        public static void Ignore(string scenePath, bool ignore)
        {
            if (ignore && !scenesToIgnore.Contains(scenePath))
                scenesToIgnore.Add(scenePath);
            else if (!ignore)
                _ = scenesToIgnore.Remove(scenePath);
        }

        static bool isAdding;
        static void EditorSceneManager_sceneSaving(scene scene, string path)
        {

            EditorSceneManager.preventCrossSceneReferences = false;
            if (isAdding || BuildPipeline.isBuildingPlayer || scenesToIgnore.Contains(path))
                return;

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                var references = CrossSceneReferenceUtility.GetResolvedReferences(scene).Where(r => r.variable.reference.Resolve().result != ResolveStatus.Succeeded).ToArray();
                foreach (var reference in references)
                    CrossSceneReferenceUtility.Remove(reference.reference);
            }

            isAdding = true;

            var l = new List<CrossSceneReference>();
            var newReferences = CrossSceneReferenceUtility.FindCrossSceneReferences(scene).ToArray();
            var referencesToCarryOver = CrossSceneReferenceUtility.Enumerate().FirstOrDefault(r => r.scene == path)?.references?.Where(r => r.variable.IsValid(returnTrueWhenSceneIsUnloaded: true)).ToArray() ?? Array.Empty<CrossSceneReference>();

            l.AddRange(referencesToCarryOver);
            l.AddRange(newReferences);

            var l1 = l.GroupBy(r => r.variable).
                Select(g => (oldRef: g.ElementAtOrDefault(0), newRef: g.ElementAtOrDefault(1))).
                Where(g =>
                {

                    //This is a bit confusing, but oldRef is newRef when no actual oldRef exist,
                    //we should probably improve this to be more readable
                    if (newReferences.Contains(g.oldRef))
                        g = (oldRef: null, newRef: g.oldRef);

                    //This is a new reference, or has been updated
                    if (g.newRef != null)
                        return true;

                    //This reference has not been updated to a new cross-scene target,
                    //but we still don't know if it has been set to null or to same scene,
                    //lets check if it is still valid (beyond unloaded target scene)
                    var shouldCarryOver = (g.oldRef?.value?.IsValid(returnTrueWhenSceneIsUnloaded: true) ?? false);
                    return shouldCarryOver;

                }).
                Select(g => g.newRef ?? g.oldRef).ToArray();

            CrossSceneReferenceUtility.ResetAllScenes();
            CrossSceneReferenceUtility.Save(scene, l1.ToArray());

            isAdding = false;

        }

        static void EditorSceneManager_sceneSaved(scene scene) =>
           ResolveScenes();

        #endregion

        static void ResolveScenes()
        {
            CoroutineUtility.Run(
                CrossSceneReferenceUtility.ResolveAllScenes,
                when: () => !EditorApplication.isCompiling && !BuildPipeline.isBuildingPlayer && SceneUtility.hasAnyScenes && Profile.current);
        }

    }

}

#endif

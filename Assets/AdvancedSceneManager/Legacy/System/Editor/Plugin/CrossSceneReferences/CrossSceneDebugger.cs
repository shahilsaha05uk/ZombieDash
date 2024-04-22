#if UNITY_EDITOR && ASM_PLUGIN_CROSS_SCENE_REFERENCES

using System.IO;
using System.Linq;
using AdvancedSceneManager.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using scene = UnityEngine.SceneManagement.Scene;

namespace AdvancedSceneManager.Plugin.Cross_Scene_References.Editor
{

    /// <summary>A window for debugging cross-scene references.</summary>
    public class CrossSceneDebugger : EditorWindow
    {

        [SerializeField] private SerializableStringBoolDict expanded = new SerializableStringBoolDict();
        SceneReferenceCollection[] references;

        /// <summary>Opens the cross-scene reference debugger.</summary>
        [MenuItem("Tools/Advanced Scene Manager/Window/Cross-scene reference debugger", priority = 52)]
        public static void Open()
        {
            var window = GetWindow<CrossSceneDebugger>();
            window.titleContent = new GUIContent("Cross-scene references");
            window.minSize = new Vector2(730, 300);
        }

        void OnEnable()
        {

            OnCrossSceneReferencesSaved();
            OnSceneStatusChanged();
            CrossSceneReferenceUtility.OnSaved += OnCrossSceneReferencesSaved;
            CrossSceneReferenceUtility.OnSceneStatusChanged += OnSceneStatusChanged;

            //Load variables from editor prefs
            var json = EditorPrefs.GetString("AdvancedSceneManager.CrossSceneDebugger", JsonUtility.ToJson(this));
            JsonUtility.FromJsonOverwrite(json, this);

        }

        void OnFocus() =>
            Reload();

        void OnDisable()
        {

            CrossSceneReferenceUtility.OnSaved -= OnCrossSceneReferencesSaved;

            //Save variables to editor prefs
            var json = JsonUtility.ToJson(this);
            EditorPrefs.SetString("AdvancedSceneManager.CrossSceneDebugger", json);

        }

        void OnCrossSceneReferencesSaved() =>
            Reload();

        void OnSceneStatusChanged() =>
            Repaint();

        void Reload()
        {
            references = CrossSceneReferenceUtility.Enumerate();
            Repaint();
        }

        #region OnGUI

        GUIStyle noItemsStyle;
        Vector2 scrollPos;
        void OnGUI()
        {

            if (noItemsStyle == null)
                noItemsStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };

            if (references?.Any() ?? false)
            {

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                _ = EditorGUILayout.BeginVertical(new GUIStyle() { margin = new RectOffset(64, 64, 42, 42) });

                foreach (var scene in references)
                    if (DrawHeader(scene.scene, Path.GetFileNameWithoutExtension(scene.scene)))
                        foreach (var item in scene.references)
                            Draw(item);

                EditorGUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                EditorGUILayout.EndScrollView();

            }
            else
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("No cross-scene references exists in the project.\n", noItemsStyle);
                GUILayout.Label("You can create some by dragging and dropping some references around from different scenes. ", noItemsStyle);
                GUILayout.Label("They will show up here when detected.", noItemsStyle);
                GUILayout.FlexibleSpace();
            }

        }

        bool DrawHeader(string key, string header) =>
            expanded.Set(key, EditorGUILayout.Foldout(expanded.GetValue(key), header, toggleOnLabelClick: true, EditorStyles.foldout));

        GUIStyle button;
        void Draw(CrossSceneReference reference)
        {

            EditorGUILayout.Space();

            GUILayout.BeginVertical(new GUIStyle(GUI.skin.window), GUILayout.Height(82), GUILayout.MaxWidth(10));

            DrawSubHeader(reference);

            if (button == null)
            {
                button = new GUIStyle(GUI.skin.button);
                button.normal.background = null;
            }

            var r = GUILayoutUtility.GetLastRect();
            r = new Rect(r.xMax - 22, r.y - 18, 22, 22);
            if (GUI.Button(r, new GUIContent("x", "Remove"), button))
            {
                CrossSceneReferenceUtility.Remove(reference);
                Reload();
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            Draw((reference.variable, CrossSceneReferenceUtility.GetResolved(reference).variable.resolve), "Variable:");
            EditorGUILayout.Space();
            Draw(((reference.value, CrossSceneReferenceUtility.GetResolved(reference).value.resolve)), "Value:");

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            GUILayout.EndVertical();
            EditorGUILayout.Space();

        }

        void DrawSubHeader(CrossSceneReference reference)
        {

            var resolved = CrossSceneReferenceUtility.GetResolved(reference);

            string str;
            if (resolved.variable.resolve.gameObject)
                str = resolved.variable.resolve.ToString(includeScene: false);
            else
                str = reference.variable.ToString();

            _ = EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space(12, false);
            GUILayout.Label(str, new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            EditorGUILayout.EndHorizontal();

        }

        void Draw((ObjectReference reference, ResolvedReference resolve) r, string label)
        {

            (ObjectReference reference, ResolvedReference resolve) = r;

            _ = EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space(12, false);

            EditorGUILayout.LabelField(label, GUILayout.Width(64));

            GUI.enabled = false;
            _ = EditorGUILayout.ObjectField(AssetDatabase.LoadAssetAtPath<SceneAsset>(reference.scene), typeof(SceneAsset), allowSceneObjects: false, GUILayout.Width(200));
            GUI.enabled = true;

            if (resolve.scene.HasValue && resolve.scene.Value.isLoaded)
            {
                GUI.enabled = false;
                _ = EditorGUILayout.ObjectField(resolve.resolvedTarget, typeof(Object), allowSceneObjects: false, GUILayout.Width(200));
                GUI.enabled = true;
            }
            else
            {
                GUILayout.Label("--Scene not loaded--", GUILayout.ExpandWidth(false));
                if (GUILayout.Button(new GUIContent("+", "Open scene additively to get more info"), GUILayout.ExpandWidth(false)))
                    _ = EditorSceneManager.OpenScene(reference.scene, OpenSceneMode.Additive);
            }

            EditorGUILayout.Space();
            EditorGUILayout.EndHorizontal();

        }

        #endregion

    }

}
#endif

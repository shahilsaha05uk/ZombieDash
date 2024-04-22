using System.Collections.Generic;
using System.IO;
using System.Linq;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using UnityEditor;
using UnityEngine;

namespace AdvancedSceneManager.Editor
{

    [CustomEditor(typeof(SceneAsset))]
    class SceneEditor : UnityEditor.Editor
    {

        public SceneAsset sceneAsset;
        public Scene scene;
        public string path;

        void OnEnable()
        {

            sceneAsset = (SceneAsset)target;
            path = AssetDatabase.GetAssetPath(sceneAsset);
            _ = SceneImportUtility.GetImportedScene(path, out scene);

            SceneImportUtility.scenesChanged -= ScenesChanged;
            SceneImportUtility.scenesChanged += ScenesChanged;

        }

        void OnDisable() =>
            SceneImportUtility.scenesChanged += ScenesChanged;

        void ScenesChanged() =>
            OnEnable();

        public override void OnInspectorGUI()
        {

            if (FallbackSceneUtility.GetStartupScene() == path)
                return;

            GUI.enabled = true;

            GUILayout.BeginVertical(new GUIStyle() { padding = new(8, 4, 8, 4) });

            Import();
            SceneOptions();
            StandaloneOptions();
            Preview();

            GUILayout.EndVertical();

            GUI.enabled = true;
            EditorGUI.showMixedValue = false;

        }

        void Import()
        {

            GUILayout.BeginArea(new Rect(0, 8, Screen.width - 4, 20));
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.enabled = !scene || (scene.isImported && scene.sceneAsset);

            if (scene && GUILayout.Button(new GUIContent("Unimport", "Unimport from ASM")))
                SceneImportUtility.Unimport(scene);

            else if (!scene && SceneImportUtility.StringExtensions.IsValidSceneToImport(path) && GUILayout.Button(new GUIContent("Import", "Import into ASM")))
                scene = SceneImportUtility.Import(path);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

        }

        void SceneOptions()
        {

            if (!scene)
                return;

            GUILayout.Label("Options:", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            scene.keepOpenWhenCollectionsClose = GUILayout.Toggle(scene.keepOpenWhenCollectionsClose, "Keep open when its collection is closed (persistent)");
            scene.keepOpenWhenNewCollectionWouldReopen = GUILayout.Toggle(scene.keepOpenWhenNewCollectionWouldReopen, "Don't reopen scene when newly opened collection would also open it");

            if (EditorGUI.EndChangeCheck())
                scene.Save();

        }

        void StandaloneOptions()
        {

            if (!scene || !Profile.current)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            GUILayout.Label("Standalone:", EditorStyles.boldLabel);

            var isStandalone = Profile.current.standaloneScenes.Contains(scene);

            EditorGUI.BeginChangeCheck();
            scene.openOnStartup = EditorGUILayout.Toggle("Open at startup", scene.openOnStartup);

            if (EditorGUI.EndChangeCheck())
            {
                if (scene.openOnStartup && !isStandalone)
                {
                    Debug.Log(scene.name + " was added to standalone.");
                    Profile.current.standaloneScenes.Add(scene);
                    Profile.current.Save();
                }
                scene.Save();
            }

        }

        UnityFileReader.Obj[] prettyPreview;
        readonly SerializableDictionary<string, bool> expanded = new();
        bool? isTooLong;

        const int maxLength = 2000000;

        void Preview()
        {

            if (!isTooLong.HasValue)
                isTooLong = new FileInfo(path).Length > maxLength;

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            GUILayout.Label("Preview:", EditorStyles.boldLabel);

            if (isTooLong ?? false)
            {
                var c = GUI.color;
                GUI.color = Color.gray;
                GUILayout.Label("Scene file too large to preview.");
                GUI.color = c;
                return;
            }

            GUILayout.BeginVertical(new GUIStyle() { padding = new(16, 12, 0, 12) });

            GUI.enabled = true;

            if (prettyPreview is null)
            {
                prettyPreview = UnityFileReader.GetObjects(path);
                Repaint();
            }

            EditorGUI.indentLevel = -1;
            foreach (var obj in prettyPreview)
                Draw(obj);
            EditorGUI.indentLevel = 0;

            GUILayout.EndVertical();

            void Draw(UnityFileReader.Obj obj)
            {

                EditorGUI.indentLevel += 1;

                if (!obj.isGameObject)
                    EditorGUILayout.LabelField(obj.name);
                else if (expanded.Set(obj.id, EditorGUILayout.Foldout(expanded.GetValueOrDefault(obj.id), " " + obj.name, true)))
                {


                    foreach (var component in obj.components)
                        Draw(component);

                    foreach (var child in obj.children)
                        Draw(child);


                }

                EditorGUI.indentLevel -= 1;

            }

        }

    }

}

#if UNITY_EDITOR

using System;
using System.Collections;
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
using UnityEngine.UIElements;
using static AdvancedSceneManager.SceneManager;
using scene = UnityEngine.SceneManagement.Scene;
using sceneManager = UnityEngine.SceneManagement.SceneManager;

namespace AdvancedSceneManager.Core
{

    /// <summary>A simplified scene manager for managing scenes in editor.</summary>
    /// <remarks>Only available in editor.</remarks>
    public class EditorManager
    {

        public event Action scenesUpdated;

        #region Replace Scene with SceneAsset in drag drop

        static void InitializeDragDropFix()
        {

            //Replaces Scene so referenses in drag drop to SceneAsset
            SceneView.duringSceneGui += (view) =>
            {
                if (Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform)
                    if (DragAndDrop.objectReferences.OfType<Scene>().Any())
                        DragAndDrop.objectReferences = DragAndDrop.objectReferences.Select(o => (o is Scene s && s) ? AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path) : o).ToArray();
            };

        }

        #endregion
        #region Settings

        static bool isInitialized;
        static VisualElement element;
        static void InitializeSettings()
        {

            if (isInitialized && element != null)
                return;
            isInitialized = true;

            if (element == null)
                element = new VisualElement();
            element.Clear();
            element.Add(Element());

            SettingsTab.instance.Add(header: SettingsTab.instance.DefaultHeaders.Options_Local, callback: element);

            VisualElement Element()
            {

                var element = new Toggle()
                {
                    label = "Open collection when SceneAsset opened:",
                    tooltip = "This opens the first found collection that a scene is contained in when opening an SceneAsset in editor."
                };
                element.SetValueWithoutNotify(settings.local.openAssociatedCollectionOnSceneAssetOpen);
                _ = element.RegisterValueChangedCallback(e => settings.local.openAssociatedCollectionOnSceneAssetOpen = e.newValue);
                return element;

            }

        }

        #endregion

        static GlobalCoroutine coroutine;
        internal static void Initialize()
        {

            InitializeSettings();
            InitializeDragDropFix();

            coroutine?.Stop();
            coroutine = Coroutine().StartCoroutine(description: "EditorSceneManager");
            IEnumerator Coroutine()
            {

                while (!runtime.isInitialized)
                    yield return null;

                if (!Profile.current || !Application.isPlaying)
                    yield break;

                var scenes = SceneUtility.GetAllOpenUnityScenes().ToArray();
                var collections = Profile.current.collections.Where(c => c && c.scenes.Where(s => s && c.Tag(s).openBehavior == SceneOpenBehavior.OpenNormally).All(s => scenes.Any(s1 => s1.path == s.path))).ToArray();

                //Since play mode was entered through regular play button, 
                //lets try to collection and standalone scenes and add them to the scene manager
                if (Application.isPlaying && !runtime.wasStartedAsBuild)
                {

                    var collection = collections.OrderByDescending(c => c.scenes.Any(s => s && s.isActive)).FirstOrDefault();

                    if (collection)
                        SceneManager.collection.Set(collection, SceneManager.standalone.openScenes.Where(s => collection.scenes.Contains(s.scene)).ToArray());

                    var scenesInCollection = collection ? scenes.Where(s => collection.scenes.Any(s1 => s1 ? s1.path == s.path : false)).ToArray() : Array.Empty<scene>();

                    var scenesToAdd = scenes.
                        Except(scenesInCollection).
                        Where(s => !standalone.openScenes.Any(s1 => s1?.path == s.path) && !DefaultSceneUtility.IsDefaultScene(s)).ToArray();

                    foreach (var scene in utility.openScenes)
                        if (scene.unityScene.HasValue)
                            PersistentUtility.Set(scene.unityScene.Value, collection ? collection.Tag(scene.scene).closeBehavior : SceneCloseBehavior.Close);

                    foreach (var scene in scenesToAdd)
                        if (Scene.Find(scene.path) is Scene s)
                            _ = SceneManager.standalone.GetTrackedScene(scene);

                    foreach (var scene in scenesInCollection)
                        if (Scene.Find(scene.path) is Scene s)
                            _ = SceneManager.collection.GetTrackedScene(scene);

                }

                editor.collections.AddRange(collections);
                editor.scenesUpdated?.Invoke();

            }

        }

        [OnOpenAsset]
        static bool OpenSingle(int instanceID, int _)
        {

            if (EditorUtility.InstanceIDToObject(instanceID) is SceneAsset scene)
            {

                var path = AssetDatabase.GetAssetPath(scene);
                if (settings.local.openAssociatedCollectionOnSceneAssetOpen && assets.collections?.FirstOrDefault(c => c && (c.scenes?.Any(s => s && s.path == path) ?? false)) is SceneCollection collection)
                    editor.Open(collection, promptSave: false);
                else
                    editor.OpenSingle(scene, promptSave: false);

                return true;

            }

            return false;

        }

        public void OpenSingle(SceneAsset scene, bool promptSave = true)
        {
            var path = AssetDatabase.GetAssetPath(scene);
            if (BlacklistUtility.IsBlocked(path) || AssetUtility.IsIgnored(path))
                _ = Open(path, additive: false);
            else
                OpenSingle(assets.allScenes.Find(path), promptSave);
        }

        public void OpenSingle(Scene scene, bool promptSave = true)
        {

            if (!scene)
                return;

            if (promptSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            CloseAll(promptSave: false);

            _ = Open(scene.path);
            if (!Application.isPlaying)
                DefaultSceneUtility.Close();

            PersistentSceneInEditorUtility.OpenAssociatedPersistentScenes(scene, promptSave: false);
            scenesUpdated?.Invoke();

        }

        public scene? Open(Scene scene, bool promptSave = true)
        {

            if (promptSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return null;

            if (scene)
            {
                var s = Open(scene.path);
                scenesUpdated?.Invoke();
                return s;
            }

            return null;

        }

        /// <summary>Opens scene without save prompts. Supports opening scene as readonly, if <see cref="LockUtility"/> is used.</summary>
        internal scene Open(string path, bool additive = true) =>
            EditorSceneManager.OpenScene(path, additive ? OpenSceneMode.Additive : OpenSceneMode.Single);

        readonly List<SceneCollection> collections = new List<SceneCollection>();
        public bool IsOpen(SceneCollection collection) => collections.Contains(collection);
        public bool CanClose(SceneCollection collection) => !(collections.Count == 1 && collections.First() == collection);

        public void Open(SceneCollection collection, bool additive = false, bool promptSave = true, bool forceOpenAllScenes = false)
        {

            if (promptSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (!additive)
                CloseAll(promptSave: false);
            else if (IsOpen(collection))
                Close(collection, promptSave: false);

            collections.Add(collection);

            var scenes = SceneUtility.GetAllOpenUnityScenes().ToArray();
            var persistentScenes = collection.scenes.SelectMany(s => PersistentSceneInEditorUtility.GetAssociatedScenes(s)).Distinct().Where(s => !scenes.Any(s1 => s1.path == s.path)).ToArray();
            foreach (var scene in persistentScenes)
                _ = Open(scene, promptSave: false);

            foreach (var scene in collection.scenes.Where(s => s))
                if (collection.Tag(scene).openBehavior == SceneOpenBehavior.OpenNormally || forceOpenAllScenes)
                    _ = Open(scene, promptSave: false);

            if (!Application.isPlaying)
                DefaultSceneUtility.Close();
            scenesUpdated?.Invoke();

            var active = collection.activeScene;
            if (!active)
                active = collection.scenes.FirstOrDefault();
            if (active)
            {
                var uScene = SceneUtility.GetAllOpenUnityScenes().FirstOrDefault(s => s.path == active.path);
                utility.SetActive(uScene);
            }

        }

        public void Close(SceneCollection collection, bool promptSave = true)
        {

            if (promptSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            DefaultSceneUtility.EnsureOpen();
            _ = this.collections.RemoveAll(c => c == collection);

            var collections = this.collections.GroupBy(c => c).Select(c => c.First());
            var scenes = collection.scenes.Where(s => !collections.Any(c => c.scenes.Contains(s)));

            foreach (var scene in scenes)
                Close(scene, promptSave: false);

            if (!Application.isPlaying && SceneUtility.GetAllOpenUnityScenes().Count() > 1)
                DefaultSceneUtility.Close();

            scenesUpdated?.Invoke();

        }

        public void Close(Scene scene, bool promptSave = true)
        {

            if (promptSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (scene)
                Close(sceneManager.GetSceneByPath(scene.path), promptSave: false);
            scenesUpdated?.Invoke();

        }

        public void Close(scene scene, bool promptSave = true)
        {

            if (promptSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (!scene.IsValid())
                return;

            if (SceneUtility.GetAllOpenUnityScenes().Count() > 1)
                _ = EditorSceneManager.CloseScene(scene, true);

            scenesUpdated?.Invoke();

        }

        public void CloseAll(bool promptSave = true)
        {

            if (promptSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            DefaultSceneUtility.EnsureOpen();

            foreach (var s in SceneUtility.GetAllOpenUnityScenes().ToArray())
            {
                if (DefaultSceneUtility.IsDefaultScene(s))
                    continue;
                _ = EditorSceneManager.CloseScene(s, true);
            }

            collections.Clear();
            scenesUpdated?.Invoke();

        }

        public bool IsOpen(Scene scene) =>
            SceneUtility.GetAllOpenUnityScenes().Any(s => scene && s.path == scene.path);

    }

}
#endif
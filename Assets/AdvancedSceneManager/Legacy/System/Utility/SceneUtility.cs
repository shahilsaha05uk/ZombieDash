#pragma warning disable IDE0051 // Remove unused private members

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using scene = UnityEngine.SceneManagement.Scene;
using sceneManager = UnityEngine.SceneManagement.SceneManager;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Core;
using System.Collections;
using Lazy.Utility;
using static AdvancedSceneManager.SceneManager;
using Scene = AdvancedSceneManager.Models.Scene;

#if UNITY_EDITOR
using UnityEditor.ProjectWindowCallback;
using UnityEditor;
using UnityEditor.SceneManagement;
using AdvancedSceneManager.Editor.Utility;
#endif

namespace AdvancedSceneManager.Utility
{

    /// <summary>An utility class to perform actions on scenes.</summary>
    public static class SceneUtility
    {

        /// <summary>Get all open unity scenes.</summary>
        public static IEnumerable<scene> GetAllOpenUnityScenes()
        {
            for (int i = 0; i < sceneManager.sceneCount; i++)
                yield return sceneManager.GetSceneAt(i);
        }

        /// <summary>Gets if current, and only, scene is the startup scene.</summary>
        public static bool isStartupScene =>
            SceneUtility.GetAllOpenUnityScenes().All(s => DefaultSceneUtility.IsDefaultScene(s) || DefaultSceneUtility.GetStartupScene() == s.path);

        /// <summary>Gets if there are any scenes open that are not dynamically created, and not yet saved to disk.</summary>
        public static bool hasAnyScenes => sceneManager.sceneCount > 0 && !(sceneCount == 1 && DefaultSceneUtility.IsDefaultScene(sceneManager.GetSceneAt(0)));

        /// <inheritdoc cref="sceneManager.sceneCount"/>
        public static int sceneCount => sceneManager.sceneCount;

        /// <inheritdoc cref="sceneManager.MoveGameObjectToScene(GameObject, scene)"/>
        public static void Move(this GameObject obj, Scene scene) =>
            Move(obj, scene.GetOpenSceneInfo());

        /// <inheritdoc cref="sceneManager.MoveGameObjectToScene(GameObject, scene)"/>
        public static void Move(this GameObject obj, OpenSceneInfo scene) =>
            Move(obj, scene.unityScene ?? default);

        /// <inheritdoc cref="sceneManager.MoveGameObjectToScene(GameObject, scene)"/>
        public static void Move(this GameObject obj, scene scene)
        {

            if (!scene.IsValid() || !obj)
                return;

            sceneManager.MoveGameObjectToScene(obj, scene);

        }

        /// <summary>Gets if the scene is included in build.</summary>
        public static bool IsIncluded(Scene scene) =>
            scene
            ? UnityEngine.SceneManagement.SceneUtility.GetBuildIndexByScenePath(scene.path) >= 0
            : false;

        #region Create

        /// <summary>Creates a scene at runtime, that is not saved to disk.</summary>
        public static OpenSceneInfo CreateDynamic(string name, UnityEngine.SceneManagement.LocalPhysicsMode localPhysicsMode = UnityEngine.SceneManagement.LocalPhysicsMode.None)
        {

            var scene = sceneManager.CreateScene(name, new UnityEngine.SceneManagement.CreateSceneParameters(localPhysicsMode));
            return new OpenSceneInfo(null, scene, SceneManager.standalone);

        }

#if UNITY_EDITOR

        /// <summary>Creates a scene, using <see cref="ProjectWindowUtil.CreateScene"/>.</summary>
        /// <remarks>Only usable in editor</remarks>
        /// <param name="collection">The collection to add the scene to.</param>
        /// <param name="index">The index of the scene in <paramref name="collection"/>, no effect if <paramref name="collection"/> is <see langword="null"/>.</param>
        /// <param name="replaceIndex">Replaces the scene at the specified index, rather than insert it.</param>
        /// <param name="save">Save collection to disk.</param>s
        public static void CreateInCurrentFolder(Action<Scene> onCreated, SceneCollection collection = null, int? index = null, bool replaceIndex = false, bool save = true)
        {

            var action = ScriptableObject.CreateInstance<CreateSceneAction>();
            action.onCreated = onCreated;
            action.collection = collection;
            action.index = index;
            action.replaceIndex = replaceIndex;
            action.save = save;

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                instanceID: 0,
                endAction: action,
                pathName: "New Scene.unity",
                icon: EditorGUIUtility.FindTexture("d_SceneAsset"),
                resourceFile: null);

        }

        class CreateSceneAction : EndNameEditAction
        {

            public Action<Scene> onCreated;
            public SceneCollection collection;
            public int? index;
            public bool replaceIndex;
            public bool save;

            public override void Action(int instanceId, string pathName, string resourceFile) =>
                Create(pathName, collection, index, replaceIndex, save);

        }

        /// <summary>Creates a scene, using save prompt for path. Returns <see langword="null"/> if save dialog cancelled.</summary>
        /// <remarks>Only usable in editor</remarks>
        /// <param name="collection">The collection to add the scene to.</param>
        /// <param name="index">The index of the scene in <paramref name="collection"/>, no effect if <paramref name="collection"/> is <see langword="null"/>.</param>
        /// <param name="replaceIndex">Replaces the scene at the specified index, rather than insert it.</param>
        /// <param name="save">Save collection to disk.</param>
        public static void Create(Action<Scene> onCreated, SceneCollection collection = null, int? index = null, bool replaceIndex = false, bool save = true)
        {

            _ = Coroutine().StartCoroutine();
            IEnumerator Coroutine()
            {

                if (!CreateAndPromptSaveNewScene(out var path))
                    yield break;

                SceneAsset asset = null;

                while (!asset)
                {
                    yield return null;
                    asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                }

                var scene = AssetUtility.Add(asset);
                AddToCollection(scene, collection, index, replaceIndex, save);
                onCreated?.Invoke(scene);

            }

        }

        /// <summary>Creates a scene at the specified path.</summary>
        /// <remarks>Only usable in editor</remarks>
        /// <param name="path">The path that the scene should be saved to.</param>
        /// <param name="collection">The collection to add the scene to.</param>
        /// <param name="index">The index of the scene in <paramref name="collection"/>, no effect if <paramref name="collection"/> is <see langword="null"/>.</param>
        /// <param name="replaceIndex">Replaces the scene at the specified index, rather than insert it.</param>
        /// <param name="save">Save collection to disk.</param>
        /// <param name="createSceneScriptableObject">If <see langword="false"/>, no <see cref="Scene"/> <see cref="ScriptableObject"/> will be created, scene also won't be added to <paramref name="collection"/>. Returns <see langword="null"/>.</param>
        public static Scene Create(string path, SceneCollection collection = null, int? index = null, bool replaceIndex = false, bool save = true, bool createSceneScriptableObject = true)
        {

            if (path is null)
                throw new ArgumentNullException(nameof(path));

            path = path.Replace('\\', '/');

            if (!path.StartsWith("Assets/"))
                path = "Assets/" + path;

            if (!path.EndsWith(".unity"))
                path += ".unity";

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            if (!sceneAsset)
            {

                const string template = "" +
                    "%YAML 1.1\n" +
                    "%TAG !u! tag:unity3d.com,2011:";

                if (!File.Exists(path) || File.ReadAllText(path) != template)
                {
                    Directory.GetParent(path).Create();
                    File.WriteAllText(path, template);
                }

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);

            }

            if (!sceneAsset)
                throw new Exception("Something went wrong when creating scene.");

            if (!createSceneScriptableObject)
                return null;

            var Scene = AssetUtility.Add(sceneAsset);
            AddToCollection(Scene, collection, index, replaceIndex, save);
            return Scene;

        }

        static bool CreateAndPromptSaveNewScene(out string path)
        {

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var saved = EditorSceneManager.SaveModifiedScenesIfUserWantsTo(new[] { scene });
            path = scene.path;
            _ = EditorSceneManager.CloseScene(scene, true);

            return saved;

        }

        static void AddToCollection(Scene scene, SceneCollection collection = null, int? index = null, bool replaceIndex = false, bool save = true)
        {

            if (collection)
            {

                var scenes = collection.scenes;

                if (index.HasValue && replaceIndex) //Replace
                    scenes[index.Value] = scene;
                else if (index.HasValue) //Insert
                    ArrayUtility.Insert(ref scenes, index.Value, scene);
                else //Add
                    ArrayUtility.Add(ref scenes, scene);

                collection.scenes = scenes;

                if (save)
                {
                    EditorUtility.SetDirty(collection);
                    AssetDatabase.SaveAssets();
                }

            }

        }

#endif

        #endregion
        #region Remove

#if UNITY_EDITOR

        /// <summary>Removes the <see cref="SceneAsset"/> at the specified path and its associated <see cref="Models.Scene"/>, and removes any references to it from any <see cref="SceneCollection"/>.</summary>
        public static void Remove(string path)
        {

            if (path is null)
                throw new ArgumentNullException(nameof(path));

            path = path.Replace('\\', '/');

            if (!path.StartsWith("Assets/"))
                path = "Assets/" + path;

            if (!path.EndsWith(".unity"))
                path += ".unity";


            AssetDatabase.DisallowAutoRefresh();

            foreach (var collection in SceneManager.assets.allCollections)
                if (collection.m_scenes.Contains(path))
                {
                    ArrayUtility.Remove(ref collection.m_scenes, path);
                    EditorUtility.SetDirty(collection);
                }

            AssetUtility.Remove(Find(path).FirstOrDefault());
            _ = AssetDatabase.DeleteAsset(path);

            AssetDatabase.AllowAutoRefresh();
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();

        }

        /// <summary>Removes the <paramref name="scene"/> and its associated <see cref="SceneAsset"/>, and removes any references to it from any <see cref="SceneCollection"/>.</summary>
        public static void Remove(Scene scene) =>
            Remove(scene ? scene.path : null);

#endif

        #endregion
        #region Find

        /// <summary>Find open scenes by name or path.</summary>
        public static IEnumerable<Scene> FindOpen(string nameOrPath) =>
            FindOpen(s => ((IASMObject)s).Match(nameOrPath));

        /// <summary>Find open scenes by predicate.</summary>
        public static IEnumerable<Scene> FindOpen(Func<Scene, bool> predicate) =>
            GetScenes(openOnly: true).Where(predicate);

        /// <summary>Find scenes by name or path, in the specified collection or profile, if defined.</summary>
        public static IEnumerable<Scene> Find(string nameOrPath, SceneCollection inCollection = null, Profile inProfile = null) =>
            Find(s => s && ((IASMObject)s).Match(nameOrPath), inCollection, inProfile);

        /// <summary>Find scenes by predicate, in the specified collection or profile, if defined.</summary>
        public static IEnumerable<Scene> Find(Func<Scene, bool> predicate, SceneCollection inCollection = null, Profile inProfile = null) =>
            GetScenes(inCollection, inProfile).Where(predicate);

        static Scene[] GetScenes(SceneCollection collection = null, Profile profile = null, bool openOnly = false)
        {

            if (openOnly)
                return GetAllOpenUnityScenes().Select(s => s.Scene().scene).Where(s => s).ToArray();
            else if (profile && collection)
                return profile.collections.Contains(collection)
                    ? collection.scenes
                    : Array.Empty<Scene>();
            else if (profile)
                return profile.scenes.ToArray();
            else if (collection)
                return collection.scenes;
            else
                return SceneManager.assets.allScenes.ToArray();

        }

        #endregion

#if UNITY_EDITOR
        #region Split

        static scene? newScene;
        [MenuItem("GameObject/Move game objects to new scene...", false, 11)]
        static void MoveToNewSceneItem() =>
            MoveToNewScene(Selection.objects.OfType<GameObject>().ToArray());

        [MenuItem("GameObject/Move game objects to new scene...", true)]
        static bool ValidateMoveToNewSceneItem() =>
            Selection.objects.Any();

        /// <summary>
        /// <para>Moves the object to a new scene.</para>
        /// <para>Only available in editor.</para>
        /// </summary>
        public static void MoveToNewScene(params GameObject[] objects)
        {

            newScene = newScene ?? UnityEditor.SceneManagement.EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            foreach (var obj in objects)
            {
                obj.transform.SetParent(null, worldPositionStays: true);
                sceneManager.MoveGameObjectToScene(obj, newScene.Value);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(newScene.Value);

        }

        class AssetModificationProcessor : UnityEditor.AssetModificationProcessor
        {

            static string[] OnWillSaveAssets(string[] paths)
            {
                newScene = null;
                return paths;
            }

        }

        #endregion
        #region Merge

        [MenuItem("Assets/Merge scenes...", priority = 200)]
        static void MergeSceneItem() =>
            MergeScenes(Selection.objects.OfType<SceneAsset>().Select(a => AssetDatabase.GetAssetPath(a)).ToArray());

        [MenuItem("Assets/Merge scenes...", validate = true)]
        static bool ValidateMergeSceneItem() =>
            Selection.objects.OfType<SceneAsset>().Count() > 1;

        /// <summary>Merges the scenes together, the first scene in the list will be the output scene.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static void MergeScenes(params string[] scenes)
        {

            scenes = scenes.Where(s => AssetDatabase.LoadAssetAtPath<SceneAsset>(s)).OrderByDescending(s => s).ToArray();

            if (scenes.Length < 2)
                return;

            var displayNames = scenes.Distinct().Select(s => s.Replace("Assets/", "").Replace(".unity", "")).ToArray();

            var (successful, selectedValue) =
                PickOptionPrompt.Prompt(
                title: "Combining scenes",
                message: "Are you sure you wish to combine the following scenes?\nThe scenes will be moved to recycle bin, including current version of the target scene, in order to allow them to be restored.\n" + Environment.NewLine +
                    string.Join(Environment.NewLine, displayNames) + Environment.NewLine + Environment.NewLine +
                    "Select target scene:",
                options: displayNames);

            if (successful && UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {

                var targetIndex = Array.IndexOf(displayNames, selectedValue);

                var setup = UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
                UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(scenes.Select((s, i) => new SceneSetup() { path = s, isLoaded = true, isActive = i == targetIndex }).ToArray());

                var targetScene = UnityEditor.SceneManagement.EditorSceneManager.GetSceneAt(targetIndex);

                //Move targetScene to trash as well, so that the user can recover previous state
                _ = AssetDatabase.MoveAssetToTrash(targetScene.path);
                _ = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(targetScene);

                for (int i = 0; i < UnityEditor.SceneManagement.EditorSceneManager.sceneCount; i++)
                {

                    if (i == targetIndex)
                        continue;

                    var scene = UnityEditor.SceneManagement.EditorSceneManager.GetSceneAt(i);
                    var objects = scene.GetRootGameObjects();
                    if (objects.Length > 0)
                    {

                        _ = new GameObject("--" + Path.GetFileNameWithoutExtension(scene.path) + "--");
                        foreach (var obj in objects)
                        {
                            obj.transform.SetParent(null, worldPositionStays: true);
                            sceneManager.MoveGameObjectToScene(obj, targetScene);
                            obj.transform.SetAsLastSibling();
                        }

                    }

                    _ = AssetDatabase.MoveAssetToTrash(scene.path);
                    AssetUtility.Remove(SceneUtility.Find(scene.path).FirstOrDefault());
                    _ = UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, removeScene: true);

                }

                _ = UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(targetScene);
                _ = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(targetScene);

                setup = setup.Where(s => AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path)).ToArray();
                if (setup.Length == 0)
                    setup = new SceneSetup[] { new SceneSetup() { path = targetScene.path, isActive = true, isLoaded = true } };
                UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(setup);

            }

        }

        #endregion
#endif

        /// <summary>Gets the runtime info of the associated scene to this <see cref="Component"/>.</summary>
        public static OpenSceneInfo Scene(this Component component) =>
            component && component.gameObject
            ? Scene(component.gameObject)
            : null;

        /// <summary>Gets the runtime info of the associated scene to this <see cref="GameObject"/>.</summary>
        public static OpenSceneInfo Scene(this GameObject gameObject) =>
            gameObject
            ? Scene(gameObject.scene)
            : null;

        /// <summary>Gets the ASM runtime info of this <see cref="scene"/>.</summary>
        public static OpenSceneInfo Scene(this scene scene) =>
            utility.FindOpenScene(SceneManager.assets.allScenes.Find(scene.path));

#if UNITY_EDITOR
        /// <summary>Finds the asm representation of this <see cref="SceneAsset"/>.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static Scene FindASMScene(this SceneAsset scene) =>
            Find(AssetDatabase.GetAssetPath(scene)).FirstOrDefault();
#endif

    }

}

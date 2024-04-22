using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Core;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Models.Enums;
using AdvancedSceneManager.Utility;
using Lazy.Utility;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AdvancedSceneManager.Editor
{

    class UnincludedSceneLoader : SceneLoader
    {

        public override bool isGlobal => true;

        public override bool CanOpen(Scene scene) =>
            !scene.isIncluded;

        public override IEnumerator LoadScene(Scene scene, SceneLoadArgs e)
        {

            if (!Profile.current)
                yield break;

            Profile.current.standaloneScenes.Add(scene);
            BuildUtility.UpdateSceneList(true);
            Debug.LogWarning("The scene '" + scene.path + "' could not be opened, as was not included in build. It has been added to standalone, but play mode must be restarted to update build scene list.");
            e.SetCompletedWithoutScene();

        }

        public override IEnumerator UnloadScene(Scene scene, SceneUnloadArgs e)
        {
            yield break;
        }

    }

    [InitializeOnLoad]
    sealed class EditorSceneLoader : SceneLoader
    {

        public override string sceneToggleText { get; }
        public override bool activeOutsideOfPlayMode => true;
        public override bool activeInPlayMode => false;

        #region Register

        static EditorSceneLoader() =>
            SceneManager.OnInitialized(() =>
            {

                SceneManager.runtime.AddSceneLoader<UnincludedSceneLoader>();

                if (!Application.isPlaying)
                    EditorApplication.delayCall += OnEnable;
                SceneOperation.beforeStart += BeforeStart;

                EditorApplication.playModeStateChanged += state =>
                {
                    if (state == PlayModeStateChange.EnteredEditMode)
                        OnEnable();
                    else if (state == PlayModeStateChange.EnteredPlayMode)
                    {
                        OnDisable();
                        OpenPlayModeScenes();
                    }
                };

            });

        static void OnEnable()
        {

            OnDisable();

#if UNITY_2022_1_OR_NEWER
            EditorSceneManager.sceneManagerSetupRestored += SceneSetupRestored;
#endif
            EditorApplication.playModeStateChanged += PlayModeChanged;
            BuildUtility.postBuild += OnPostBuild;

            SceneManager.runtime.AddSceneLoader<EditorSceneLoader>();

            SetupTracking();
            Reload();
            PersistOpenCollection();

            BuildUtility.UpdateSceneList();

        }

        static void OpenPlayModeScenes()
        {
            if (Application.isPlaying && !SceneManager.app.isBuildMode && Profile.current)
                foreach (var scene in Profile.current.standaloneScenes.Where(s => s && s.openOnPlayMode && !s.isOpenInHierarchy))
                    scene.Open();
        }

        static void OnDisable()
        {

#if UNITY_2022_1_OR_NEWER
            EditorSceneManager.sceneManagerSetupRestored -= SceneSetupRestored;
#endif
            EditorApplication.playModeStateChanged -= PlayModeChanged;
            BuildUtility.postBuild -= OnPostBuild;

            SceneManager.runtime.RemoveSceneLoader<EditorSceneLoader>();

            QueueUtility<SceneOperation>.StopAll();

        }

        static void SceneSetupRestored(UnityEngine.SceneManagement.Scene[] scenes) =>
            scenes.ForEach(Track);

        static void PlayModeChanged(PlayModeStateChange state)
        {
            EditorApplication.delayCall += Reload;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {

                if (!SceneManager.app.isBuildMode)
                    foreach (var scene in SceneManager.openScenes)
                        _ = CallbackUtility.DoSceneOpenCallbacks(scene).StartCoroutine();

            }
        }

        static void OnPostBuild(BuildUtility.PostBuildEventArgs _) =>
            EditorApplication.delayCall += Reload;

        static void Reload() =>
            TrackScenes();

        static void BeforeStart(SceneOperation operation)
        {

            if (Application.isPlaying)
                return;

            var untitledScenes = SceneUtility.GetAllOpenUnityScenes().Where(s => s.rootCount == 0 && !s.isDirty && !AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path)).ToArray();
            foreach (var scene in untitledScenes)
            {
                FallbackSceneUtility.EnsureOpen();
                EditorSceneManager.CloseScene(scene, true);
            }

            operation.DisableLoadingScreen();

            var scenes = operation.open.Any() ? operation.open : SceneManager.openScenes;
            foreach (var scene in scenes)
                operation.PrependOpen(FindAutoOpenScenes(scene));

            if (!PromptSave(operation.close))
                operation.Cancel();

        }

        static bool PromptSave(IEnumerable<Scene> scenes) =>
            !scenes.Any(s => s.internalScene?.isDirty ?? false) || EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        static IEnumerable<Scene> FindAutoOpenScenes(Scene scene)
        {

            return SceneManager.assets.scenes.Where(ShouldOpen);

            bool ShouldOpen(Scene s)
            {

                if (s.autoOpenInEditor is EditorPersistentOption.AnySceneOpened)
                    return true;
                else if (s.autoOpenInEditor is EditorPersistentOption.WhenAnyOfTheFollowingScenesAreOpened && s.autoOpenInEditorScenes.Contains(scene))
                    return true;

                return false;

            }

        }

        #endregion
        #region Tracking

        static void TrackScenes()
        {

            if (Application.isPlaying)
                return;

            //Make sure open scenes are tracked
            foreach (var scene in SceneUtility.GetAllOpenUnityScenes().ToArray())
            {

                if (!scene.ASMScene(out var s))
                    continue;

                //Persist preloaded scene
                if (!scene.isLoaded)
                {
                    Debug.Log("About to track preload 2");
                    SceneManager.runtime.TrackPreload(s, () => FinishPreloadCallback(scene));
                }

                Track(s, scene);

            }

        }

        static void Track(Scene scene, UnityEngine.SceneManagement.Scene unityScene) => SceneManager.runtime.Track(scene, unityScene);
        static void Track(Scene scene) => SceneManager.runtime.Track(scene);
        static void Untrack(Scene scene) => SceneManager.runtime.Untrack(scene);

        static void SetupTracking()
        {

            TrackScenes();

            EditorSceneManager.sceneOpened += (e, _) => Track(e);
            EditorSceneManager.sceneClosed += (e) => Untrack(e);

        }

        static void Track(UnityEngine.SceneManagement.Scene scene)
        {
            if (scene.ASMScene(out var s))
                Track(s, scene);
        }

        static void Untrack(UnityEngine.SceneManagement.Scene scene)
        {
            if (scene.ASMScene(out var s))
                Untrack(s);
        }

        static void PersistOpenCollection()
        {

            var id = EditorPrefs.GetString("ASM.OpenCollection");
            var collection = SceneManager.assets.collections.Find(id);
            if (collection)
                SceneManager.runtime.Track(collection);

            SceneManager.runtime.collectionOpened -= SetCollection;
            SceneManager.runtime.collectionClosed -= SetCollection;

            SceneManager.runtime.collectionOpened += SetCollection;
            SceneManager.runtime.collectionClosed += SetCollection;

            static void SetCollection(SceneCollection collection)
            {
                if (!Application.isPlaying)
                    EditorPrefs.SetString("ASM.OpenCollection", collection ? collection.id : "");
            }

        }

        #endregion
        #region Load / unload

        public override IEnumerator LoadScene(Scene scene, SceneLoadArgs e)
        {
            if (!Application.isPlaying)
            {

                if (!e.isPreload)
                {
                    var uScene = EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Additive);
                    e.SetCompleted(uScene);
                }
                else
                {
                    var uScene = EditorSceneManager.OpenScene(scene.path, OpenSceneMode.AdditiveWithoutLoading);
                    e.SetCompleted(uScene, () => FinishPreloadCallback(e.scene));
                }

                FallbackSceneUtility.Close();
                yield break;

            }
        }

        static IEnumerator FinishPreloadCallback(Scene scene)
        {
            var uScene = EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Additive);
            scene.internalScene = uScene;
            yield break;
        }

        public override IEnumerator UnloadScene(Scene scene, SceneUnloadArgs e)
        {

            if (e.scene.internalScene.HasValue)
            {
                FallbackSceneUtility.EnsureOpen();
                EditorSceneManager.CloseScene(e.scene.internalScene.Value, true);
                FallbackSceneUtility.Close();
                e.SetCompleted();
            }

            yield break;

        }

        #endregion
        #region SceneAsset open

        [OnOpenAsset]
        static bool OnOpen(int instanceID)
        {

            if (Application.isPlaying)
                return false;

            var sceneAsset = EditorUtility.InstanceIDToObject(instanceID) as SceneAsset;
            if (!sceneAsset || !sceneAsset.ASMScene(out var scene))
                return false;

#if !COROUTINES
            return false;
#else

            if (SceneManager.settings.user.openCollectionOnSceneAssetOpen && scene.FindCollection(out var collection))
            {
                collection.Open();
                return true;
            }

            SceneManager.runtime.UntrackCollections();
            SceneManager.runtime.CloseAll(false, false).Open(scene);

            return true;

#endif

        }

        #endregion

    }

}

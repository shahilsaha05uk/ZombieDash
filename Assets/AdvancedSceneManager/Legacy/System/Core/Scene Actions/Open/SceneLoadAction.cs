using System.Collections;
using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using UnityEngine;
using UnityEngine.SceneManagement;
using scene = UnityEngine.SceneManagement.Scene;
using Scene = AdvancedSceneManager.Models.Scene;
using sceneManager = UnityEngine.SceneManagement.SceneManager;

namespace AdvancedSceneManager.Core.Actions
{

    /// <summary>Loads a scene.</summary>
    public sealed class SceneLoadAction : SceneAction
    {

        public static Scene currentScene { get; private set; }
        public static bool isCurrentPreload { get; private set; }

        public bool isPreload { get; private set; }

        public SceneLoadAction(Scene scene, SceneCollection collection = null, bool isPreload = false)
        {

            this.scene = scene;
            this.collection = collection ? collection : SceneManager.collection.current;
            this.isPreload = isPreload;

            if (!scene)
            {
                Done();
                return;
            }

        }

        public override IEnumerator DoAction(SceneManagerBase _sceneManager)
        {

            if (!scene)
            {
                Done();
                yield break;
            }

            currentScene = scene;
            isCurrentPreload = isPreload;

            if (scene.GetOpenSceneInfo()?.isOpen ?? false)
            {
                Done();
                yield break;
            }

            var e = new SceneLoadOverrideArgs()
            {
                scene = scene,
                collection = collection,
                isPreload = isPreload,
                updateProgress = OnProgress
            };

            if (SceneManager.utility.sceneLoadOverride != null)
                yield return SceneManager.utility.sceneLoadOverride?.Invoke(e);
            if (!e.isHandled)
                yield return LoadScene(e);

            var loadedScene = e.returnValue;

            if (loadedScene.IsValid())
            {
                SetPersistentFlag(loadedScene);
                AddScene(e, _sceneManager);
                Done(loadedScene);
            }
            else
            {
                Debug.LogError($"Could not open scene {(scene ? $" ('{scene.name}')" : "")} due to unknown error.");
                Done();
            }

        }

        static IEnumerator LoadScene(SceneLoadOverrideArgs e)
        {

            if (!e.CheckIsIncluded())
                yield break;

            if (e.isPreload)
            {
                yield return sceneManager.LoadSceneAsync(e.scene.path, LoadSceneMode.Additive).Preload(out var activateCallback).WithProgress(e.ReportProgress);
                e.SetCompleted(e.GetOpenedScene(), activateCallback);
            }
            else
            {
                yield return sceneManager.LoadSceneAsync(e.scene.path, LoadSceneMode.Additive).WithProgress(e.ReportProgress);
                e.SetCompleted(e.GetOpenedScene());
            }

        }

        void SetPersistentFlag(scene scene) =>
            PersistentUtility.Set(scene, collection
            ? collection.Tag(SceneManager.assets.allScenes.Find(scene.path)).closeBehavior
            : SceneCloseBehavior.Close);

        void AddScene(SceneLoadOverrideArgs e, SceneManagerBase sceneManager)
        {

            var trackedScene = sceneManager.GetTrackedScene(e.returnValue);

            if (e.isPreload && e.preloadCallback != null && trackedScene != null)
                SceneManager.standalone.preloadedScene = new PreloadedSceneHelper(trackedScene, e.preloadCallback);

            if (trackedScene != null)
                sceneManager.RaiseSceneOpened(trackedScene);

        }

        protected override void Done()
        {
            if (currentScene == scene)
            {
                currentScene = null;
                isCurrentPreload = false;
            }
            base.Done();
        }


    }

}

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using AdvancedSceneManager.Utility;
using AdvancedSceneManager.Core;
using AdvancedSceneManager.Core.Actions;
using Lazy.Utility;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using AdvancedSceneManager.Editor.Utility;
#endif

using static AdvancedSceneManager.SceneManager;

using scene = UnityEngine.SceneManagement.Scene;
using Scene = AdvancedSceneManager.Models.Scene;
using sceneManager = UnityEngine.SceneManagement.SceneManager;
using System;

namespace AdvancedSceneManager
{

    namespace Utility
    {

        //This is defined here, instead of its own file, because unity will display it in the object picker when searching for scene helper

        /// <summary>An helper class to make working with preloaded scenes easier, contains method for finish loading and discarding preloaded scene.</summary>
        public class PreloadedSceneHelper
        {

            static Func<IEnumerator> callback { get; set; }

            public PreloadedSceneHelper(OpenSceneInfo scene, Func<IEnumerator> callback)
            {
                this.scene = scene;
                PreloadedSceneHelper.callback = callback;
            }

            /// <summary>The scene that was preloaded.</summary>
            public OpenSceneInfo scene { get; private set; }

            /// <summary>Gets whatever the scene is still preloaded.</summary>
            public bool isStillPreloaded => scene?.isPreloaded ?? false;

            /// <summary>Finishes loading scene.</summary>
            public SceneOperation<OpenSceneInfo> FinishLoading()
            {

                if (!isStillPreloaded || scene.sceneManager == null)
                    return SceneOperation<OpenSceneInfo>.done;

                var finishLoad = new FinishLoadAction();
                var callbacks = new SceneOpenCallbackAction(scene);

                var operation =
                    SceneOperation.Add<OpenSceneInfo>(standalone, ignoreQueue: true).
                    Return(o => scene).
                    WithAction(finishLoad).
                    WithAction(callbacks);

                return operation;

            }

            /// <summary>Closes the scene.</summary>
            public SceneOperation Discard()
            {

                if (!isStillPreloaded || scene.sceneManager == null)
                    return SceneOperation<OpenSceneInfo>.done;

                var operation =
                    SceneOperation.Add(standalone, ignoreQueue: true).
                    WithAction(new FinishLoadAction()).
                    WithAction(new SceneUnloadAction(scene));

                return operation;

            }

            class FinishLoadAction : SceneAction
            {

                public override IEnumerator DoAction(SceneManagerBase _sceneManager)
                {
                    yield return callback.Invoke();
                    standalone.preloadedScene = null;
                    callback = null;
                }

            }

        }

    }

    namespace Core
    {

        /// <summary>The manager for stand-alone scenes.</summary>
        /// <remarks>Usage: <see cref="standalone"/>.</remarks>
        public class StandaloneManager : SceneManagerBase
        {

            internal void OnLoad()
            {
                if (!(preloadedScene?.scene?.isOpen ?? false))
                    preloadedScene = null;
                RegisterUnityCallback();
            }

            #region Unity scene event hooks

            readonly List<string> multipleInstanceWarnings = new List<string>();
            bool hasRegisteredCallbacks;
            void RegisterUnityCallback()
            {

                if (hasRegisteredCallbacks)
                    return;
                hasRegisteredCallbacks = true;

                sceneManager.sceneLoaded -= OnSceneLoaded;
                sceneManager.sceneUnloaded -= OnSceneUnloaded;

                sceneManager.sceneLoaded += OnSceneLoaded;
                sceneManager.sceneUnloaded += OnSceneUnloaded;

#if UNITY_EDITOR

                EditorSceneManager.sceneOpened -= OnSceneOpened;
                EditorSceneManager.sceneClosed -= OnSceneClosed;

                EditorSceneManager.sceneOpened += OnSceneOpened;
                EditorSceneManager.sceneClosed += OnSceneClosed;

                void OnSceneOpened(scene scene, OpenSceneMode openMode) =>
                    OnSceneLoaded(scene, openMode == OpenSceneMode.Single ? LoadSceneMode.Single : LoadSceneMode.Additive);

                void OnSceneClosed(scene scene) =>
                    OnSceneUnloaded(scene);

#endif

                void OnSceneLoaded(scene scene, LoadSceneMode mode)
                {

                    if (!LoadingScreenUtility.IsLoadingScreenOpen(scene) && Utility.SceneUtility.GetAllOpenUnityScenes().GroupBy(s => s.path).Any(g => g.Count() > 1))
                    {
                        if (!multipleInstanceWarnings.Contains(scene.path))
                            Debug.LogWarning("Scene is opened more than once, this is not supported and may result in first instance being tracked twice and second one not tracked at all.");
                        multipleInstanceWarnings.Add(scene.path);
                        return;
                    }

                    _ = Coroutine().StartCoroutine(description: "StandaloneManager.OnSceneLoaded callback");
                    IEnumerator Coroutine()
                    {

                        yield return new WaitForSeconds(1);

                        while (QueueUtility<SceneOperation>.isBusy)
                            yield return null;

                        if (utility.FindOpenScene(scene) is null)
                        {

                            if (string.IsNullOrEmpty(scene.path))
                                yield break;

                            var Scene = SceneManager.assets.allScenes.Find(scene.path);
                            if (!Scene)
                            {

#if UNITY_EDITOR
                                if (AssetUtility.IsIgnored(scene.path))
                                    yield break;
#endif

                                Debug.LogError("A scene was opened from outside Advanced Scene Manager, but no associated Scene scriptable object could be found.");
                                yield break;
                            }

                            if (mode == LoadSceneMode.Single)
                            {
                                collection.SetNull();
                                collection.Clear();
                                Clear();
                            }

                            _ = GetTrackedScene(scene);

                        }

                    }

                }

                void OnSceneUnloaded(scene scene)
                {

                    multipleInstanceWarnings.Remove(scene.path);

                    Coroutine().StartCoroutine(description: "StandaloneManager.OnSceneUnloaded callback");
                    IEnumerator Coroutine()
                    {

                        while (QueueUtility<SceneOperation>.isBusy)
                            yield return null;

                        if (utility.FindOpenScene(scene) is OpenSceneInfo openScene)
                        {

                            if (standalone.IsOpen(openScene.scene))
                                standalone.Remove(openScene);

                            if (collection.IsOpen(openScene.scene))
                                collection.Remove(openScene);

                        }

                    }

                }

            }

            #endregion
            #region Open as persistent

            /// <summary>Opens the scene (if not already open), and sets it as <see cref="Models.SceneCloseBehavior.KeepOpenAlways"/>.</summary>
            public SceneOperation<OpenSceneInfo> OpenPersistent(Scene scene)
            {

                if (scene.GetOpenSceneInfo() is OpenSceneInfo openScene && openScene.unityScene.HasValue)
                {
                    PersistentUtility.Set(openScene.unityScene.Value, Models.SceneCloseBehavior.KeepOpenAlways);
                    return SceneOperation<OpenSceneInfo>.FromResult(openScene);
                }

                return Open(scene).Return((o) =>
                {

                    if (scene.GetOpenSceneInfo() is OpenSceneInfo s && s.unityScene.HasValue)
                    {
                        PersistentUtility.Set(s);
                        return s;
                    }

                    return null;

                });

            }

            #endregion
            #region Preload

            /// <summary>Preloads the scene.</summary>
            /// <remarks>Use <see cref="PreloadedSceneHelper.FinishLoading"/> or <see cref="SceneManagerBase.Open(Scene)"/> to finish loading scene.</remarks> 
            public SceneOperation<PreloadedSceneHelper> Preload(Scene scene)
            {

                if (scene.GetOpenSceneInfo()?.isOpen ?? false)
                    throw new Exceptions.OpenSceneException(scene, message: "The scene cannot be preloaded since it is already open!");

                var loadAction = new SceneLoadAction(scene, isPreload: true);
                var operation =
                    SceneOperation.Add<PreloadedSceneHelper>(this).
                    WithAction(loadAction).
                    Return(o => preloadedScene);

                return operation;

            }

            //Set from SceneLoadAction.AddScene()
            /// <summary>Represents the current preloaded scene, if there is one.</summary>
            public PreloadedSceneHelper preloadedScene { get; internal set; }

            #endregion

            /// <summary>Close existing scenes and open the specified one.</summary>
            /// <remarks>This will close the current collection.</remarks>
            public SceneOperation<OpenSceneInfo> OpenSingle(Scene scene, bool closePersistent = false) =>
                SceneOperation.Add(this, @return: o => o.FindLastAction<SceneLoadAction>()?.GetTrackedScene()).
                    Close(utility.openScenes, force: closePersistent).
                    Open(scene).
                    WithCallback(Callback.BeforeLoadingScreenClose().Do(collection.SetNull));

        }

    }

}

using System;
using System.Collections;
using System.Linq;
using AdvancedSceneManager.Callbacks;
using UnityEngine;
using Scene = AdvancedSceneManager.Models.Scene;

namespace AdvancedSceneManager.Core.Actions
{

    /// <summary>Opens a scene and finds a script of the specified type, and runs a callback, scene is closed if not found.</summary>
    public class OpenAndRunCallbackAction<T> : SceneAction
    {

        public override bool reportsProgress => false;

        public OpenAndRunCallbackAction(Scene scene, Func<T, IEnumerator> runCallback, bool isLoadingScreen = false, Action onMissingCallback = null)
        {

            this.scene = scene;
            this.runCallback = runCallback;
            this.isLoadingScreen = isLoadingScreen;
            this.onMissingCallback = onMissingCallback;

            if (!scene)
                Done();

        }

        internal OpenAndRunCallbackAction(T callback)
        {

            this.callback = callback;
            if (callback == null)
                Done();

        }

        public Action onMissingCallback { get; private set; }
        public T callback { get; private set; }
        public Func<T, IEnumerator> runCallback { get; private set; }
        public bool isLoadingScreen { get; private set; }

        public override IEnumerator DoAction(SceneManagerBase _sceneManager)
        {

            if (!scene)
                yield break;

            var openAction = new SceneLoadAction(scene);
            yield return openAction.DoAction(_sceneManager);

            if (!openAction.unityScene.IsValid())
                yield break;

            var unityScene = openAction.unityScene;

            callback = unityScene.GetRootGameObjects().SelectMany(s => s.GetComponentsInChildren<MonoBehaviour>(true)).OfType<T>().FirstOrDefault();
            var i = 0;
            while (callback == null && i < 60)
            {
                callback = unityScene.GetRootGameObjects().SelectMany(s => s.GetComponentsInChildren<MonoBehaviour>(true)).OfType<T>().FirstOrDefault();
                i += 1;
            }

            if (callback != null)
            {

                if (isLoadingScreen && callback is LoadingScreen l)
                    SceneManager.utility.RaiseLoadingScreenOpening(l);

                yield return runCallback?.Invoke(callback);

                if (isLoadingScreen && callback is LoadingScreen l1)
                    SceneManager.utility.RaiseLoadingScreenOpened(l1);
                Done(unityScene);

            }
            else
            {
                onMissingCallback?.Invoke();
                yield return new SceneUnloadAction(openAction.GetTrackedScene()).DoAction(openAction.GetTrackedScene().sceneManager);
                Done();
            }

        }

    }

}

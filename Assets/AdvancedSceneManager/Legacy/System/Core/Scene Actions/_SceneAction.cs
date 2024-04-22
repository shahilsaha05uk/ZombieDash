using System;
using System.Collections;
using System.Collections.Generic;
using AdvancedSceneManager.Models;
using UnityEngine;
using scene = UnityEngine.SceneManagement.Scene;

namespace AdvancedSceneManager.Core.Actions
{

    /// <summary>Represents a <see cref="OpenSceneInfo"/> that may, or may not, be open yet, but might in the future.</summary>
    public struct LazyOpenScene
    {

        Func<OpenSceneInfo> callback;

        /// <summary>Attempts to retrieve <see cref="OpenSceneInfo"/>.</summary>
        public OpenSceneInfo ToOpenSceneInfo() =>
            callback?.Invoke();

        /// <summary>Convert to <see cref="LazyOpenScene"/>.</summary>
        public static implicit operator LazyOpenScene(Func<OpenSceneInfo> callback) => new LazyOpenScene() { callback = callback };

        /// <summary>Convert to <see cref="LazyOpenScene"/>.</summary>
        public static implicit operator LazyOpenScene(OpenSceneInfo scene) => new LazyOpenScene() { callback = () => scene };

        /// <summary>Convert to <see cref="LazyOpenScene"/>.</summary>
        public static implicit operator LazyOpenScene(SceneAction action) => new LazyOpenScene() { callback = () => action.GetTrackedScene() };

        /// <summary>Convert to <see cref="OpenSceneInfo"/>.</summary>
        public static implicit operator OpenSceneInfo(LazyOpenScene lazy) => lazy.ToOpenSceneInfo();

        /// <summary>Convert to <see cref="bool"/>.</summary>
        /// <remarks><see langword="true"/> if <see cref="OpenSceneInfo"/> could be retrieved, otherwise <see langword="false"/>.</remarks>
        public static implicit operator bool(LazyOpenScene lazy) => lazy.ToOpenSceneInfo() != null;

    }

    /// <summary>The base class of all scene actions. The scene actions perform an specific action on a <see cref="Scene"/> when contained within a <see cref="SceneOperation"/>.</summary>
    public abstract class SceneAction
    {

        /// <summary>Gets whatever this action reports progress.</summary>
        public virtual bool reportsProgress { get; } = true;

        /// <summary>The action that is performed by this <see cref="SceneAction"/>.</summary>
        public abstract IEnumerator DoAction(SceneManagerBase _sceneManager);

        /// <summary>The unity scene that was being opened or closed.</summary>
        public scene unityScene { get; set; }

        /// <summary>Gets the tracked scene, if it is open.</summary>
        public OpenSceneInfo GetTrackedScene() =>
            SceneManager.utility.FindOpenScene(unityScene);

        /// <summary>The scene this <see cref="SceneAction"/> is performing its action on.</summary>
        public Scene scene { get; protected set; }

        /// <summary>The collection that is being opened. null if stand-alone.</summary>
        public SceneCollection collection { get; protected set; }

        /// <summary>The progress of this scene action.</summary>
        public float progress { get; private set; }

        /// <summary>Is this scene action done?</summary>
        public bool isDone { get; protected set; }

        readonly List<Action> callbacks = new List<Action>();

        /// <summary>Register a callback when scene action is done.</summary>
        public void RegisterCallback(Action action) => callbacks.Add(action);

        /// <summary>Remove an registered callback when scene action is done.</summary>
        public void UnregisterCallback(Action action) => callbacks.Remove(action);

        readonly List<Action<float>> onProgress = new List<Action<float>>();

        /// <summary>Add a callback for when progress changes.</summary>
        public void OnProgressCallback(Action<float> callback)
        {
            if (!onProgress.Contains(callback))
                onProgress.Add(callback);
        }

        /// <summary>Called when progress changes.</summary>
        protected void OnProgress(float progress)
        {
            progress = Mathf.Clamp(progress, 0f, 1f);
            this.progress = progress;
            foreach (var callback in onProgress)
                callback?.Invoke(progress);
        }

        /// <summary>Called by implementation when done.</summary>
        protected virtual void Done()
        {
            isDone = true;
            OnProgress(1);
            callbacks.ForEach(a => a?.Invoke());
        }

        /// <summary>Called by implementation when done.</summary>
        protected virtual void Done(scene openedScene)
        {
            unityScene = openedScene;
            Done();
        }

        public override string ToString() =>
            GetType().Name + ": " +
            (scene ? scene.name : unityScene.name ?? "");

    }

}

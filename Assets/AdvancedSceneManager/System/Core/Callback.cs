using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lazy.Utility;
using UnityEngine;
using Scene = AdvancedSceneManager.Models.Scene;

namespace AdvancedSceneManager.Core
{

    /// <summary>The phase that a <see cref="SceneOperation"/> is currently in.</summary>
    public enum Phase
    {
        /// <summary>The scene operation is currently executing close callbacks on the scenes that are being closed, if any.</summary>
        CloseCallbacks,
        /// <summary>The scene operation is currently unloading the scenes, if any.</summary>
        UnloadScenes,
        /// <summary>The scene operation is currently loading the scenes, if any.</summary>
        LoadScenes,
        /// <summary>The scene operation is currently executing open callbacks on the scenes that are being opened, if any.</summary>
        OpenCallbacks,
        /// <summary>The scene operation is currently executing custom actions, added through <see cref="SceneOperation.WithAction(SceneAction[])"/> or similar methods, if any.</summary>
        CustomActions
    }

    static class SceneOperationCallbackUtility
    {
        public static IEnumerator Run(this IEnumerable<Callback> callbacks, SceneOperation operation, Scene scene, Phase? phase, Callback.When when)
        {
            foreach (var callback in callbacks.Where(c => c.ShouldRun(phase, scene, when)).ToArray())
                yield return callback.Run(operation);
        }
    }

    /// <summary>Represents a callback that can be run on <see cref="Phase"/> change, or right before loading screen hide (or when it would, if it was enabled).</summary>
    public class Callback
    {

        /// <summary>Specifies when to run the callback on a given <see cref="Phase"/>.</summary>
        public enum When
        {
            /// <summary>Run callback before any scene actions have started during a given <see cref="Phase"/>.</summary>
            Before,
            /// <summary>Run callback after all scene actions have run during a given <see cref="Phase"/>.</summary>
            After
        }

        /// <summary>Specifies on what phase this callback should run at.</summary>
        public Phase? phase { get; private set; }

        /// <summary>Specifies when to run the callback on a given <see cref="Phase"/>.</summary>
        public When when { get; private set; }

        /// <summary>The <see cref="Action"/> to run.</summary>
        public Action action { get; private set; }

        /// <summary>The <see cref="IEnumerator"/> coroutine to run.</summary>
        public Func<IEnumerator> enumerator { get; private set; }

        /// <summary>The <see cref="GlobalCoroutine"/> to run.</summary>
        public GlobalCoroutine coroutine { get; private set; }

        /// <summary>Specifies the scene that this callback should run on.</summary>
        /// <remarks>Specify <see langword="null"/> to run on all scenes.</remarks>
        public Scene scene { get; private set; }

        internal bool ShouldRun(Phase? phase, Scene scene, When when) =>
            this.phase == phase && this.when == when && this.scene == scene;

        internal IEnumerator Run(SceneOperation operation)
        {

            action?.Invoke();

            if (enumerator != null)
                yield return enumerator.Invoke();

            if (coroutine != null)
            {
                if (!coroutine.isRunning)
                    coroutine = coroutine.StartCoroutine();
                yield return coroutine;
            }

        }

        static IEnumerator Delay(float time)
        {
            yield return new WaitForSeconds(time);
        }

        #region Constructors

        /// <summary>Converts to <see cref="Callback"/>.</summary>
        public static implicit operator Callback(Action action) => new() { action = action, when = When.After };

        /// <summary>Converts to <see cref="Callback"/>.</summary>
        public static implicit operator Callback(Func<IEnumerator> enumerator) => new() { enumerator = enumerator, when = When.After };

        /// <summary>Converts to <see cref="Callback"/>.</summary>
        public static implicit operator Callback(GlobalCoroutine coroutine) => new() { coroutine = coroutine, when = When.After };

        private Callback()
        { }

        #region When

        /// <summary>Runs a callback after loading screen would have opened, if one was specified.</summary>
        public static Callback AfterLoadingScreenOpen() =>
            new() { when = When.Before }; //This means before operation begins, after loading screen has opened

        /// <summary>Runs a callback before loading screen would close, if one was opened.</summary>
        public static Callback BeforeLoadingScreenClose() =>
            new() { when = When.After }; //This means after operation is done, before loading screen has closed

        /// <summary>Runs a callback before the specified phase, when processing the specified scene.</summary>
        /// <remarks>Phase will still have changed to the next though, but scene actions won't run until after callback.</remarks>
        public static Callback Before(Phase phase, Scene on) =>
            new() { when = When.Before, phase = phase, scene = on };

        /// <summary>Runs a callback after the specified phase.</summary>
        public static Callback After(Phase phase, Scene on) =>
            new() { when = When.After, phase = phase, scene = on };

        /// <summary>Runs a callback before the specified phase, when processing the specified scene.</summary>
        /// <remarks>Phase will still have changed to the next though, but scene actions won't run until after callback.</remarks>
        public static Callback Before(Phase phase) =>
            Before(phase, null);

        /// <summary>Runs a callback after the specified phase.</summary>
        public static Callback After(Phase phase) =>
            After(phase, null);

        #endregion
        #region Do

        /// <summary>Performs the specified callback.</summary>
        public Callback Do(Action action) =>
            Set(() => this.action = action);

        /// <summary>Performs the specified callback.</summary>
        public Callback Do(Func<IEnumerator> enumerator) =>
            Set(() => this.enumerator = enumerator);

        /// <summary>Performs the specified callback.</summary>
        public Callback Do(GlobalCoroutine coroutine) =>
            Set(() => this.coroutine = coroutine);

        /// <summary>Performs the specified callback.</summary>
        public Callback Do(float delay) =>
            Do(() => Delay(delay));

        #endregion

        Callback Set(Action action)
        {
            action.Invoke();
            return this;
        }

        #endregion

    }

}

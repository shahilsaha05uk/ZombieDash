using System;
using System.Collections;

namespace AdvancedSceneManager.Core.Actions
{

    /// <summary>Runs a coroutine.</summary>
    public class CallbackAction : SceneAction
    {

        public override bool reportsProgress => false;

        readonly Func<IEnumerator> callback;
        readonly Action action;

        public CallbackAction(Action action) =>
            this.action = action;

        public CallbackAction(Func<IEnumerator> callback) =>
            this.callback = callback;

        public override IEnumerator DoAction(SceneManagerBase _sceneManager)
        {
            action?.Invoke();
            if (callback != null)
                yield return callback.Invoke();
        }

        public static implicit operator CallbackAction(Action action) =>
            new CallbackAction(action);

        public static implicit operator CallbackAction(Func<IEnumerator> callback) =>
            new CallbackAction(callback);

    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using AdvancedSceneManager.Utility;
using UnityEngine;

namespace AdvancedSceneManager.Editor.UI
{

    partial class SceneManagerWindow
    {

        private SaveHelper save { get; } = new();

        class SaveHelper
        {

            public bool hasUnsavedItems => callbacks.Any();

            readonly List<Action> callbacks = new();

            public void Queue(ScriptableObject obj) =>
                callbacks.Add(obj.Save);

            public void Queue(Action callback) =>
                callbacks.Add(callback);

            /// <summary>Saves all items in the queue.</summary>
            public void Now()
            {
                callbacks.ForEach(a => a?.Invoke());
                callbacks.Clear();
            }

        }

    }

}

#if UNITY_EDITOR

using System;
using UnityEngine;

namespace AdvancedSceneManager.Editor.Utility
{

    internal static class SceneManagerWindowProxy
    {

        /// <summary>Occurs when a request to save a <see cref="ScriptableObject"/> using scene manager window is made.</summary>
        public static event Action<ScriptableObject, bool> requestSave;

        /// <summary>Occurs when scene manager window actually saves, probably using ctrl+s.</summary>
        public static event Action onSave;

        /// <summary>Send a save request to scene manager window.</summary>
        public static void RequestSave(ScriptableObject obj, bool updateBuildSettings = true) =>
            requestSave?.Invoke(obj, updateBuildSettings);

        /// <summary>Called from scene manager window when saved.</summary>
        public static void NotifyOnSave() =>
            onSave?.Invoke();

    }

}

#endif

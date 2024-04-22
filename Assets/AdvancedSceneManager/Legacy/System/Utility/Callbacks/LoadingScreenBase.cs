using System;
using System.Collections;
using UnityEngine;

namespace AdvancedSceneManager.Callbacks
{

    /// <summary>A generic base class for loading screens. You probably want to inherit from <see cref="LoadingScreen"/> though.</summary>
    /// <remarks>When multiple loading screens exist within the same scene, only the first found one will be used.</remarks>
    [DisallowMultipleComponent]
    public abstract class LoadingScreenBase : MonoBehaviour
    {

        /// <summary>Occurs when loading screen is destroyed.</summary>
        public Action<LoadingScreenBase> onDestroy;
        protected virtual void OnDestroy() =>
            onDestroy?.Invoke(this);

        /// <summary>
        /// <para>The canvas that this loading screen uses.</para>
        /// <para>This will automatically register canvas with <see cref="CanvasSortOrderUtility"/>, to automatically manage canvas sort order.</para>
        /// </summary>
        /// <remarks>You probably want to set this through the inspector.</remarks>
        [Tooltip("The canvas to automatically manage sort order for, optional.")]
        public Canvas canvas;

        /// <summary>Called when the loading screen is opened.</summary>
        public abstract IEnumerator OnOpen();

        /// <summary>Called when the loading screen is about to close.</summary>
        public abstract IEnumerator OnClose();

    }

}

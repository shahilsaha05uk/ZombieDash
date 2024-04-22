#pragma warning disable CS0414

using System.Collections;
using AdvancedSceneManager.Core;
using UnityEngine;

namespace AdvancedSceneManager.Callbacks
{

    /// <summary>A class that contains callbacks for loading screens.</summary>
    /// <remarks>One instance must exist in a scene that specified as a loading screen.</remarks>
    public abstract class LoadingScreen : LoadingScreenBase
    {

        /// <summary>The current scene operation that this loading screen is associated with. May be null for the first few frames, before loading has actually begun.</summary>
        public SceneOperation operation { get; internal set; }

        /// <summary>Called when progress has changed.</summary>
        public virtual void OnProgressChanged(float progress)
        { }

        /// <inheritdoc cref="LoadingScreenBase.OnOpen"/>
        /// <remarks>Use this callback to show your loading screen, the scene manager will wait until its done.</remarks>
        public abstract override IEnumerator OnOpen();

        /// <inheritdoc cref="LoadingScreenBase.OnOpen"/>
        /// <remarks>Use this callback to hide your loading screen.</remarks>
        public abstract override IEnumerator OnClose();

        [SerializeField]
        [HideInInspector]
        private bool isLoadingScreen = true;

    }

}
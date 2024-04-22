using System;
using AdvancedSceneManager.Models;
using scene = UnityEngine.SceneManagement.Scene;

namespace AdvancedSceneManager.Exceptions
{

    /// <summary>Thrown when a scene could not be opened.</summary>
    public class OpenSceneException : Exception
    {

        /// <summary>Creates a new instance of <see cref="OpenSceneException"/></summary>
        public OpenSceneException(Scene scene, SceneCollection collection = null, string message = null)
            : base(message ?? "The scene could not be opened.")
        {
            this.scene = scene;
            this.collection = collection;
        }

        /// <summary>
        /// <para>The collection that the scene was associated with.</para>
        /// <para>This is <see langword="null"/> if scene was opened as stand-alone.</para>
        /// </summary>
        public SceneCollection collection { get; }

        /// <summary>The scene that could not be opened.</summary>
        public Scene scene { get; }

    }

    /// <summary>Thrown when a scene could not be closed.</summary>
    public class CloseSceneException : Exception
    {

        /// <summary>Creates a new instance of <see cref="CloseSceneException"/></summary>
        public CloseSceneException(Scene scene, scene unityScene, SceneCollection collection = null, string message = null)
            : base(message ?? "The scene could not be closed.")
        {
            this.scene = scene;
            this.unityScene = unityScene;
            this.collection = collection;
        }

        /// <summary>The scene that could not be closed.</summary>
        public Scene scene;

        /// <summary>The scene that could not be closed.</summary>
        public scene unityScene;

        /// <summary>
        /// <para>The collection that the scene was associated with.</para>
        /// <para>This is <see langword="null"/> if scene was opened as stand-alone.</para>
        /// </summary>
        public SceneCollection collection;

    }

}

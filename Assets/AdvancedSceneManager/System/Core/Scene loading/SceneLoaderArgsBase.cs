using System;
using AdvancedSceneManager.Models;

namespace AdvancedSceneManager.Core
{

    /// <summary>Base class for <see cref="SceneLoadArgs"/> and <see cref="SceneUnloadArgs"/>.</summary>
    public abstract class SceneLoaderArgsBase
    {

        public Scene scene { get; internal set; }
        public SceneCollection collection { get; internal set; }
        internal Action<float> updateProgress { get; set; }
        internal bool isHandled { get; set; }
        internal bool noSceneWasLoaded { get; set; }
        public bool isError { get; private set; }
        public string errorMessage { get; private set; }

        public void SetError(string message)
        {
            isError = true;
            isHandled = true;
            errorMessage = message;
        }

        public void ReportProgress(float progress) =>
            updateProgress.Invoke(progress);

        /// <summary>Gets if this scene is a loading screen.</summary>
        public bool isLoadingScreen => scene && scene.isLoadingScreen;

        /// <summary>Gets if this scene is a splash screen.</summary>
        public bool isSplashScreen => scene && scene.isSplashScreen;

    }

}

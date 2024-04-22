using System;
using System.Linq;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using scene = UnityEngine.SceneManagement.Scene;

namespace AdvancedSceneManager.Core
{

    [Serializable]
    /// <summary>A runtime class that identifies an open scene.</summary>
    public partial class OpenSceneInfo : IASMObject
    {

        private OpenSceneInfo()
        { }

        internal OpenSceneInfo(Scene scene, scene unityScene, SceneManagerBase sceneManager)
        {
            this.scene = scene;
            this.unityScene = unityScene;
            this.sceneManager = sceneManager;
        }

        #region IASMObject

#if UNITY_EDITOR
        //Not supported
        event System.ComponentModel.PropertyChangedEventHandler System.ComponentModel.INotifyPropertyChanged.PropertyChanged
        { add { } remove { } }
#endif
        void IASMObject.OnPropertyChanged()
        { }

        bool IASMObject.Match(string name)
        {
            if (scene)
                return ((IASMObject)scene).Match(name);
            else if (unityScene.HasValue)
                return unityScene.Value.name == name || unityScene.Value.path == name;
            else
                return false;
        }

        #endregion
        #region Properties

        /// <summary>The path to the scene.</summary>
        public string path => scene ? scene.path : unityScene?.path;

        /// <summary>The <see cref="Scene"/> that this <see cref="OpenSceneInfo"/> is associated with.</summary>
        public Scene scene { get; internal set; }

        /// <summary>The <see cref="UnityEngine.SceneManagement.Scene"/> that this <see cref="OpenSceneInfo"/> is associated with.</summary>
        public scene? unityScene { get; internal set; }

        /// <summary>Gets whatever this scene is preloaded.</summary>
        public bool isPreloaded => SceneManager.standalone.preloadedScene?.scene == this;

        /// <summary>Gets whatever this scene is persistent. See <see cref="PersistentUtility"/> for more details.</summary>
        public bool isPersistent => (unityScene?.IsValid() ?? false) && (PersistentUtility.GetPersistentOption(unityScene.Value) != SceneCloseBehavior.Close);

        /// <summary>Gets whatever this scene is a collection scene.</summary>
        public bool isCollection => (unityScene?.IsValid() ?? false) && (SceneManager.collection.openScenes.Contains(this));

        /// <summary>Gets whatever this scene is a standalone scene.</summary>
        public bool isStandalone => (unityScene?.IsValid() ?? false) && (SceneManager.standalone.openScenes.Contains(this));

        /// <summary>Gets whatever this scene is a special scene, i.e. splash screen / loading screen.</summary>
        public bool isSpecial => (unityScene?.IsValid() ?? false) && LoadingScreenUtility.IsLoadingScreenOpen(this);

        /// <summary>Gets whatever this scene is a untracked scene, this should never return <see langword="true"/>, that would be a bug.</summary>
        internal bool isUntracked => (unityScene?.IsValid() ?? false) && (!SceneManager.utility.openScenes.Contains(this));

        /// <summary>Gets whatever this scene is the active scene.</summary>
        public bool isActive => unityScene?.handle == UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle;

        /// <summary>Gets whatever this scene is currently open.</summary>
        public bool isOpen => unityScene?.isLoaded ?? false;

        /// <summary>The scene manager associated with this <see cref="OpenSceneInfo"/>.</summary>
        public SceneManagerBase sceneManager { get; private set; }

        #endregion
        #region Persistent

        /// <inheritdoc cref="PersistentUtility.Set(OpenSceneInfo, SceneCloseBehavior)"/>
        public void SetPersistent(SceneCloseBehavior behavior = SceneCloseBehavior.KeepOpenAlways) =>
            PersistentUtility.Set(this, behavior);

        /// <inheritdoc cref="PersistentUtility.Unset(OpenSceneInfo)"/>
        public void UnsetPersistent() =>
            PersistentUtility.Unset(this);

        #endregion

        //Called by SceneUnloadAction when scene is closed.
        internal void OnSceneClosed() =>
            unityScene = null;

        public override string ToString() =>
            scene ? scene.name : unityScene?.name ?? "Invalid scene";

    }

}

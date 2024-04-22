using System;
using AdvancedSceneManager.Core;
using UnityEngine;

namespace AdvancedSceneManager.Models.Utility
{

    /// <summary>Represents a <see cref="Scene"/> that changes depending on active <see cref="Profile"/>.</summary>
    [CreateAssetMenu(menuName = "Advanced Scene Manager/Profile dependent scene")]
    public class ProfileDependentScene : ProfileDependent<Scene>, Scene.IMethods, Scene.IMethods.IEvent
    {

        public static implicit operator Scene(ProfileDependentScene instance) =>
            instance.GetModel(out var scene) ? scene : null;

        #region IMethods

        public SceneOperation Open() => SceneManager.runtime.Open(this);
        public SceneOperation ToggleOpen(bool? openState = null) => SceneManager.runtime.ToggleOpen(this, openState);
        public SceneOperation Close() => SceneManager.runtime.Close(this);
        public SceneOperation Preload(Action onPreloaded = null) => SceneManager.runtime.Preload(this, onPreloaded);
        public SceneOperation FinishPreload() => GetModel(out var scene) && scene.isPreloaded ? SceneManager.runtime.FinishPreload(scene) : throw new InvalidOperationException("Cannot call FinishPreload() on a scene that is not preloaded.");
        public SceneOperation DiscardPreload() => SceneManager.runtime.DiscardPreload(this);
        public SceneOperation OpenWithLoadingScreen(Scene loadingScreen) => SceneManager.runtime.Open(this).With(loadingScreen);
        public void SetActive() => SceneManager.runtime.SetActive(this);

        #endregion
        #region IEvent

        public void _Open() => Open();
        public void _ToggleOpenState() => ToggleOpen();
        public void _ToggleOpen(bool? openState = null) => ToggleOpen(openState);
        public void _Close() => Close();
        public void _Preload() => Preload();
        public void _FinishPreload() => FinishPreload();
        public void _DiscardPreload() => DiscardPreload();
        public void _OpenWithLoadingScreen(Scene loadingScene) => OpenWithLoadingScreen(loadingScene);
        public void _SetActive() => SetActive();

        #endregion

    }

}

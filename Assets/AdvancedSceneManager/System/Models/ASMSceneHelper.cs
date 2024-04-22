using System;
using System.Linq;
using AdvancedSceneManager.Core;
using AdvancedSceneManager.Models.Internal;
using AdvancedSceneManager.Utility;
using UnityEngine;

namespace AdvancedSceneManager.Models
{

    /// <summary>Represents scene helper. Contains functions for opening / closing collections and scenes from <see cref="UnityEngine.Events.UnityEvent"/>.</summary>
    [AddComponentMenu("")]
    public class ASMSceneHelper : ScriptableObject,
        SceneCollection.IMethods_Target, SceneCollection.IMethods_Target.IEvent,
        Scene.IMethods_Target, Scene.IMethods_Target.IEvent
    {

        /// <inheritdoc cref="Object.name"/>
        public new string name { get; } //Prevent renaming from UnityEvent

        /// <summary>Gets a reference to scene helper.</summary>
        public static ASMSceneHelper instance => Assets.sceneHelper;

        #region SceneCollection.IMethods

        public SceneOperation Open(SceneCollection collection, bool openAll = false) => collection.Open(openAll);
        public SceneOperation OpenAdditive(SceneCollection collection, bool openAll = false) => collection.OpenAdditive(openAll);
        public SceneOperation ToggleOpen(SceneCollection collection, bool? openState = null, bool openAll = false) => collection.ToggleOpen(openState, openAll);
        public SceneOperation Close(SceneCollection collection) => collection.Close();

        #endregion
        #region SceneCollection.IEvent

        public void _Open(SceneCollection collection) => SpamCheck.EventMethods.Execute(() => collection.Open());
        public void _OpenAdditive(SceneCollection collection) => SpamCheck.EventMethods.Execute(() => collection.OpenAdditive());
        public void _ToggleOpen(SceneCollection collection) => SpamCheck.EventMethods.Execute(() => collection.ToggleOpen());
        public void _Close(SceneCollection collection) => SpamCheck.EventMethods.Execute(() => collection.Close());

        #endregion
        #region Scene.IMethods

        public SceneOperation Open(Scene scene) =>
            scene.Open();

        public SceneOperation ToggleOpenState(Scene scene) =>
            scene.ToggleOpen();

        public SceneOperation ToggleOpen(Scene scene, bool? openState = null) =>
            scene.ToggleOpen(openState);

        public SceneOperation Close(Scene scene) =>
            scene.Close();

        public SceneOperation Preload(Scene scene, Action onPreloaded = null) =>
            scene.Preload(onPreloaded);

        public SceneOperation FinishPreload(Scene scene) =>
            scene.FinishPreload();

        public SceneOperation DiscardPreload(Scene scene) =>
            scene.DiscardPreload();

        public SceneOperation OpenWithLoadingScreen(Scene scene, Scene loadingScene) =>
            scene.OpenWithLoadingScreen(loadingScene);

        public void SetActive(Scene scene) =>
            scene.SetActive();

        #endregion
        #region Scene.IEvent

        public void _Open(Scene scene) =>
            SpamCheck.EventMethods.Execute(() => Open(scene));

        public void _ToggleOpen(Scene scene) =>
            SpamCheck.EventMethods.Execute(() => ToggleOpenState(scene));

        public void _Close(Scene scene) =>
            SpamCheck.EventMethods.Execute(() => Close(scene));

        public void _Preload(Scene scene) =>
            SpamCheck.EventMethods.Execute(() => Preload(scene));

        public void _FinishPreload(Scene scene) =>
            SpamCheck.EventMethods.Execute(() => FinishPreload(scene));

        public void _DiscardPreload(Scene scene) =>
            SpamCheck.EventMethods.Execute(() => DiscardPreload(scene));

        public void _SetActive(Scene scene) =>
            SpamCheck.EventMethods.Execute(() => SetActive(scene));

        #endregion
        #region Custom

        /// <summary>Open all scenes that starts with the specified name.</summary>
        public void OpenWhereNameStartsWith(string name) =>
            SpamCheck.EventMethods.Execute(() => SceneManager.runtime.Open(SceneManager.assets.scenes.Where(s => s.name.StartsWith(name) && s.isIncluded).ToArray()));

        /// <inheritdoc cref="SceneManager.Quit(bool)"/>
        public void Quit() =>
            SceneManager.app.Quit();

        /// <inheritdoc cref="SceneManager.Startup.Restart()"/>
        public void Restart() =>
            SpamCheck.EventMethods.Execute(() => SceneManager.app.Start());

        /// <inheritdoc cref="CollectionManager.Reopen"/>
        public void RestartCollection() =>
            SpamCheck.EventMethods.Execute(() => SceneManager.openCollection.Open());

        #endregion

    }

}

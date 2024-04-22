using System;
using System.Linq;
using AdvancedSceneManager.Core;
using AdvancedSceneManager.Models;
using UnityEngine;

#if UNITY_EDITOR
#endif

namespace AdvancedSceneManager.Utility
{

    /// <summary>
    /// <para>An helper for opening and closing scenes or scene collections.</para>
    /// <para>Most common use case would be to open / close scenes or scene collections from <see cref="UnityEngine.Events.UnityEvent"/>.</para>
    /// </summary>
    /// <remarks>See also: <see cref="ASM"/>.</remarks>
    public static class SceneHelper
    {
        public static ASM current => ASM.current;
    }

    /// <summary>This is <see cref="SceneHelper"/>, but we don't want the script to show up in object picker to avoid confusion, using a different name seems to be the only way?</summary>
    [AddComponentMenu("")]
    public class ASM : ScriptableObject
    {

        /// <inheritdoc cref="Object.name"/>
        public new string name { get; } //Prevent renaming from UnityEvent

        #region Singleton

        internal static ASM current => AssetRef.instance.sceneHelper;

        #endregion
        #region Open

        /// <inheritdoc cref="Core.CollectionManager.Open(SceneCollection, bool)"/>
        public void Open(SceneCollection collection) => SpamCheck.EventMethods.Execute(() => SceneManager.collection.Open(collection));

        /// <inheritdoc cref="Core.CollectionManager.Reopen"/>
        public void ReopenCollection() => SpamCheck.EventMethods.Execute(() => SceneManager.collection.Reopen());

        /// <inheritdoc cref="SceneCollection.OpenOrReopen"/>
        public void OpenOrReopenCollection(SceneCollection collection) => SpamCheck.EventMethods.Execute(() => collection.OpenOrReopen());

        /// <inheritdoc cref="Core.CollectionManager.Open(Scene)"/>
        public void Open(Scene scene) => SpamCheck.EventMethods.Execute(() => SceneManager.standalone.Open(scene));

        /// <inheritdoc cref="Core.SceneManagerBase.Reopen(OpenSceneInfo)"/>
        public void Reopen(Scene scene) => SpamCheck.EventMethods.Execute(() => SceneManager.utility.Reopen(scene ? scene.GetOpenSceneInfo() : null));

        /// <inheritdoc cref="Core.StandaloneManager.OpenSingle(Scene)"/>
        public void OpenSingle(Scene scene, bool closePersistent) => SpamCheck.EventMethods.Execute(() => SceneManager.standalone.OpenSingle(scene, closePersistent));

        /// <inheritdoc cref="Core.SceneManagerBase.EnsureOpen(Scene)"/>
        public void EnsureOpen(Scene scene) => SpamCheck.EventMethods.Execute(() => SceneManager.standalone.EnsureOpen(scene));

        /// <inheritdoc cref="Core.SceneManagerBase.EnsureOpen(Scene)"/>
        public void EnsureOpen(SceneCollection collection) => SpamCheck.EventMethods.Execute(() => collection.EnsureOpen());

        /// <inheritdoc cref="Core.SceneManagerBase.Preload(Scene)"/>
        public void Preload(Scene scene) => SpamCheck.EventMethods.Execute(() => SceneManager.standalone.Preload(scene));

        /// <inheritdoc cref="PreloadedSceneHelper.FinishLoading"/>
        public void FinishPreload() => SpamCheck.EventMethods.Execute(() => SceneManager.standalone.preloadedScene?.FinishLoading());

        /// <inheritdoc cref="PreloadedSceneHelper.Discard"/>
        public void DiscardPreload() => SpamCheck.EventMethods.Execute(() => SceneManager.standalone.preloadedScene?.Discard());

        /// <inheritdoc cref="UtilitySceneManager.OpenOrReopen(Scene, SceneCollection)"/>
        public void OpenOrReopen(Scene scene) => SpamCheck.EventMethods.Execute(() => SceneManager.utility.OpenOrReopen(scene));

        /// <summary>Open all scenes that starts with the specified name.</summary>
        public void OpenWhereNameStartsWith(string name) =>
            SpamCheck.EventMethods.Execute(() => SceneManager.standalone.OpenMultiple(SceneManager.assets.allScenes.Where(s => s.name.StartsWith(name) && s.isIncluded).ToArray()));

        #endregion
        #region Close

        /// <inheritdoc cref="Core.CollectionManager.Close"/>
        public void CloseCollection() => SpamCheck.EventMethods.Execute(() => SceneManager.collection.Close());

        /// <inheritdoc cref="Core.UtilitySceneManager.Close(OpenSceneInfo)"/>
        public void Close(Scene scene) => SpamCheck.EventMethods.Execute(() => SceneManager.utility.Close(scene.GetOpenSceneInfo()));

        #endregion
        #region Toggle

        /// <inheritdoc cref="Core.CollectionManager.Toggle(SceneCollection, bool?)"/>
        public void Toggle(SceneCollection collection) => SpamCheck.EventMethods.Execute(() => SceneManager.collection.Toggle(collection));

        /// <inheritdoc cref="Core.CollectionManager.Toggle(SceneCollection, bool?)"/>
        public void Toggle(SceneCollection collection, bool enabled) => SpamCheck.EventMethods.Execute(() => SceneManager.collection.Toggle(collection, enabled));

        /// <inheritdoc cref="Core.SceneManagerBase.Toggle(OpenSceneInfo, bool?)"/>
        public void Toggle(Scene scene) => SpamCheck.EventMethods.Execute(() => SceneManager.utility.Toggle(scene));

        /// <inheritdoc cref="Core.SceneManagerBase.Toggle(OpenSceneInfo, bool?)"/>
        public void Toggle(Scene scene, bool enabled) => SpamCheck.EventMethods.Execute(() => SceneManager.utility.Toggle(scene, enabled));

        #endregion

        /// <inheritdoc cref="Core.CollectionManager.IsOpen(SceneCollection)"/>
        public bool IsOpen(SceneCollection collection) => SceneManager.collection.IsOpen(collection);

        /// <inheritdoc cref="Core.CollectionManager.IsOpen(SceneCollection)"/>
        public bool IsOpen(Scene scene) => scene.isOpen;

        /// <inheritdoc cref="Core.UtilitySceneManager.GetState(Scene, UnityEngine.SceneManagement.Scene?, OpenSceneInfo)(Scene)"/>
        public SceneState GetState(Scene scene) => SceneManager.utility.GetState(scene);

        /// <inheritdoc cref="Core.UtilitySceneManager.SetActive(Scene)"/>
        public void SetActiveScene(Scene scene) => SceneManager.utility.SetActive(scene);

        /// <summary>Finds the collections that are associated with this <see cref="Scene"/>.</summary>
        public (SceneCollection collection, bool asLoadingScreen)[] FindCollections(Scene scene) => scene.FindCollections();

        /// <inheritdoc cref="SceneManager.Quit(bool)"/>
        public void Quit() => SceneManager.runtime.Quit();

        /// <inheritdoc cref="SceneManager.Startup.Restart()"/>
        public void Restart() => SpamCheck.EventMethods.Execute(() => SceneManager.runtime.Restart());

        /// <inheritdoc cref="CollectionManager.Reopen"/>
        public void RestartCollection() => SpamCheck.EventMethods.Execute(() => SceneManager.collection.Reopen());

    }

}

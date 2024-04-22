using System.Collections;
using System.Linq;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using UnityEngine;

namespace AdvancedSceneManager.Core.Actions
{

    /// <summary>Opens all collections and scenes that are set to open at startup.</summary>
    public class OpenStartupCollections : SceneAction
    {

        public override bool reportsProgress => false;

        readonly SceneCollection playCollection;
        readonly bool forceOpenAllScenes;

        bool isCollectionPlayMode => playCollection && playCollection != SceneManager.collection.current;

        /// <param name="collection">Opens the specified collection after all other collections and scenes has been opened.</param>
        public OpenStartupCollections(SceneCollection playCollection = null, bool forceOpenAllScenes = false)
        {
            this.playCollection = playCollection;
            this.forceOpenAllScenes = forceOpenAllScenes;
        }

        public override IEnumerator DoAction(SceneManagerBase _sceneManager)
        {

            if (!Profile.current)
                yield break;

            yield return OpenCollections();

            //User can enter play mode directly from a collection play button in scene manager window, open it, if not already open
            if (isCollectionPlayMode)
                yield return Open(playCollection);

            if (!SceneManager.collection.current)
                Debug.LogWarning("Could not find any collection to open during startup!");

            if (!SceneManager.utility.openScenes.Any(s => !LoadingScreenUtility.IsLoadingScreenOpen(s)))
                Debug.LogWarning("No scenes has been opened during startup.");

        }

        IEnumerator OpenCollections()
        {

#if UNITY_EDITOR
            if (isCollectionPlayMode && !SceneManager.settings.local.openStartupCollectionWhenPlayingSpecificCollection)
                yield break;
#endif

            foreach (var collection in Profile.current.StartupCollections())
                yield return Open(collection);

        }

        IEnumerator Open(SceneCollection collection) =>
            SceneManager.collection.
            OpenInternal(collection, ignoreLoadingScreen: true, forceOpen: forceOpenAllScenes, ignoreQueue: true).
            WithCallback(Callback.BeforeLoadingScreenClose().Do(() => SetScenesPersistent(collection))).
            SetThreadPriority(collection, ignoreQueueCheck: true);

        void SetScenesPersistent(SceneCollection collection)
        {
            if (collection.startupOption == CollectionStartupOption.OpenAsPersistent)
                foreach (var scene in collection.scenes.Where(s => s))
                {
                    var s = scene.GetOpenSceneInfo()?.unityScene ?? default;
                    if (s != default)
                        PersistentUtility.Set(s, SceneCloseBehavior.KeepOpenAlways);
                }
        }

    }

}

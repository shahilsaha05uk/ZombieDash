#if ASM_PLUGIN_NETCODE && UNITY_2021_1_OR_NEWER

using System.Collections;
using AdvancedSceneManager.Callbacks;
using Unity.Netcode;

namespace AdvancedSceneManager.Plugin.Netcode
{

    //Loads and unloads netcode scenes

    static class SceneLoader
    {

        public static IEnumerator LoadScene(SceneLoadOverrideArgs e)
        {

            if (!e.scene.IsNetcode())
                yield break;

            if (!IsNetcodeInitialized())
                yield break;

            if (e.isSplashScreen || e.isLoadingScreen)
                yield break;

            //Logs error and calls e.NotifyComplete(handled: true)
            //if scene is not actually included in build,
            //which means we can just break then.
            //Remove this if the scene isn't supposed to be in build list, like addressable scenes
            if (!e.CheckIsIncluded())
                yield break;

            bool canContinue = false;
            NetworkManager.Singleton.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
            _ = NetworkManager.Singleton.SceneManager.LoadScene(e.scene.path, UnityEngine.SceneManagement.LoadSceneMode.Additive);

            while (!canContinue)
                yield return null;

            NetworkManager.Singleton.SceneManager.OnSceneEvent -= SceneManager_OnSceneEvent;

            //Notify that we're complete, required
            //If handled: false, then normal ASM action will run
            e.SetCompleted(e.GetOpenedScene());

            void SceneManager_OnSceneEvent(SceneEvent e1)
            {
                if (e1.SceneEventType == SceneEventType.LoadEventCompleted)
                    canContinue = true;
            }

        }

        public static IEnumerator UnloadScene(SceneUnloadOverrideArgs e)
        {

            if (!e.scene.IsNetcode())
                yield break;

            if (!IsNetcodeInitialized())
                yield break;

            if (e.isSplashScreen || e.isLoadingScreen)
                yield break;

            bool canContinue = false;
            NetworkManager.Singleton.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
            _ = NetworkManager.Singleton.SceneManager.UnloadScene(e.unityScene);

            while (!canContinue)
                yield return null;

            NetworkManager.Singleton.SceneManager.OnSceneEvent -= SceneManager_OnSceneEvent;

            //Scene is probably closed, but hierarchy might still display it,
            //so lets wait for it to update for good measure
            yield return null;

            e.SetCompleted();

            void SceneManager_OnSceneEvent(SceneEvent e1)
            {
                if (e1.SceneEventType == SceneEventType.UnloadEventCompleted)
                    canContinue = true;
            }

        }

        static bool IsNetcodeInitialized() =>
            NetworkManager.Singleton && NetworkManager.Singleton.SceneManager != null;

        static string GetFriendlyErrorMessage(SceneEventProgressStatus status) =>
            status switch
            {
                SceneEventProgressStatus.SceneNotLoaded => "Netcode: The scene could not be unloaded, since it was not loaded to begin with.",
                SceneEventProgressStatus.SceneEventInProgress => "Netcode: Only one scene can be loaded / unloaded at any given time.",
                SceneEventProgressStatus.InvalidSceneName => "Netcode: Invalid scene",
                SceneEventProgressStatus.SceneFailedVerification => "Netcode: Scene verification failed",
                SceneEventProgressStatus.InternalNetcodeError => "Netcode: Internal error",
                _ => null,
            };

    }

}

#endif
#if NETCODE && UNITY_2021_1_OR_NEWER

using System.Collections;
using System.Linq;
using AdvancedSceneManager.Core;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using Lazy.Utility;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

namespace AdvancedSceneManager.PackageSupport.Netcode
{

    class SceneLoader : Core.SceneLoader
    {

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod]
        static void Initialize() =>
        SceneManager.OnInitialized(() =>
            {
                SceneValidator.Initialize();
                SceneManager.runtime.AddSceneLoader<SceneLoader>();
                SetupNetworkManager().StartCoroutine(description: "Waiting for network manager.");
            });

        public override string sceneToggleText => "Netcode";
        public override Indicator indicator => new() { useFontAwesome = true, text = "" };

        public override bool isGlobal => false;

        public override IEnumerator LoadScene(Scene scene, SceneLoadArgs e)
        {

            yield return WaitForNetworkManager(5);
            if (!isNetworkManagerInitialized)
            {
                e.SetError("Could not load scene, netcode is not initialized.");
                yield break;
            }

            //Logs error and calls e.NotifyComplete(handled: true)
            //if scene is not actually included in build,
            //which means we can just break then.
            //Remove this if the scene isn't supposed to be in build list, like addressable scenes
            if (!e.CheckIsIncluded())
                yield break;

            bool canContinue = false;
            NetworkManager.Singleton.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
            _ = NetworkManager.Singleton.SceneManager.LoadScene(e.scene, UnityEngine.SceneManagement.LoadSceneMode.Additive);

            yield return new WaitUntil(() => canContinue);

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

        public override IEnumerator UnloadScene(Scene scene, SceneUnloadArgs e)
        {

            yield return WaitForNetworkManager(5);
            if (!isNetworkManagerInitialized)
            {
                e.SetError("Could not load scene, netcode is not initialized.");
                yield break;
            }

            bool canContinue = false;
            NetworkManager.Singleton.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
            _ = NetworkManager.Singleton.SceneManager.UnloadScene(scene);

            yield return new WaitUntil(() => canContinue);

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

        IEnumerator WaitForNetworkManager(float timeout)
        {
            var time = Time.time;
            while (!isNetworkManagerInitialized && (Time.time - time < timeout))
                yield return null;
        }

        string GetFriendlyErrorMessage(SceneEventProgressStatus status) =>
            status switch
            {
                SceneEventProgressStatus.SceneNotLoaded => "Netcode: The scene could not be unloaded, since it was not loaded to begin with.",
                SceneEventProgressStatus.SceneEventInProgress => "Netcode: Only one scene can be loaded / unloaded at any given time.",
                SceneEventProgressStatus.InvalidSceneName => "Netcode: Invalid scene",
                SceneEventProgressStatus.SceneFailedVerification => "Netcode: Scene verification failed",
                SceneEventProgressStatus.InternalNetcodeError => "Netcode: Internal error",
                _ => null,
            };

        static bool isNetworkManagerInitialized;
        static IEnumerator SetupNetworkManager()
        {

            while (!NetworkManager.Singleton || NetworkManager.Singleton.SceneManager is null)
                yield return null;
            isNetworkManagerInitialized = true;

            NetworkManager.Singleton.SceneManager.OnLoadComplete += (clientID, sceneName, mode) =>
            {
                if (NetworkManager.Singleton.LocalClientId == clientID)
                    foreach (var scene in SceneUtility.GetAllOpenUnityScenes().ToArray())
                        if (scene.ASMScene(out var s))
                        {
                            s.isSynced = true;
                            SceneManager.runtime.Track(s, scene);
                        }
            };

            NetworkManager.Singleton.SceneManager.OnUnloadComplete += (clientID, sceneName) =>
            {
                if (NetworkManager.Singleton.LocalClientId == clientID)
                    foreach (var scene in SceneManager.openScenes.ToArray())
                        if (scene && scene.isSynced && scene.internalScene.HasValue && !scene.internalScene.Value.isLoaded)
                        {
                            scene.isSynced = false;
                            SceneManager.runtime.Untrack(scene);
                        }
            };

        }

    }

}

#endif

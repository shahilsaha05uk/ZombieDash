#if ASM_PLUGIN_NETCODE && UNITY_2021_1_OR_NEWER

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lazy.Utility;
using Unity.Netcode;
using UnityEngine;

namespace AdvancedSceneManager.Plugin.Netcode
{

    static class SceneValidator
    {
        static List<Models.Scene> scenes;
        private static NetworkManager networkManager;

        /// <summary>Enables or disables ASM implementation of scene validation.</summary>
        public static bool enable
        {
            get => SceneManager.settings.project.GetCustomData("netcode.validation.enabled") == "true";
            set
            {
                SceneManager.settings.project.SetCustomData("netcode.validation.enabled", value.ToString());
                UpdateEnabled();
            }
        }

        static void UpdateEnabled()
        {
            if (enable)
            {
                networkManager.SceneManager.SetClientSynchronizationMode(UnityEngine.SceneManagement.LoadSceneMode.Additive);
                networkManager.SceneManager.VerifySceneBeforeLoading = Validate;
            }
        }

        public static void Initialize()
        {
            scenes = SceneManager.assets.scenes.Where(s => s.IsNetcode()).ToList();

            WaitForNetworkManager().StartCoroutine();
        }

        private static IEnumerator WaitForNetworkManager()
        {

            yield return new WaitUntil(() => NetworkManager.Singleton);

            networkManager = NetworkManager.Singleton;
            networkManager.OnServerStarted += OnServerStarted;
        }

        private static void OnServerStarted()
        {
            UpdateEnabled();
            networkManager.SceneManager.OnSceneEvent += OnSceneEvent;
        }

        // just me debugging
        private static void OnSceneEvent(SceneEvent sceneEvent)
        {
            //Debug.Log("---");
            //Debug.Log(sceneEvent.ClientId != NetworkManager.ServerClientId ? "Client" : "Server");
            //Debug.Log($"Index: {sceneEvent.Scene.buildIndex}, Name: {sceneEvent.Scene.name}, LoadMode: {sceneEvent.LoadSceneMode}");
        }


        // validate tells server what to sync, currently its just netcode marked scenes, perhaps make it so it can be overwritten?
        private static bool Validate(int sceneIndex, string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode)
        {
            // complained out of scope, fix.
            var scenes = SceneManager.assets.scenes.Where(s => s.IsNetcode()).ToList();
            return scenes.Any(x => x.name == sceneName);
        }
    }

}

#endif


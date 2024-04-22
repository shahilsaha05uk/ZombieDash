#if ASM_PLUGIN_ADDRESSABLES

using System.Collections;
using System.Collections.Generic;
using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Core;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace AdvancedSceneManager.Plugin.Addressables
{

    static class SceneLoadOverride
    {

        static readonly Dictionary<Scene, SceneInstance> sceneInstances = new Dictionary<Scene, SceneInstance>();

        [RuntimeInitializeOnLoadMethod]
        static void OnLoad()
        {

            RemoveAddressMappings();
            sceneInstances.Clear();
            SceneManager.utility.OverrideSceneLoad(LoadSceneAsync, UnloadSceneAsync);

        }

        static void RemoveAddressMappings()
        {
#if UNITY_EDITOR
            var path = AssetRef.path + "/AddressMappings.asset";
            _ = AssetDatabase.DeleteAsset(path);
#endif
        }

        static IEnumerator LoadSceneAsync(SceneLoadOverrideArgs e)
        {

            if (!e.scene.IsAddressable())
                yield break;

            var address = e.scene.GetAddress();
            if (string.IsNullOrWhiteSpace(address))
            {
                Debug.LogError("Could not find address for scene: " + e.scene.path);
                yield break;
            }

            var async = UnityEngine.AddressableAssets.Addressables.LoadSceneAsync(address, loadMode: UnityEngine.SceneManagement.LoadSceneMode.Additive, activateOnLoad: !e.isPreload);

            while (!async.IsDone)
            {
                yield return null;
                e.ReportProgress(async.PercentComplete);
            }

            if (async.OperationException != null)
            {
                Debug.LogError(async.OperationException);
                e.SetCompleted(default);
                yield break;
            }
            else
            {

                sceneInstances.Set(e.scene, async.Result);

                if (e.isPreload)
                    e.SetCompleted(e.GetOpenedScene(), ActivatePreloadedScene);
                else
                    e.SetCompleted(e.GetOpenedScene());

            }

            IEnumerator ActivatePreloadedScene()
            {
                yield return async.Result.ActivateAsync();
            }

        }

        static IEnumerator UnloadSceneAsync(SceneUnloadOverrideArgs e)
        {

            if (!e.scene)
                yield break;

            if (!sceneInstances.TryGetValue(e.scene, out var instance))
                yield break;
            _ = sceneInstances.Remove(e.scene);

            var async = UnityEngine.AddressableAssets.Addressables.UnloadSceneAsync(instance);
            while (!async.IsDone)
            {
                e.ReportProgress(async.PercentComplete);
                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.5f);

            e.SetCompleted();

        }

    }

}
#endif

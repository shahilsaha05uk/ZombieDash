#if ASM_PLUGIN_NETCODE && UNITY_2021_1_OR_NEWER

using UnityEngine;

namespace AdvancedSceneManager.Plugin.Netcode
{

    static class OnLoad
    {

        [RuntimeInitializeOnLoadMethod]
        static void Initialize()
        {
            SceneManager.utility.OverrideSceneLoad(SceneLoader.LoadScene, SceneLoader.UnloadScene);
            SceneValidator.Initialize();
        }

    }

}

#endif
#if ASM_PLUGIN_CROSS_SCENE_REFERENCES

using UnityEngine;

namespace AdvancedSceneManager.Plugin.Cross_Scene_References
{

    static class Initialize
    {

        [RuntimeInitializeOnLoadMethod]
        static void Runtime()
        {
            SceneOperation.Initialize();
            CrossSceneReferenceUtility.ResetAllScenes();
        }

    }

}

#endif

#if ASM_PLUGIN_NETCODE && UNITY_2021_1_OR_NEWER

using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;

namespace AdvancedSceneManager.Plugin.Netcode
{

    /// <summary>Provides extension methods for <see cref="Scene"/>.</summary>
    public static class SceneExtensions
    {

        const string key = "netcode";

        /// <summary>Gets whatever this scene should be opened by netcode.</summary>
        public static void NetcodeState(this Scene scene, out bool isEnabled) =>
            isEnabled = SceneDataUtility.Get(scene, key, false);

        /// <summary>Gets whatever this scene should be opened by netcode.</summary>
        public static bool IsNetcode(this Scene scene) =>
            SceneDataUtility.Get(scene, key, false);

#if UNITY_EDITOR
        /// <summary>Sets whatever this scene should be opened by netcode.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static void NetcodeState(this Scene scene, bool isEnabled) =>
            SceneDataUtility.Set(scene, key, isEnabled);
#endif

    }

}

#endif
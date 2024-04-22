#if ASM_PLUGIN_ADDRESSABLES

using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;

#if UNITY_EDITOR
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Plugin.Addressables.Editor;
#endif

namespace AdvancedSceneManager.Plugin.Addressables
{

    /// <summary>Provides extension methods for <see cref="Scene"/>.</summary>
    public static class SceneExtensions
    {

        internal const string key = "addressables";

        /// <summary>Gets whatever this scene should be opened by addressables.</summary>
        public static bool IsAddressable(this Scene scene)
        {

            if (!scene)
                return false;

#if UNITY_EDITOR
            if (AddressablesListener.FindEntry(scene, out var entry))
                return true;
#endif

            return !string.IsNullOrEmpty(GetAddress(scene));

        }

        /// <summary>Gets addressable address for this scene.</summary>
        internal static string GetAddress(this Scene scene)
        {

            if (!scene)
                return default;

#if UNITY_EDITOR
            if (AddressablesListener.FindEntry(scene, out var entry))
            {
                if (SceneDataUtility.Get<string>(scene, key) != entry.address)
                    SceneDataUtility.Set(scene, key, entry.address);
                return entry.address;
            }
#endif

            return SceneDataUtility.Get<string>(scene, key);

        }

#if UNITY_EDITOR

        internal static void SetAddress(this Scene scene, string address)
        {
            if (scene)
            {
                if (string.IsNullOrEmpty(address))
                {
                    SceneDataUtility.Unset(scene, key);
                    AddressablesListener.Unset(scene);
                }
                else
                {

                    //Addressable scenes cannot be added to build list and addressables has a
                    //check before creating entry if it is, so we'll need to remove it beforehand.
                    SceneDataUtility.Set(scene, key, address);
                    BuildUtility.UpdateSceneList();

                    AddressablesListener.Set(scene, address);

                }
            }
        }

        /// <summary>Sets whatever this scene should be opened by addressables.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static void IsAddressable(this Scene scene, bool isEnabled)
        {

            if (!scene)
                return;

            if (isEnabled)
                SetAddress(scene, scene.path);
            else
                SetAddress(scene, null);

        }
#endif

    }

}
#endif

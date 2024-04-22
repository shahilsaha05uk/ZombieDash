#if ASM_PLUGIN_ADDRESSABLES && UNITY_EDITOR

using AdvancedSceneManager.Core;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace AdvancedSceneManager.Plugin.Addressables.Editor
{

    [InitializeOnLoad]
    static class AddressablesListener
    {

        public static AddressableAssetSettings settings { get; private set; }

        public static bool FindEntry(Scene scene, out AddressableAssetEntry entry)
        {

            entry = null;
            if (!settings)
                return false;

            entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(scene.path));
            return entry != null;

        }

        static AddressablesListener()
        {

            AssetRef.OnInitialized(() =>
            {

                settings = AddressableAssetSettingsDefaultObject.GetSettings(true);

                Refresh();
                ListenToAddressablesChange();

            });

        }

        static void ListenToAddressablesChange()
        {

            if (settings == null)
                return;

            settings.OnModification -= OnModification;
            settings.OnModification += OnModification;

            void OnModification(AddressableAssetSettings s, AddressableAssetSettings.ModificationEvent e, object obj)
            {

                if (SceneManager.assets.allScenes.TryFind(s.AssetPath, out var scene))
                {

                    if (e == AddressableAssetSettings.ModificationEvent.EntryAdded)
                        scene.IsAddressable(true);
                    else if (e == AddressableAssetSettings.ModificationEvent.EntryRemoved)
                        scene.IsAddressable(false);

                    BuildUtility.UpdateSceneList();

                }

            }

        }

        internal static void Refresh()
        {

            foreach (var scene in SceneManager.assets.allScenes)
            {
                _ = FindEntry(scene, out var entry);
                scene.IsAddressable(entry != null);
            }

            BuildUtility.UpdateSceneList();

        }

        internal static void Set(Scene scene, string address)
        {

            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(scene.path), settings.DefaultGroup, postEvent: false);
            entry.SetLabel("ASM", true, true);
            entry.SetAddress(address);

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryCreated, null, postEvent: true, settingsModified: true);

        }

        internal static void Unset(Scene scene)
        {
            settings.RemoveAssetEntry(AssetDatabase.AssetPathToGUID(scene.path));
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryRemoved, null, postEvent: true, settingsModified: true);
        }

    }

}
#endif

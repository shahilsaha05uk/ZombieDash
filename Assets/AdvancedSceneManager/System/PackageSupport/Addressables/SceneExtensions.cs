#if ADDRESSABLES

using System.Collections.Generic;
using AdvancedSceneManager.Models;

#if UNITY_EDITOR
using static AdvancedSceneManager.PackageSupport.Addressables.Editor.AddressablesUtility;
using AdvancedSceneManager.Editor.Utility;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
#endif

namespace AdvancedSceneManager.PackageSupport.Addressables
{

    static class SceneExtensions
    {

        public static readonly List<Scene> updating = new();
        public static void OnAddressablesChanged(Scene scene)
        {

#if UNITY_EDITOR

            if (!scene)
                return;

            if (scene.isAddressable)
                AddToAddressables(scene);
            else
                RemoveFromAddressables(scene);


#endif

        }

#if UNITY_EDITOR

        /// <summary>Adds scene to addressables.</summary>
        internal static void AddToAddressables(this Scene scene)
        {

            //Addressable scenes cannot be added to build list and addressables has a
            //check before creating entry if it is, so we'll need to remove it beforehand.
            BuildUtility.UpdateSceneList();

            if (!updating.Contains(scene))
                updating.Add(scene);

            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(scene.path), settings.DefaultGroup, postEvent: false);

            entry.SetLabel("ASM", true, true);
            entry.SetLabel(scene.name, true, true);
            entry.SetAddress(scene.id);

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryCreated, entry, postEvent: true, settingsModified: true);

            updating.Remove(scene);
            BuildUtility.UpdateSceneList();

        }

        /// <summary>Removes scene from addressables.</summary>
        internal static void RemoveFromAddressables(this Scene scene)
        {

            if (!updating.Contains(scene))
                updating.Add(scene);

            _ = settings.RemoveAssetEntry(AssetDatabase.AssetPathToGUID(scene.path));

            //Addressable scenes cannot be added to build list and addressables has a
            //check before creating entry if it is, so we'll need to remove it beforehand.
            updating.Remove(scene);
            BuildUtility.UpdateSceneList();

        }

#endif

    }

}
#endif

using System.IO;
using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Core;
using AdvancedSceneManager.Editor;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace AdvancedSceneManager.Setup.Editor
{

    /// <summary>Entry point of ASM.</summary>
    internal class ASM
    {

        [InitializeOnLoadMethod]
        static void OnLoad()
        {

            //Editor coroutine package is a soft dependency for CoroutineUtility (a direct dependency of ASM, which is embedded)
            //Needed for editor functionality
            if (!File.ReadAllText("Packages/manifest.json").Contains("com.unity.editorcoroutines"))
                _ = Client.Add("com.unity.editorcoroutines@1.0");

            SceneManager.settings.local.Reload();

            AssetRef.OnInitialized(() =>
            {

                SetProfile();

                Setup.ASM.Initialize();
                InitalizeShared();

                InitializeEditor();

            });

        }

        [RuntimeInitializeOnLoadMethod]
        static void OnLoad2Runtime()
        {

            SceneManager.settings.local.Reload();

            AssetRef.OnInitialized(() =>
            {

                SetProfile();

                Setup.ASM.Initialize();
                InitalizeShared();

                InitializeEditor();

                Setup.ASM.OnLoad();

            });

        }

        static void InitalizeShared()
        {
            SceneManager.settings.project.Initialize();
        }

        static void SetProfile()
        {

            Profile profile;

            if (Application.isBatchMode) profile = Profile.buildProfile;
            else if (Profile.forceProfile) profile = Profile.forceProfile;
            else if (Profile.defaultProfile) profile = Profile.defaultProfile;
            else
                profile = SceneManager.assets.profiles.Find(SceneManager.settings.local.activeProfile);

            Profile.SetProfile(profile, updateBuildSettings: false);

        }

        static void InitializeEditor()
        {

            DefaultSceneUtility.Initialize();

            EditorManager.Initialize();

            DrawCollectionOnScenesInHierarchy.Initialize();
            PluginUtility.Initialize();
            CallbackUtility.Initialize();
            HierarchyGUIUtility.Initialize();
            PersistentUtility.Initialize();

            DynamicCollectionUtility.Initialize();
            BuildUtility.Initialize();

            AssetRefreshUtility.Initialize();
            SceneManagerWindow.Initialize();

        }

    }

}

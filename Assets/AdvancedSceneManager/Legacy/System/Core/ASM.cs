using AdvancedSceneManager.Utility;
using UnityEngine;
using AdvancedSceneManager.Core.Actions;
using AdvancedSceneManager.Core;
using System.Linq;
using AdvancedSceneManager.Models;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AdvancedSceneManager.Setup
{

    /// <summary>Entry point of ASM.</summary>
    internal static class ASM
    {

#if UNITY_EDITOR
        /// <summary>Gets if asm is set up, and intro process has been completed.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static bool isSetup
        {
            get
            {
                var profiles = AssetDatabase.FindAssets("t:Profile").
                   Select(AssetDatabase.GUIDToAssetPath).
                   Select(AssetDatabase.LoadAssetAtPath<Profile>).
                   Where(p => p);
                return profiles.Count() > 0;
            }
        }
#endif

        [RuntimeInitializeOnLoadMethod]
        internal static void Initialize()
        {

            AssetRef.OnInitialized(() =>
            {

                SceneManager.Initialize();
                UtilitySceneManager.Initialize();

                InGameToolbarUtility.Initialize();
                PauseScreenUtility.Initialize();

#if !UNITY_EDITOR
                OnLoad();
#endif
            });

        }

        //Called by SceneManager.Editor assembly when in editor
        internal static void OnLoad()
        {
            if (Application.isPlaying)
            {
                QuitAction.Reset();
                Runtime.Initialize();
            }
        }

    }

}

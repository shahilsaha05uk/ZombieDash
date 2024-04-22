using UnityEngine;
using scene = UnityEngine.SceneManagement.Scene;
using sceneManager = UnityEngine.SceneManagement.SceneManager;
using System.Linq;
using UnityEditor;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Core;

#if UNITY_EDITOR
using AdvancedSceneManager.Editor.Utility;
using UnityEditor.SceneManagement;
#endif

namespace AdvancedSceneManager.Utility
{

    /// <summary>
    /// <para>An utility class that manages the default scene, called 'AdvancedSceneManager'.</para>
    /// <para>The default scene allows us to more easily close all scenes when we need to, since unity requires at least one scene to be open at any time.</para>
    /// </summary>
    public static class DefaultSceneUtility
    {

        #region Scene actived callback

        static bool cancelNextActive;
        internal static void OnBeforeActiveSceneChanged(scene oldScene, scene newScene, out bool cancel)
        {

            //Prevents default scene from getting activated

            cancel = false;
            if (IsDefaultScene(newScene))
            {
                cancelNextActive = true;
                cancel = true;
                SceneManager.utility.SetActive(oldScene);
            }

            if (cancelNextActive)
            {
                cancel = true;
                cancelNextActive = false;
            }

        }

        #endregion
        #region HierarchyGUI

#if UNITY_EDITOR

        static bool OnGUI(scene scene)
        {

            if (IsDefaultScene(scene))
            {
                GUILayout.Label("Default scene", HierarchyGUIUtility.defaultStyle, GUILayout.ExpandWidth(false));
                return true;
            }
            return false;

        }

#endif

        #endregion

#if UNITY_EDITOR
        static bool isInitialized;
        internal static void Initialize()
        {
            AssetUtility.Ignore(StartupScenePath);
            if (isInitialized)
                return;
            isInitialized = true;
            HierarchyGUIUtility.AddSceneGUI(OnGUI, index: 10);
        }
#endif

        public const string Name = "AdvancedSceneManager";
        internal static string StartupScenePath => AssetRef.path + $"/{Name}.unity";

        internal static void EnsureOpen()
        {

            if (FindOpenScene(out var scene))
                ValidateScene(scene);
            else
            {

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    ValidateScene(EditorSceneManager.OpenScene(StartupScenePath, OpenSceneMode.Additive));
                    return;
                }
#endif

                sceneManager.sceneLoaded += SceneManager_sceneLoaded;
                sceneManager.LoadScene(StartupScenePath, UnityEngine.SceneManagement.LoadSceneMode.Additive);

                void SceneManager_sceneLoaded(scene s, UnityEngine.SceneManagement.LoadSceneMode e)
                {
                    sceneManager.sceneLoaded -= SceneManager_sceneLoaded;
                    ValidateScene(s);
                }

            }

        }

        static void ValidateScene(scene? scene)
        {

            if (scene.HasValue)
            {
                if (string.IsNullOrWhiteSpace(scene.Value.path))
                {
                    var s = scene.Value;
                    s.name = Name;
                }
                PersistentUtility.Set(scene.Value, Models.SceneCloseBehavior.KeepOpenAlways);
            }
            else
                Debug.LogError("Could not open default scene. Things may not work as expected.");

        }

        static bool FindOpenScene(out scene? scene)
        {

            scene = null;
            foreach (var s in SceneUtility.GetAllOpenUnityScenes())
                if ((s.path == "" || s.path == StartupScenePath) && (s.name == Name || s.name == "") && s.IsValid())
                    scene = s;

            return scene.HasValue;

        }

        /// <summary>Gets whatever the default scene is open.</summary>
        internal static bool isOpen =>
            FindOpenScene(out _);

        /// <summary>Gets whatever the specified scene is the default scene.</summary>
        public static bool IsDefaultScene(scene scene) =>
            scene.IsValid() && scene.name == Name && (scene.path == "" || scene.path == StartupScenePath);

#if UNITY_EDITOR

        /// <summary>Close the default scene.</summary>
        ///<remarks>Only available in editor.</remarks>
        internal static void Close()
        {

            if (SceneUtility.GetAllOpenUnityScenes().Count() == 1 && isOpen)
                return;

            if (FindOpenScene(out var scene))
                if (Application.isPlaying)
                    _ = sceneManager.UnloadSceneAsync(scene.Value);
                else
                    _ = UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene.Value, true);

        }

#endif

        internal static string GetStartupScene()
        {

#if UNITY_EDITOR
            AssetUtility.Ignore(StartupScenePath);
            EditorApplication.delayCall += () =>
            {
                if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(StartupScenePath))
                    _ = SceneUtility.Create(StartupScenePath, createSceneScriptableObject: false);
            };
#endif

            return
                Profile.current && Profile.current.startupScene
                ? Profile.current.m_startupScene
                : StartupScenePath;

        }

    }

}

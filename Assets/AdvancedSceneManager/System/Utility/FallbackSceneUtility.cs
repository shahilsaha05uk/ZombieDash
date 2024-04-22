using UnityEngine;
using scene = UnityEngine.SceneManagement.Scene;
using sceneManager = UnityEngine.SceneManagement.SceneManager;
using System.Linq;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Models.Internal;
using UnityEditor;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace AdvancedSceneManager.Utility
{

    [InitializeInEditor]
    /// <summary>An utility class that manages the default scene, called 'AdvancedSceneManager'.</summary>
    /// <remarks>The default scene allows us to more easily close all scenes when we need to, since unity requires at least one scene to be open at any time.</remarks>
    public static class FallbackSceneUtility
    {

        static FallbackSceneUtility() =>
            SceneManager.OnInitialized(() => sceneManager.activeSceneChanged += PreventFallbackSceneActivation);

        #region Active check

        static void PreventFallbackSceneActivation(scene previousScene, scene newScene)
        {

            if (!IsFallbackScene(newScene))
                return;

            if (SceneUtility.GetAllOpenUnityScenes().Count(IsValidScene) == 0)
                return;

            SceneManager.runtime.SetActive(SceneManager.openScenes.LastOrDefault());

        }

        static bool IsValidScene(scene scene) =>
            !scene.isLoaded && IsSpecialScene(scene);

        static bool IsSpecialScene(scene scene) =>
            IsFallbackScene(scene) ||
            (SceneManager.runtime.dontDestroyOnLoad && SceneManager.runtime.dontDestroyOnLoad.internalScene?.handle == scene.handle);

        #endregion
        #region Startup scene

        public const string Name = "ASM - Fallback scene";

        internal static void EnsureOpen()
        {

            if (FindOpenScene(out var scene))
                ValidateScene(scene);
            else
            {

#if UNITY_EDITOR
                if (!Application.isPlaying)
                    ValidateScene(EditorSceneManager.OpenScene(Assets.fallbackScenePath, OpenSceneMode.Additive));
                else if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Assets.fallbackScenePath))
                    sceneManager.LoadScene(Assets.fallbackScenePath, UnityEngine.SceneManagement.LoadSceneMode.Additive);
                else
                    Debug.LogError("Could not load fallback scene.");
#else

                sceneManager.sceneLoaded += SceneManager_sceneLoaded;
                sceneManager.LoadScene(Assets.fallbackScenePath, UnityEngine.SceneManagement.LoadSceneMode.Additive);

                static void SceneManager_sceneLoaded(scene s, UnityEngine.SceneManagement.LoadSceneMode e)
                {
                    sceneManager.sceneLoaded -= SceneManager_sceneLoaded;
                    ValidateScene(s);
                }

#endif

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
            }
            else
                Debug.LogError("Could not open fallback scene. Things may not work as expected.");

        }

        static bool FindOpenScene(out scene? scene)
        {

            scene = null;
            foreach (var s in SceneUtility.GetAllOpenUnityScenes())
                if (s.IsValid() && (s.path == Assets.fallbackScenePath || s.name == Name))
                    scene = s;

            return scene.HasValue;

        }

        /// <summary>Gets whatever the default scene is open.</summary>
        internal static bool isOpen =>
            FindOpenScene(out _);

        /// <summary>Gets whatever the specified scene is the default scene.</summary>
        public static bool IsFallbackScene(scene scene) =>
            scene.IsValid() && (scene.name == Name || scene.path == GetStartupScene());

#if UNITY_EDITOR

        /// <summary>Close the default scene.</summary>
        ///<remarks>Only available in editor.</remarks>
        internal static void Close()
        {

            if (SceneUtility.GetAllOpenUnityScenes().Where(s => s.isLoaded).Count() == 1 && isOpen)
                return;

            if (FindOpenScene(out var scene))
                if (Application.isPlaying)
                    _ = sceneManager.UnloadSceneAsync(scene.Value);
                else
                    _ = EditorSceneManager.CloseScene(scene.Value, true);

        }

#endif

        internal static string GetStartupScene() =>
            Profile.current && Profile.current.startupScene
            ? Profile.current.startupScene.path
            : Assets.fallbackScenePath;

        #endregion

    }

}

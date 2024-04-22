using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using AdvancedSceneManager.Utility;
using UnityEditor;
using UnityEngine;

namespace AdvancedSceneManager.Models.Helpers
{

    /// <summary>Provides access to the default ASM scenes.</summary>
    public sealed class DefaultScenes
    {

        const string relPath = "/AdvancedSceneManager/Defaults/";

        static readonly Dictionary<string, string> paths = new()
        {
            { nameof(splashScreen), "Splash Screen/ASM_SplashScreen.unity" },
            { nameof(fadeScreen), "Loading Screen/Fade/FadeLoadingScreen.unity" },
            { nameof(progressBarScreen), "Loading Screen/ProgressBar/ProgressBarLoadingScreen.unity" },
            { nameof(iconBounceScreen), "Loading Screen/IconBounce/IconBounceLoadingScreen.unity" },
            { nameof(pressAnyButtonScreen), "Loading Screen/PressAnyButton/PressAnyButtonLoadingScreen.unity" },
            { nameof(quoteScreen), "Loading Screen/Quote/QuoteLoadingScreen.unity" },
            { nameof(videoScreen), "Loading Screen/Video/VideoLoadingScreen.unity" },
            { nameof(pauseScreen), "Other/DefaultPauseScreen.unity" },
            { nameof(inGameToolbar), "Other/InGameToolbar.unity" },
        };

        string GetPath([CallerMemberName] string name = "")
        {
            if (paths.TryGetValue(name, out var path))
                return path;
            else
                Debug.LogError("Could not retrieve path for scene.");
            return null;
        }

        /// <summary>Gets the default splash screen.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        public Scene splashScreen => GetScene(GetPath());

        /// <summary>Gets the default fade loading screen.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        public Scene fadeScreen => GetScene(GetPath());

        /// <summary>Gets the default progress bar loading screen.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        public Scene progressBarScreen => GetScene(GetPath());

        /// <summary>Gets the default icon bounce loading screen.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        public Scene iconBounceScreen => GetScene(GetPath());

        /// <summary>Gets the default press any button loading screen.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        public Scene pressAnyButtonScreen => GetScene(GetPath());

        /// <summary>Gets the default quote loading screen.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        public Scene quoteScreen => GetScene(GetPath());

        /// <summary>Gets the default video loading screen.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        public Scene videoScreen => GetScene(GetPath());

        /// <summary>Gets the default pause screen.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        public Scene pauseScreen => GetScene(GetPath());

        /// <summary>Gets the default in-game-toolbar scene.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        public Scene inGameToolbar => GetScene(GetPath());

        /// <summary>Gets a default scene.</summary>
        /// <remarks>May be <see langword="null"/> if scene has been removed, or is not imported.</remarks>
        public Scene GetScene(string name)
        {
            return SceneManager.assets.scenes.FirstOrDefault(s => s && s.path.EndsWith(relPath + name));
        }

        /// <summary>Enumerates all default scenes.</summary>
        /// <param name="listNulls">Specifies whatever <see langword="null"/> will be returned for scenes that could not be found.</param>
        public IEnumerable<Scene> Enumerate(bool listNulls = false)
        {
            IEnumerable<Scene> list = new[] { splashScreen, fadeScreen, progressBarScreen, iconBounceScreen, pressAnyButtonScreen, quoteScreen, videoScreen, pauseScreen, inGameToolbar };
            if (!listNulls)
                list = list.NonNull();
            return list;
        }

#if UNITY_EDITOR

        /// <summary>Enumerates the path to all default scenes.</summary>
        /// <remarks>Only available in editor.</remarks>
        internal IEnumerable<string> EnumeratePaths()
        {

            var name = "AdvancedSceneManager.asmdef";
            var relativePath = AssetDatabase.FindAssets("t:asmdef").Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(path => path.EndsWith($"/AdvancedSceneManager/{name}"));
            relativePath = relativePath.Remove(relativePath.Length - name.Length, name.Length);
            relativePath += "Defaults/";

            return paths.Values.Select(path => relativePath + path);

        }

#endif

    }

}

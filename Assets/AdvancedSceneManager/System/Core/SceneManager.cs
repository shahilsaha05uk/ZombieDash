using System;
using System.Collections.Generic;
using AdvancedSceneManager.Core;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Models.Helpers;

namespace AdvancedSceneManager
{

    /// <summary>The central Advanced Scene Manager API. Provides access to the most important things in ASM.</summary>
    public static class SceneManager
    {

        /// <inheritdoc cref="AssetsProxy"/>
        public static AssetsProxy assets { get; } = new();

        /// <inheritdoc cref="Runtime.openScenes"/>
        public static IEnumerable<Scene> openScenes => runtime.openScenes;

        /// <inheritdoc cref="Runtime.openCollection"/>
        public static SceneCollection openCollection => runtime.openCollection;

        /// <inheritdoc cref="Runtime.preloadedScene"/>
        public static Scene preloadedScene => runtime.preloadedScene;

        /// <inheritdoc cref="Runtime"/>
        public static Runtime runtime { get; } = new();

        /// <inheritdoc cref="App"/>
        public static App app { get; } = new App();

        /// <inheritdoc cref="SettingsProxy"/>
        public static SettingsProxy settings { get; } = new();

        /// <inheritdoc cref="Profile.current"/>
        public static Profile profile => Profile.current;

        /// <summary>Gets whatever ASM is initialized. Calling ASM methods may fail if <see langword="false"/>, this is due to <see cref="ASMSettings"/> singleton not being loaded yet.</summary>
        /// <remarks>See also <see cref="OnInitialized(Action)"/>.</remarks>
        public static bool isInitialized => ASMSettings.isInitialized;

        /// <summary>Call <paramref name="action"/> when ASM has initialized.</summary>
        /// <remarks>Will call immediately if already initialized.</remarks>
        public static void OnInitialized(Action action) =>
            ASMSettings.OnInitialized(action);

    }

}

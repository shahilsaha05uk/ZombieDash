using System;
using AdvancedSceneManager.Core;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;

namespace AdvancedSceneManager
{

    /// <summary>
    /// The core of Advanced Scene Manager, provides access to the following:
    /// <list type="bullet">
    /// <item>
    ///     <term><see cref="assets"/> </term>
    ///     <description> provides an overview over all <see cref="Profile"/>, <see cref="SceneCollection"/>, and <see cref="Scene"/> in project. Note that some scenes might be missing due to blacklist settings.</description>
    /// </item>
    /// <item>
    ///     <term><see cref="runtime"/> </term>
    ///     <description> manages startup and quit processes of the game.</description>
    /// </item>
    /// <item>
    ///     <term><see cref="profile"/> </term>
    ///     <description> the current profile, which contains your collections.</description>
    /// </item>
    /// <item>
    ///     <term><see cref="settings"/> </term>
    ///     <description> settings of the scene manager that isn't stored in the profile.</description>
    /// </item>
    /// </list>
    /// 
    /// Scene managers:
    /// <list type="bullet">
    /// <item>
    ///     <term><see cref="collection"/> </term>
    ///     <description> contains functions to open or close collections or manage collection scenes.</description>
    /// </item>
    /// <item>
    ///     <term><see cref="standalone"/> </term>
    ///     <description> contains functions to manage scenes that are not associated with the currently active collection.</description>
    /// </item>
    /// <item>
    ///     <term><see cref="utility"/> </term>
    ///     <description> contains functions to manage scenes that may be open in either <see cref="standalone"/> or <see cref="collection"/>.</description>
    /// </item>
    /// <item>
    ///     <term><see cref="editor"/> </term>
    ///     <description >a simplified scene manager to manages scenes in editor. Only available in editor.</description>
    /// </item>
    /// </list>
    /// </summary>
    public static class SceneManager
    {

        #region Initialize

        internal static void Initialize()
        {

            if (collection == null)
            {
                collection = AssetRef.instance.collectionManager;
                collection.OnAfterDeserialize2();
            }

            if (standalone == null)
            {
                standalone = AssetRef.instance.standaloneManager;
                standalone.OnAfterDeserialize2();
            }

            collection.Reinitialize();
            standalone.Reinitialize();
            utility.Reinitialize();

        }

        #endregion

        /// <summary>Provides access to the scenes, collections and profiles managed by ASM.</summary>
        public static AssetUtilityRuntime.AssetsProxy assets { get; } = new AssetUtilityRuntime.AssetsProxy();

        /// <summary>Provides functions to open or close collections or manage collection scenes</summary>
        public static CollectionManager collection { get; private set; }

        /// <summary>Provides functions to manage scenes outside that are not associated with the currently active collection</summary>
        public static StandaloneManager standalone { get; private set; }

        /// <summary>Provides functions to manage scenes that may be open in either <see cref="standalone"/> or <see cref="collection"/></summary>
        public static UtilitySceneManager utility { get; } = new UtilitySceneManager();

        /// <summary>Manages startup and quit processes of the game</summary>
        public static Runtime runtime { get; } = new Runtime();

        /// <summary>ASM settings.</summary>
        public static ASMSettings.SettingsProxy settings { get; } = new ASMSettings.SettingsProxy();

        /// <summary>The currently active profile.</summary>
        public static Profile profile => Profile.current;

#if UNITY_EDITOR
        /// <summary>A simplified scene manager to manages scenes in editor.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static EditorManager editor { get; } = new EditorManager();
#endif

        internal static void OnInitialized(Action action) =>
            AssetRef.OnInitialized(action);

    }

}

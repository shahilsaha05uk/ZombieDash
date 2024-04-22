using AdvancedSceneManager.Models;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using scene = UnityEngine.SceneManagement.Scene;
using Scene = AdvancedSceneManager.Models.Scene;
using AdvancedSceneManager.Core;

#if UNITY_EDITOR
using AdvancedSceneManager.Editor.Utility;
#endif

namespace AdvancedSceneManager.Utility
{

    /// <summary>Manages persistent scenes.</summary>
    public static class PersistentUtility
    {

        #region Indicator

#if UNITY_EDITOR

        static bool isInitialized;
        internal static void Initialize()
        {
            if (isInitialized)
                return;
            isInitialized = true;
            HierarchyGUIUtility.AddSceneGUI(OnSceneGUI, index: 1);
        }

        static bool OnSceneGUI(scene scene)
        {

            if (!Application.isPlaying || !SceneManager.settings.local.displayPersistentIndicatorInHierarchy)
                return false;

            var isPersistent =
                GetPersistentOption(scene) != SceneCloseBehavior.Close
                || SceneManager.utility.dontDestroyOnLoad.unityScene.Value.handle == scene.handle
                || PersistentSceneInEditorUtility.IsPersistent(scene);

            if (isPersistent)
                GUILayout.Label("Persistent", HierarchyGUIUtility.defaultStyle, GUILayout.ExpandWidth(false));

            return true;

        }

#endif

        #endregion
        #region Set / Unset / Get

        static readonly Dictionary<scene, SceneCloseBehavior> behaviors = new Dictionary<scene, SceneCloseBehavior>();

        /// <inheritdoc cref="Set(scene, SceneCloseBehavior)"/>
        public static void Set(OpenSceneInfo scene, SceneCloseBehavior behavior = SceneCloseBehavior.KeepOpenAlways) =>
            Set(scene?.unityScene ?? default, behavior);

        /// <summary>Set <see cref="SceneCloseBehavior"/> for this scene.</summary>
        public static void Set(scene scene, SceneCloseBehavior behavior = SceneCloseBehavior.KeepOpenAlways) =>
            behaviors.Set(scene, behavior);

        /// <inheritdoc cref="Unset(scene)"/>
        public static void Unset(OpenSceneInfo scene) =>
            Unset(scene?.unityScene ?? default);

        /// <summary>Unset and revert to default <see cref="SceneCloseBehavior"/> for this scene.</summary>
        public static void Unset(scene scene) =>
            behaviors.Remove(scene);

        /// <summary>Unsets <see cref="SceneCloseBehavior"/> for all scenes.</summary>
        public static void UnsetAll() =>
            behaviors.Clear();

        /// <inheritdoc cref="GetPersistentOption(scene)"/>
        public static SceneCloseBehavior GetPersistentOption(OpenSceneInfo scene) =>
            GetPersistentOption(scene?.unityScene ?? default);

        /// <summary>Gets the persistent option that is set for this <see cref="scene"/>.</summary>
        public static SceneCloseBehavior GetPersistentOption(scene scene) =>
            behaviors.GetValue(scene);

        #endregion
        #region KeepOpen / KeepClosed

        /// <summary>Gets if the scene should stay open.</summary>
        internal static bool KeepOpen(this scene scene, params Scene[] scenesToOpen)
        {

            switch (behaviors.GetValue(scene))
            {
                case SceneCloseBehavior.Close:
                    return false;
                case SceneCloseBehavior.KeepOpenIfNextCollectionAlsoContainsScene:
                    return scenesToOpen.Any(s => s.path == scene.path);
                case SceneCloseBehavior.KeepOpenAlways:
                    return true;
                default:
                    return false;
            }

        }

        internal static bool KeepClosed(this scene scene) =>
            KeepClosed(scene.Scene().scene);

        internal static bool KeepClosed(this Scene scene) =>
            scene && SceneManager.collection.current && SceneManager.collection.current.Tag(scene).openBehavior == SceneOpenBehavior.DoNotOpenInCollection;

        #endregion

    }

}

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AdvancedSceneManager.Editor.Utility
{

    /// <summary>An utility class for managing build settings scene order.</summary>
    /// <remarks>Only available in editor.</remarks>
    public static class BuildUtility
    {

        /// <summary>Specifies a reason as to why a scene is included or excluded from build.</summary>
        public enum Reason
        {
            Default, InvalidScene, NotIncludedInProfile, IncludedInProfile, OverridenByPlugin
        }

        internal static void Initialize()
        {
            if (!Application.isPlaying)
                BuildPlayerWindow.RegisterBuildPlayerHandler(e => _ = DoBuild(e));
            EditorBuildSettings.sceneListChanged += OnBuildSettingsChanged;
        }

        #region Extensibility


        /// <summary>Contains functions for extending or overriding of functionality.</summary>
        public static class Extensibility
        {

            /// <summary>Event args for <see cref="SceneOverride"/>.</summary>
            public class CallbackEventArgs
            {

                public string scene;
                internal bool? value;

                public void SetValue(bool value) =>
                    this.value = value;

            }

            /// <summary>Occurs when updating scene list.</summary>
            public delegate void SceneOverride(CallbackEventArgs e);

            static readonly List<SceneOverride> isIncludedOverride = new List<SceneOverride>();
            static readonly List<SceneOverride> isEnabledOverride = new List<SceneOverride>();

            /// <summary>Adds an override.</summary>
            public static void Add(SceneOverride isIncluded = null, SceneOverride isEnabled = null)
            {
                if (isIncluded != null) isIncludedOverride.Add(isIncluded);
                if (isEnabled != null) isEnabledOverride.Add(isIncluded);
                UpdateSceneList();
            }

            /// <summary>Removes an override.</summary>
            public static void Remove(SceneOverride isIncluded = null, SceneOverride isEnabled = null)
            {
                if (isIncluded != null) _ = isIncludedOverride.Remove(isIncluded);
                if (isEnabled != null) _ = isEnabledOverride.Remove(isIncluded);
                UpdateSceneList();
            }

            internal static bool IsEnabled(string scene, out bool value)
            {
                var e = new CallbackEventArgs() { scene = scene };
                foreach (var callback in isEnabledOverride)
                    callback?.Invoke(e);
                value = e.value ?? false;
                return e.value.HasValue;
            }

            internal static bool IsIncluded(string scene, out bool value)
            {
                var e = new CallbackEventArgs() { scene = scene };
                foreach (var callback in isIncludedOverride)
                    callback?.Invoke(e);
                value = e.value ?? false;
                return e.value.HasValue;
            }

        }

        #region Callbacks

        static readonly List<Action> changeCallbacks = new List<Action>();
        internal static void AddBuildSettingsCallback(Action callback)
        {
            if (!changeCallbacks.Contains(callback))
                changeCallbacks.Add(callback);
        }

        internal static void RemoveBuildSettingsCallback(Action callback) =>
            changeCallbacks.Remove(callback);

        internal static void Callbacks()
        {
            foreach (var callback in changeCallbacks)
                callback?.Invoke();
        }

        #endregion

        #endregion
        #region Scene list

        #region On scene list changed

        static readonly List<object> isUpdatingBuildSettings = new List<object>();
        static void OnBuildSettingsChanged()
        {

            if (isUpdatingBuildSettings.Any())
                return;

            Callbacks();

            var modifiedScenes = ModifiedScenes(GetOrderedList().Select(s => s.buildScene), EditorBuildSettings.scenes).ToArray();
            var containsInvalidChanges = false;
            foreach (var scene in modifiedScenes)
            {

                var s = Scene.Find(scene.scene.path);
                if (!scene.isChangeAllowed)
                    containsInvalidChanges = true;
                else if (Scene.Find(scene.scene.path) is Scene s1 && Profile.current)
                    Profile.current.Set(s1, scene.scene.enabled);

            }

            if (!containsInvalidChanges)
                UpdateSceneList();

        }

        /// <summary>Gets the scenes that was modified, and as to whatever this change was allowed, since since scenes that are contained in collections are forced to be included.</summary>
        static IEnumerable<(EditorBuildSettingsScene scene, bool isChangeAllowed)>
        ModifiedScenes(IEnumerable<EditorBuildSettingsScene> oldScenes, IEnumerable<EditorBuildSettingsScene> newScenes)
        {

            if (!Profile.current)
                return Array.Empty<(EditorBuildSettingsScene, bool)>();

            var profile = Profile.current.scenes.ToArray();

            return oldScenes.
                Select(s => (oldScene: s, newScene: newScenes.FirstOrDefault(s1 => s1.path == s.path))).
                Where(s => s.newScene != null).
                Where(s => s.oldScene.enabled != s.newScene.enabled).
                Select(s =>
                {
                    //var isOverriden = IsOverriden(s.newScene.path);
                    var isInProfile = profile.Any(s1 => s1.path == s.newScene.path);
                    var isChangeAllowed = !isInProfile;
                    return (s.newScene, isChangeAllowed);
                });

        }

        #endregion

        /// <summary>Updates the scene build settings.</summary>
        public static void UpdateSceneList()
        {

            if (!Profile.current || Application.isPlaying)
                return;

            var o = new object();
            isUpdatingBuildSettings.Add(o);

            DynamicCollectionUtility.UpdateDynamicCollections(updateBuildSettings: false);

            var buildScenes = GetOrderedList().Select(s => s.buildScene).ToList();
            buildScenes.Insert(0, new EditorBuildSettingsScene(DefaultSceneUtility.GetStartupScene(), true));

            EditorBuildSettings.scenes = buildScenes.GroupBy(s => s.path).Select(g => g.First()).ToArray();

            _ = isUpdatingBuildSettings.Remove(o);

        }

        /// <summary>Get an ordered list of all scenes that would be set as scene build settings.</summary>
        public static IEnumerable<(EditorBuildSettingsScene buildScene, Reason reason)> GetOrderedList()
        {

            if (!Profile.current)
                return Array.Empty<(EditorBuildSettingsScene buildScene, Reason reason)>();

            return Profile.current.scenePaths.
                Distinct().
                OrderByDescending(s => s == Profile.current.m_splashScreen).
                ThenByDescending(s => s == Profile.current.m_loadingScreen).
                Where(path => AssetDatabase.LoadAssetAtPath<SceneAsset>(path)).
                Select(path => (path, isIncluded: IsIncluded(path, out _))).
                Where(s => s.isIncluded).
                Select(s => s.path).
                Select(path =>
                {

                    var enabled = IsEnabled(path, out var reason);
                    return (
                        buildScene: new EditorBuildSettingsScene(path, enabled),
                        reason);

                }).
                OrderByDescending(s => s.buildScene.enabled);

        }

        /// <summary>Gets the inclusion and enabled state of a scene.</summary>
        public static bool IsIncluded(string path, out Reason reason)
        {

            if (string.IsNullOrWhiteSpace(path))
            {
                reason = Reason.InvalidScene;
                return false;
            }
            else if (Extensibility.IsIncluded(path, out var value))
            {
                reason = Reason.OverridenByPlugin;
                return value;
            }
            else if (Profile.current && Profile.current.scenePaths.Contains(path))
            {
                reason = Reason.IncludedInProfile;
                return true;
            }
            else
            {
                reason = Reason.NotIncludedInProfile;
                return false;
            }

        }

        public static bool IsEnabled(string path, out Reason reason)
        {

            if (string.IsNullOrWhiteSpace(path))
            {
                reason = Reason.InvalidScene;
                return false;
            }
            else if (Extensibility.IsEnabled(path, out var value))
            {
                reason = Reason.OverridenByPlugin;
                return value;
            }

            reason = Reason.Default;
            return true;

        }

        #endregion
        #region Build

        /// <summary>Performs the pre-build stuff that ASM needs to perform before a build.</summary>
        public static void DoPreBuild()
        {

            if (Application.isBatchMode)
            {

                Debug.Log("#UCB Initializing Advanced Scene Manager:");
                Debug.Assert(SceneManager.assets.profiles.Any(), "#UCB No profiles found!");
                Debug.Assert(SceneManager.assets.allCollections.Any(), "#UCB No collections found!");
                Debug.Assert(SceneManager.assets.allScenes.Any(), "#UCB No scenes found!");

                Debug.Log("#UCB Profiles: " + string.Join(", ", SceneManager.assets.profiles.Select(p => p.name)));
                Debug.Log("#UCB Collections: " + string.Join(", ", SceneManager.assets.allCollections.Select(p => p.name)));
                Debug.Log("#UCB Scenes: " + string.Join(", ", SceneManager.assets.allScenes.Select(p => p.name)));

            }

            UpdateSceneList();

            if (Application.isBatchMode)
            {
                Debug.Assert(Profile.current && Profile.current.scenePaths.Count() > 1 && EditorBuildSettings.scenes.Length == 1, "#UCB Could not update build scenes!");
                Debug.Log("#UCB Scenes in build settings: " + string.Join(", ", EditorBuildSettings.scenes.Select(s => s.path)));
            }

            asmPreBuild?.Invoke();

        }

        /// <summary>Performs a build of your project, which is then removed once game closes.</summary>
        public static void DoTempBuild(bool attachProfiler = false, BuildOptions customOptions = BuildOptions.None)
        {
            var path = Path.Combine(Path.GetTempPath(), "Advanced Scene Manager", Application.companyName + "+" + Application.productName, "build.exe");
            _ = DoBuild(path, attachProfiler: attachProfiler, runGameWhenBuilt: true, customOptions: customOptions);
        }

        /// <summary>Performs a build of your project.</summary>
        /// <param name="path">Specifies the target path of the .exe to build.</param>
        /// <param name="attachProfiler">Specifies whatever we should attach the profiler.</param>
        /// <param name="runGameWhenBuilt">Specifies whatever to run the game after it has been built.</param>
        public static BuildReport DoBuild(string path, bool attachProfiler = false, bool runGameWhenBuilt = false, bool dev = true, BuildOptions customOptions = BuildOptions.None)
        {

            var options =
                customOptions |
                (dev ? BuildOptions.Development : BuildOptions.None) |
                (runGameWhenBuilt ? BuildOptions.AutoRunPlayer : BuildOptions.None) |
                (attachProfiler ? BuildOptions.ConnectWithProfiler : BuildOptions.None);

            return DoBuild(new BuildPlayerOptions()
            {
                locationPathName = path,
                options = options,
                target = EditorUserBuildSettings.activeBuildTarget,
                scenes = EditorBuildSettings.scenes.Select(s => s.path).ToArray()
            });

        }

        public static BuildReport DoBuild(BuildPlayerOptions options)
        {

            if (BuildPipeline.isBuildingPlayer)
                throw new Exception("Cannot start build when already building.");

            DoPreBuild();
            return BuildPipeline.BuildPlayer(options);

        }

        #endregion
        #region Build events

        public delegate void BuildError(string condition, string stacktrace, LogType type);

        /// <summary>Occurs before build, during <see cref="DoPreBuild"/>.</summary>
        /// <remarks>Only called if ASM manages build.</remarks>
        public static event Action asmPreBuild;

        /// <summary>Occurs before build.</summary>
        /// <remarks>Note that changes will not be included in build.</remarks>
        public static event Action<BuildReport> preBuild;

        /// <summary>Occurs after build.</summary>
        public static event Action<BuildReport> postBuild;

        /// <summary>Occurs when an error occurs during build.</summary>
        public static event Action onError;

        /// <summary>Occurs when an error occurs during build.</summary>
        public static event BuildError onErrorWithArgs;

        /// <summary>Occurs when an error occurs during build.</summary>
        public static event BuildError onWarningWithArgs;

        class BuildEvents : IPreprocessBuildWithReport, IPostprocessBuildWithReport
        {

            public int callbackOrder => 0;

            public void OnPreprocessBuild(BuildReport e)
            {
                Application.logMessageReceived += OnBuildError;
                preBuild?.Invoke(e);
            }

            public void OnPostprocessBuild(BuildReport e)
            {
                Application.logMessageReceived -= OnBuildError;
                postBuild?.Invoke(e);
            }

            public void OnBuildError(string condition, string stacktrace, LogType type)
            {
                if (type != LogType.Error)
                    onWarningWithArgs?.Invoke(condition, stacktrace, type);
                else
                {
                    onErrorWithArgs?.Invoke(condition, stacktrace, type);
                    onError?.Invoke();
                }
            }

        }

        #endregion

    }
}
#endif

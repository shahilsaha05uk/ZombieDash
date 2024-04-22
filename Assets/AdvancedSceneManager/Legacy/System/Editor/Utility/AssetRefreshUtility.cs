using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using AdvancedSceneManager.Core;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using Lazy.Utility;
using UnityEditor;
using UnityEngine;

namespace AdvancedSceneManager.Editor.Utility
{

    /// <summary>A class that tracks when scenes are created, removed, renamed or moved in the project and automatically updates the list in <see cref="SceneManager.assetManagement"/>.</summary>
    internal class AssetRefreshUtility : AssetPostprocessor
    {

        #region Triggers

        /// <summary>Refresh the scenes.</summary>
        public static void Refresh() => Refresh(evenIfPlaying: false, immediate: false);

        /// <summary>Refresh the scenes.</summary>
        public static void Refresh(bool immediate) => Refresh(evenIfPlaying: false, immediate: immediate);

        /// <summary>Refresh the scenes.</summary>
        public static void Refresh(bool evenIfPlaying = false, bool immediate = false)
        {

            var currentScenes = SceneManager.assets.allScenes.Where(s => s).ToArray();
            var added = AssetDatabase.FindAssets("t:" + nameof(SceneAsset)).Select(AssetDatabase.GUIDToAssetPath);
            currentScenes = currentScenes.Where(s => s).ToArray();

            var removed = currentScenes.Where(scene => !added.Contains(scene.path)).Select(s => s.path);
            var moved = currentScenes.Select(s => (from: s.path, to: AssetDatabase.GUIDToAssetPath(s.assetID))).Where(s => s.from != s.to);

            Refresh(added.Where(s => !BlacklistUtility.IsBlocked(s)).ToArray(), removed.ToArray(), moved.ToArray(), evenIfPlaying, immediate);

        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromPath)
        {

            importedAssets = importedAssets.Where(p => !BlacklistUtility.IsBlocked(p)).ToArray();
            deletedAssets = deletedAssets.Where(p => !BlacklistUtility.IsBlocked(p)).ToArray();

            AdjustForTriggers(ref importedAssets, ref deletedAssets, ref movedAssets, ref movedFromPath);
            RemoveNonScenes(ref importedAssets);
            RemoveNonScenes(ref deletedAssets);
            RemoveNonScenes(ref movedAssets);
            RemoveNonScenes(ref movedFromPath);

            if (importedAssets.Any() || deletedAssets.Any() || movedAssets.Any() || movedFromPath.Any())
                Refresh(importedAssets.ToArray(), deletedAssets.ToArray(), movedAssets.Select((path, i) => (movedFromPath[i], movedAssets[i])).ToArray(), immediate: true);
            else if (deletedAssets.Any())
                Refresh(immediate: true);

            OnAssetsPostprocess?.Invoke();

        }

        static void AdjustForTriggers(ref string[] importedAssets, ref string[] deletedAssets, ref string[] movedAssets, ref string[] movedFromPath)
        {

            if (!SceneManager.settings.local.assetRefreshTriggers.HasFlag(ASMSettings.Local.AssetRefreshTrigger.SceneCreated))
                importedAssets = Array.Empty<string>();

            if (!SceneManager.settings.local.assetRefreshTriggers.HasFlag(ASMSettings.Local.AssetRefreshTrigger.SceneRemoved))
                deletedAssets = Array.Empty<string>();

            if (!SceneManager.settings.local.assetRefreshTriggers.HasFlag(ASMSettings.Local.AssetRefreshTrigger.SceneMoved))
                movedAssets = movedFromPath = Array.Empty<string>();

        }

        static void RemoveNonScenes(ref string[] paths) =>
             paths = paths.Where(IsScene).ToArray();

        static bool IsScene(string path) =>
            Path.GetExtension(path) == ".unity";

        /// <summary>Stops current and all queued asset refreshes.</summary>
        public static void Stop() =>
            QueueUtility<AssetRefresh>.StopAll();

        #endregion

        class AssetRefresh : CustomYieldInstruction, IQueueable
        {

            bool isDone;
            public override bool keepWaiting => !isDone;

            public bool CanQueue() => true;

            AssetRefresh()
            { }

            /// <summary>Finds all <see cref="SceneAsset"/> in project and processes them all.</summary>
            public static AssetRefresh FullRefresh() =>
                new AssetRefresh(
                    added: AssetDatabase.FindAssets("t:SceneAsset").Select(AssetDatabase.GUIDToAssetPath).Where(s => !BlacklistUtility.IsBlocked(s)),
                    removed: AssetDatabase.FindAssets("t:SceneAsset").Select(AssetDatabase.GUIDToAssetPath).Where(s => BlacklistUtility.IsBlocked(s)),
                    moved: Array.Empty<(string, string)>(),
                    doFullRefresh: true);

            public AssetRefresh(IEnumerable<string> added, IEnumerable<string> removed, IEnumerable<(string from, string to)> moved, bool doFullRefresh = false)
            {

                if (!Profile.current)
                    return;

                this.added = added.
                    Where(s => !AssetUtility.ignore.Contains(s)).
                    Where(s => !BlacklistUtility.IsBlocked(s)).
                    Where(s => !moved.Any(s1 => s1.to == s)).
                    Distinct().
                    ToArray();

                this.removed = removed.Distinct().ToArray();
                this.moved = moved.Distinct().ToArray();

                if (doFullRefresh)
                {
                    this.added = added.ToArray();
                    this.removed = removed.ToArray();
                    this.moved = moved.ToArray();
                }

                //Filter out scenes generated by test runners
                this.added = this.added.Where(s => !Path.GetFileNameWithoutExtension(s).StartsWith("InitTestScene")).ToArray();
                this.removed = this.removed.Where(s => !Path.GetFileNameWithoutExtension(s).StartsWith("InitTestScene")).ToArray();

                actions = new (RefreshAction action, RefreshActionCount progressCount)[]
                {
                    (AssignAssetIds,                        () => 0),
                    (MakeSureAssetsAddedToAssetManagement,  () => 0),
                    (RefreshDeletedFiles,                   () => this.removed.Length),
                    (RefreshAddedFiles,                     () => this.added.Length),
                    (RefreshAddressables,                   () => 0),
                    (UpdateLabels,                          () => this.added.Length),
                    (MakeSureSceneHelperExists,             () => 0),
                    (Blacklist,                             () => 0),
                    (RemoveStartupSceneFromAssetRef,        () => 0),
                    (SetLoadingScreenFlags,                 () => 0),
                };

#if !UNITY_2019
                Progress("Waiting for turn...");
#endif

            }


            public string[] added;
            public string[] removed;
            public (string from, string to)[] moved;
            readonly (RefreshAction action, RefreshActionCount progressCount)[] actions;

            GlobalCoroutine coroutine;
            public void OnTurn(Action onComplete)
            {

                //RefreshMoved has to run before regular actions
                _ = RefreshMoved().StartCoroutine(onComplete: Start);

                void Start() =>
                    coroutine = DoRefresh().StartCoroutine(
                        description: "Asset refresh",
                        onComplete: () =>
                        {
                            AssetDatabaseUtility.AllowAutoRefresh(this);
                            onComplete.Invoke();
                            OnCancel();
                            isDone = true;
                        });

            }

            public void OnCancel()
            {
                ClearProgress();
                coroutine?.Stop();
                coroutine = null;
            }

            IEnumerator DoRefresh()
            {

                AssetUtility.isRefreshing = true;

#if UNITY_2019
                maxProgress = actions.Sum(action => action.progressCount.Invoke());
#endif

                EditorApplication.update -= UpdateProgress;
                EditorApplication.update += UpdateProgress;

                AssetUtility.Cleanup();

                AssetDatabaseUtility.DisallowAutoRefresh(this);
                for (var i = 0; i < actions.Length; i++)
                {
                    progressString = "Refreshing...";
                    yield return actions[i].action.Invoke();
                }

                AssetUtility.Cleanup();

                AssetDatabaseUtility.AllowAutoRefresh(this);

                BuildUtility.UpdateSceneList();

#if UNITY_2019
                AssetDatabase.SaveAssets();
#else
                foreach (var asset in AssetRef.instance.allAssets.ToArray())
                    AssetDatabase.SaveAssetIfDirty(asset);
#endif

                RefreshCallback?.Invoke(added, removed, moved);

                EditorApplication.update -= UpdateProgress;
                ClearProgress();

                CoroutineUtility.Run(ClearProgress, after: 0.1f);
                AssetUtility.isRefreshing = false;
                SceneManagerWindow.Reload();

            }

            #region Progress

            int? progressID;
            string progressString;

#if UNITY_2019
            int progress;
            int maxProgress;
            float GetProgress() =>
                progress / (float)maxProgress;
#endif
            void Progress(string message = "", bool increment = true)
            {

#if UNITY_2019
                if (increment)
                    progress += 1;
#endif
                progressString = message;

                EditorApplication.update -= UpdateProgress;
                EditorApplication.update += UpdateProgress;

            }

            void UpdateProgress()
            {
#if UNITY_2019
                EditorUtility.DisplayProgressBar("Advanced Scene Manager: Refreshing assets...", progressString, GetProgress());
#else
                progressID ??= UnityEditor.Progress.Start("Refreshing assets...", options: UnityEditor.Progress.Options.Indefinite);
                UnityEditor.Progress.Report(progressID.Value, 0, progressString);
#endif
            }

            void ClearProgress()
            {
#if UNITY_2019
                EditorUtility.ClearProgressBar();
#else
                if (progressID.HasValue)
                    UnityEditor.Progress.Remove(progressID.Value);
                progressID = null;
#endif
                EditorApplication.update -= UpdateProgress;
            }

            #endregion
            #region Actions

            #region Assign assetID

            IEnumerator AssignAssetIds()
            {

                foreach (var scene in SceneManager.assets.allScenes)
                {
                    var id = AssetDatabase.AssetPathToGUID(scene.path);
                    if (scene.assetID != id)
                    {
                        scene.assetID = id;
                        scene.Save();
                    }
                }

                yield break;

            }

            #endregion
            #region Moved files

            IEnumerator RefreshMoved()
            {
                foreach (var (from, to) in moved)
                {

                    Progress($"Updating path: " + from + " -> " + to);

                    if (string.IsNullOrWhiteSpace(to))
                    {

                        var s = Scene.Find(from);

                        if (s)
                            AssetRef.instance.Remove(s);

                        continue;

                    }

                    foreach (var profile in SceneManager.assets.profiles)
                    {
                        if (profile.m_loadingScreen == from)
                        {
                            profile.m_loadingScreen = to;
                            profile.MarkAsDirty();
                        }
                        if (profile.m_splashScreen == from)
                        {
                            profile.m_splashScreen = to;
                            profile.MarkAsDirty();
                        }
                    }

                    foreach (var collection in SceneManager.assets.allCollections)
                    {

                        for (int i = 0; i < collection.Count; i++)
                            if (collection.m_scenes[i] == from)
                            {
                                collection.m_scenes[i] = to;
                                collection.MarkAsDirty();
                            }

                        if (collection.m_loadingScreen == from)
                        {
                            collection.m_loadingScreen = to;
                            collection.MarkAsDirty();
                        }

                        if (collection.m_activeScene == from)
                        {
                            collection.m_activeScene = to;
                            collection.MarkAsDirty();
                        }

                    }

                    var scene = Scene.Find(from);
                    if (!scene)
                        continue;

                    scene.UpdateAsset(path: to);
                    EditorUtility.SetDirty(scene);
                    AssetUtility.Rename(scene, Path.GetFileNameWithoutExtension(to));

                    yield return null;

                }

            }

            #endregion
            #region Addressables

            IEnumerator RefreshAddressables()
            {
#if ASM_PLUGIN_ADDRESSABLES
                Plugin.Addressables.Editor.AddressablesListener.Refresh();
#endif
                yield break;
            }

            #endregion
            #region Added and deleted files

            IEnumerator RefreshDeletedFiles()
            {

                foreach (var path in removed.ToArray())
                {

                    Progress("Deleting: " + path);

                    var scene = SceneManager.assets.allScenes.Find(path);

                    if (scene)
                        AssetUtility.Remove(scene);

                    yield return null;

                }

                foreach (var scene in SceneManager.assets.allScenes.Where(s => !AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path)))
                {
                    Progress("Deleting: " + scene.path);
                    if (scene)
                        AssetUtility.Remove(scene);
                    yield return null;

                }

            }

            IEnumerator RefreshAddedFiles()
            {
                foreach (var path in added.ToArray())
                {

                    if (BlacklistUtility.IsBlocked(path))
                        continue;

                    if (AssetUtility.IsIgnored(path))
                        continue;

                    Progress("Adding: " + path);
                    _ = SceneUtility.Create(path, createSceneScriptableObject: true);

                    yield return null;

                }
            }

            #endregion
            #region Update labels

            IEnumerator UpdateLabels()
            {
                yield break;
                //foreach (var scene in added.Select(s => Scene.Find(s)).Where(s => s).ToArray())
                //{
                //    Progress("Updating labels: " + scene.name, increment: false);
                //    SetLabels(scene, scene);
                //}

                //yield return null;

                //void SetLabels(Object obj, Scene scene)
                //{

                //    if (!File.Exists(scene.path))
                //        return;

                //    var oldLabels = AssetDatabase.GetLabels(obj);
                //    var labels = oldLabels.Where(l => !l.StartsWith("ASM")).ToArray();
                //    var newLabels = scene.FindCollections(true).
                //        Where(c => c.collection).
                //        GroupBy(c => c.collection).
                //        Select(c => c.First()).
                //        Select(c => c.collection.label).
                //        Where(l => l != "ASM:").
                //        Distinct().
                //        ToArray();

                //    var file = File.ReadAllText(scene.path);
                //    if (file.Contains("isSplashScreen: 1"))
                //        ArrayUtility.Add(ref newLabels, "ASM:SplashScreen");
                //    else if (file.Contains("isLoadingScreen: 1"))
                //        ArrayUtility.Add(ref newLabels, "ASM:LoadingScreen");

                //    if (!newLabels.Any())
                //        return;

                //    ArrayUtility.AddRange(ref labels, newLabels);

                //    if (!oldLabels.SequenceEqual(labels))
                //        AssetDatabase.SetLabels(obj, labels);

                //}

            }

            #endregion
            #region Make sure scene helper exists

            IEnumerator MakeSureSceneHelperExists()
            {
                _ = AdvancedSceneManager.Utility.ASM.current;
                yield return null;
            }

            #endregion
            #region Make sure assets are added to asset management

            IEnumerator MakeSureAssetsAddedToAssetManagement()
            {

                foreach (var profile in AssetDatabase.FindAssets("t:Profile").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Profile>))
                    AssetRef.instance.Add(profile);

                foreach (var collection in AssetDatabase.FindAssets("t:SceneCollection").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<SceneCollection>))
                    AssetRef.instance.Add(collection);

                var files = AssetDatabase.FindAssets("t:Scene").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Scene>).ToArray();
                foreach (var scene in AssetDatabase.FindAssets("t:Scene").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Scene>))
                    AssetRef.instance.Add(scene);

                AssetRef.instance.Cleanup();

                yield return null;

            }

            #endregion
            #region Blacklist

            IEnumerator Blacklist()
            {

                //Remove scenes that are now blocked
                foreach (var (from, to) in moved.Where(s => BlacklistUtility.IsBlocked(s.to)))
                {
                    AssetUtility.Remove(Scene.Find(from));
                    AssetUtility.Remove(Scene.Find(to));
                }

                //Add scenes that are now unblocked
                foreach (var scene in moved.Where(s => !BlacklistUtility.IsBlocked(s.to)).Select(s => AssetDatabase.LoadAssetAtPath<SceneAsset>(s.to)))
                    if (scene && !string.IsNullOrWhiteSpace(scene.name))
                        _ = AssetUtility.Add(scene, ignoreBlacklist: true);

                if (!Profile.current)
                    yield break;

                //Remove assets that are blocked
                if (Profile.current.blacklist.isWhitelist)
                {

                    var scenes = AssetDatabase.
                        FindAssets("t:SceneAsset", Profile.current.blacklist.paths.ToArray()).
                        Select(AssetDatabase.GUIDToAssetPath).
                        ToArray();

                    scenes = AssetDatabase.
                        FindAssets("t:SceneAsset").
                        Select(AssetDatabase.GUIDToAssetPath).
                        Except(scenes).
                        Where(BlacklistUtility.IsBlocked).
                        ToArray();

                    foreach (var scene in scenes)
                        AssetUtility.Remove(Scene.Find(scene));

                }
                else
                {

                    var assets = AssetRef.instance.scenes.Where(s => s && BlacklistUtility.IsBlocked(s.path)).ToArray();
                    foreach (var asset in assets)
                    {
                        AssetRef.instance.Remove(asset);
                        _ = AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(asset));
                    }

                }

                yield return null;

            }

            #endregion

            IEnumerator SetLoadingScreenFlags()
            {

                foreach (var scene in SceneManager.assets.allScenes)
                {

                    if (!File.Exists(scene.path))
                        continue;

                    var file = File.ReadAllText(scene.path);
                    var isSplashScreen = file.Contains("isSplashScreen: 1");
                    var isLoadingScreen = file.Contains("isLoadingScreen: 1") && !isSplashScreen;

                    if (scene.isSplashScreen != isSplashScreen || scene.isLoadingScreen != isLoadingScreen)
                    {
                        scene.isSplashScreen = isSplashScreen;
                        scene.isLoadingScreen = isLoadingScreen;
                        scene.MarkAsDirty();
                    }

                }

                yield return null;

            }

            IEnumerator RemoveStartupSceneFromAssetRef()
            {

                if (AssetRef.instance && AssetRef.instance.scenes?.FirstOrDefault(s => s && s.path == DefaultSceneUtility.StartupScenePath) is Scene scene && scene)
                    AssetRef.instance.Remove(scene);

                yield return null;

            }

            #endregion

        }

        static SynchronizationContext syncContext;

        internal static void Initialize()
        {

            syncContext = SynchronizationContext.Current;

            AssetUtility.onAssetsCleared += Refresh;
            AssetUtility.OnRefreshRequest += ((bool full, bool immediate) e) =>
            {

                if (e.full)
                    _ = DoFullRefresh();
                else
                    Refresh(immediate: e.immediate);

            };
            EditorApplication.playModeStateChanged += _ =>
            {
                QueueUtility<AssetRefresh>.StopAll();
                EditorUtility.ClearProgressBar();
            };

            if (!Application.isPlaying)
                EditorApplication.wantsToQuit += () => { AssetUtility.allowAssetRefresh = false; return true; };

        }

        internal delegate IEnumerator RefreshAction();
        delegate int RefreshActionCount();

        internal delegate void OnRefresh(string[] added, string[] deleted, (string from, string to)[] moved);
        internal static event OnRefresh RefreshCallback;

        internal static event Action OnAssetsPostprocess;

        static GlobalCoroutine timer;

        static readonly List<string> addedQueue = new List<string>();
        static readonly List<string> removedQueue = new List<string>();
        static readonly List<(string from, string to)> movedQueue = new List<(string from, string to)>();

        /// <inheritdoc cref="AssetRefresh.FullRefresh"/>
        public static IEnumerator DoFullRefresh()
        {

            if (!Profile.current)
                yield break;

            var refresh = AssetRefresh.FullRefresh();
            refresh.OnTurn(() => { });
            yield return refresh;

        }

        static void Refresh(string[] added, string[] deleted, (string from, string to)[] moved, bool evenIfPlaying = false, bool immediate = false)
        {

            if (!Profile.current)
                return;

            if (!AssetUtility.allowAssetRefresh)
                return;

            if (!evenIfPlaying)
                if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                    return;

            addedQueue.AddRange(added);
            removedQueue.AddRange(deleted);
            movedQueue.AddRange(moved);

            if (timer?.isComplete ?? true)
                StartTimer(immediate);
            else
            {
                timer.Stop();
                timer = null;
            }

        }

        static void StartTimer(bool immediate = false)
        {
            if (addedQueue.Any() || removedQueue.Any() || movedQueue.Any())
                timer = Timer(immediate).StartCoroutine(() => StartTimer(), description: "Asset Refresh");
            else
                timer = null;
        }

        static IEnumerator Timer(bool immediate = false)
        {

            yield return immediate ? null : new WaitForSeconds(1);

            var refresh = new AssetRefresh(addedQueue, removedQueue, movedQueue);
            _ = QueueUtility<AssetRefresh>.Queue(refresh);
            addedQueue.Clear();
            removedQueue.Clear();
            movedQueue.Clear();

        }

    }

}

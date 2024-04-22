#pragma warning disable CS0649 // Field is not assigned to

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

using Component = UnityEngine.Component;
using AdvancedSceneManager.Utility;
using AdvancedSceneManager.Core;

using unityScene = UnityEngine.SceneManagement.Scene;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AdvancedSceneManager.Models
{

    /// <summary>Specifies that state of a scene.</summary>
    public enum SceneState
    {
        /// <summary>The state of the scene is unknown. (An issue probably occured while checking state)</summary>
        Unknown,
        /// <summary>The scene is not open.</summary>
        NotOpen,
        /// <summary>The scene is in queue to be opened.</summary>
        Queued,
        /// <summary>The scene is currently being opened. Mutually exclusive to <see cref="Preloading"/>.</summary>
        Opening,
        /// <summary>The scene is currently being preloaded. Mutually exclusive to <see cref="Opening"/>.</summary>
        Preloading,
        /// <summary>The scene is currently preloaded.</summary>
        Preloaded,
        /// <summary>The scene is open.</summary>
        Open
    }

    /// <summary>A <see cref="Scene"/> is a <see cref="ScriptableObject"/> that represents a scene in Unity, and are automatically generated or updated when a scene is added, renamed, moved or removed.</summary>
    /// <remarks>The advantage of doing it this way is that we can actually create variables in scripts that refers to a scene rather than an arbitrary int or string. This also allows us to open scenes directly from an <see cref="UnityEngine.Events.UnityEvent"/> without a specific scene load script.</remarks>
    public partial class Scene : ScriptableObject, IASMObject
    {

        #region IASMObject

        /// <inheritdoc cref="Object.name"/>
        /// <remarks>See also: <see cref="AdvancedSceneManager.Editor.Utility.AssetUtility.Rename{T}(T, string)"/>.</remarks>
        public new string name =>
            this ? base.name : "(null)";

#if UNITY_EDITOR
        public event PropertyChangedEventHandler PropertyChanged;
#endif

        internal void OnPropertyChanged([CallerMemberName] string name = "")
        {
#if UNITY_EDITOR
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            MarkAsDirty();
#endif
        }

        public void OnPropertyChanged() =>
            OnPropertyChanged("");

        /// <summary>Mark scriptable object as dirty after modifying.</summary>
        /// <remarks>No effect in build.</remarks>
        public void MarkAsDirty()
        {
#if UNITY_EDITOR
            if (this && AssetDatabase.LoadAssetAtPath<Profile>(AssetDatabase.GetAssetPath(this)) is Object o)
                EditorUtility.SetDirty(o);
#endif
        }

        bool IASMObject.Match(string name) =>
            this.name == name || path == name || assetID == name;

        #endregion
        #region Fields

        [SerializeField] private string m_path;
        [SerializeField] private string m_assetID;
        [SerializeField] private bool m_isLoadingScreen;
        [SerializeField] private bool m_isSplashScreen;

        #endregion
        #region Properties

        /// <summary>Gets if this scene is a loading screen.</summary>
        /// <remarks>
        /// <para>Automatically updated.</para>
        /// <para>If this is <see langword="false"/> for an actual loading screen, please make sure scene contains a <see cref="AdvancedSceneManager.Callbacks.LoadingScreen"/> script.</para>
        /// <para>Scene might sometimes have to be resaved for this flag to appear.</para>
        /// </remarks>
        public bool isLoadingScreen
        {
            get => m_isLoadingScreen;
            internal set => m_isLoadingScreen = value;
        }

        /// <summary>Gets if this scene is a splash screen.</summary>
        /// <remarks>
        /// <para>Automatically updated.</para>
        /// <para>If this is <see langword="false"/> for an actual splash screen screen, please make sure scene contains a <see cref="AdvancedSceneManager.Callbacks.SplashScreen"/> script.</para>
        /// <para>Scene might sometimes have to be resaved for this flag to appear.</para>
        /// </remarks>
        public bool isSplashScreen
        {
            get => m_isSplashScreen;
            internal set => m_isSplashScreen = value;
        }

        /// <summary>The path to the scene file, relative to the project folder.</summary>
        /// <remarks>Automatically updated.</remarks>
        public string path
        {
            get => m_path;
            internal set { m_path = value; OnPropertyChanged(); }
        }

        ///<summary>The id of the asset in the asset database.</summary>
        /// <remarks>Automatically updated.</remarks>
        public string assetID
        {
            get => m_assetID;
            internal set { m_assetID = value; OnPropertyChanged(); }
        }

        /// <summary>Gets whatever this scene is included in build.</summary>
        public bool isIncluded => SceneUtility.IsIncluded(this);

        /// <summary>Gets if this scene is currently active.</summary>
        public bool isActive =>
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().path == path;

        /// <summary>Gets whatever the scene is open in the hierarchy, this is <see langword="true"/> if scene is currently loading, if scene is preloaded, if scene is fully open..</summary>
        public bool isOpenInHierarchy =>
            SceneUtility.GetAllOpenUnityScenes().Any(s => s.path == path);

        /// <summary>Gets whatever the scene is open and tracked by ASM.</summary>
        public bool isTracked =>
            GetOpenSceneInfo() != null;

        /// <inheritdoc cref="UtilitySceneManager.GetState(Scene, unityScene?, OpenSceneInfo)"/>
        public SceneState state => SceneManager.utility.GetState(this);

        /// <summary>Gets whatever the scene is open.</summary>
        public bool isOpen => state == SceneState.Open;

        /// <summary>Gets whatever the scene is preloaded.</summary>
        public bool isPreloaded => state == SceneState.Preloaded;

        #endregion
        #region Methods

        #region SceneOperation

        /// <inheritdoc cref="SceneManagerBase.Open"/>
        public SceneOperation<OpenSceneInfo> Open() => SceneManager.standalone.Open(this);

        /// <inheritdoc cref="StandaloneManager.OpenSingle"/>
        public SceneOperation<OpenSceneInfo> OpenSingle(bool closePersistent = false) => SceneManager.standalone.OpenSingle(this, closePersistent);

        /// <inheritdoc cref="UtilitySceneManager.Reopen"/>
        public SceneOperation<OpenSceneInfo> Reopen() => SceneManager.utility.Reopen(GetOpenSceneInfo());

        /// <inheritdoc cref="UtilitySceneManager.Toggle"/>
        public SceneOperation Toggle() => SceneManager.utility.Toggle(this);

        /// <inheritdoc cref="UtilitySceneManager.Toggle"/>
        public SceneOperation Toggle(bool enabled) => SceneManager.utility.Toggle(this, enabled);

        /// <summary>Ensures collection is open. Does not reopen and does not throw error if collection already is open.</summary>
        public SceneOperation EnsureOpen() => Toggle(true);

        /// <inheritdoc cref="UtilitySceneManager.Close"/>
        public SceneOperation Close() => SceneManager.utility.Close(GetOpenSceneInfo());

        /// <inheritdoc cref="StandaloneManager.Preload(Scene, bool)"/>
        public SceneOperation<PreloadedSceneHelper> Preload() => SceneManager.standalone.Preload(this);

        /// <inheritdoc cref="StandaloneManager.OpenPersistent(Scene)"/>
        public SceneOperation<OpenSceneInfo> OpenPersistent() => SceneManager.standalone.OpenPersistent(this);

        #endregion
        #region UnityEvent

        /// <inheritdoc cref="SceneManagerBase.Open"/>
        public void OpenEvent() => SpamCheck.EventMethods.Execute(() => Open());

        /// <inheritdoc cref="StandaloneManager.OpenSingle"/>
        public void OpenSingleEvent() => SpamCheck.EventMethods.Execute(() => OpenSingle());

        /// <inheritdoc cref="SceneManagerBase.Reopen"/>
        public void ReopenEvent() => SpamCheck.EventMethods.Execute(() => Reopen());

        /// <inheritdoc cref="SceneManagerBase.Toggle"/>
        public void ToggleEvent() => SpamCheck.EventMethods.Execute(() => Toggle());

        /// <inheritdoc cref="SceneManagerBase.Toggle"/>
        public void ToggleEvent(bool enabled) => SpamCheck.EventMethods.Execute(() => Toggle(enabled));

        /// <inheritdoc cref="SceneManagerBase.Close"/>
        public void CloseEvent() => SpamCheck.EventMethods.Execute(() => Close());
        public void OpenWithLoadingScreenEvent(Scene loadingScene) => SpamCheck.EventMethods.Execute(() => Open().WithLoadingScreen(loadingScene));

        #endregion
        #region Find

        /// <summary>Finds which collections that this scene is a part of.</summary>
        public (SceneCollection collection, bool asLoadingScreen)[] FindCollections(bool allProfiles = false) =>
            allProfiles
            ? FindCollections(null)
            : FindCollections(Profile.current);

        /// <summary>Finds which collections that this scene is a part of.</summary>
        public (SceneCollection collection, bool asLoadingScreen)[] FindCollections(Profile profile) =>
            (profile ? profile.collections.ToArray() : SceneManager.assets.allCollections).
            Where(c => c && c.scenes != null && c.scenes.Contains(this)).Select(c => (c, LoadingScreenUtility.FindLoadingScreen(c) == this)).ToArray();

        /// <summary>Gets the root game objects in this <see cref="Scene"/>, only works if scene is loaded.</summary>
        public IEnumerable<GameObject> GetRootGameObjects() =>
            GetOpenSceneInfo()?.unityScene?.GetRootGameObjects() ?? Array.Empty<GameObject>();

        /// <summary>Finds the object in the heirarchy of this <see cref="Scene"/>.</summary>
        /// <remarks>Only works if scene is loaded.</remarks>
        public T FindObject<T>() where T : Component =>
            FindObjects<T>().FirstOrDefault();

        /// <summary>Finds the objects in the heirarchy of this <see cref="Scene"/>.</summary>
        /// <remarks>Only works if scene is loaded.</remarks>
        public IEnumerable<T> FindObjects<T>() where T : Component =>
            GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<T>()).OfType<T>();

        /// <summary>Finds the scene with the specified name.</summary>
        public static Scene Find(string nameOrPath, SceneCollection inCollection = null, Profile inProfile = null) =>
            SceneUtility.Find(nameOrPath, inCollection, inProfile).FirstOrDefault();

        /// <summary>Finds the scenes with the specified name.</summary>
        public static IEnumerable<Scene> FindAll(string nameOrPath, SceneCollection inCollection = null, Profile inProfile = null) =>
            SceneUtility.Find(nameOrPath, inCollection, inProfile);

        #endregion

        /// <inheritdoc cref="UtilitySceneManager.SetActive"/>
        public void SetActiveScene() => SceneManager.utility.SetActive(this);

        /// <inheritdoc cref="UtilitySceneManager.FindOpenScene(Scene)"/>
        public OpenSceneInfo GetOpenSceneInfo() => SceneManager.utility.FindOpenScene(this);

        //Called when scene is renamed or moved
        internal void UpdateAsset(string assetID = null, string path = null)
        {
            if (assetID != null)
                m_assetID = assetID;
            if (path != null)
                m_path = path;
        }

        #endregion

    }

}

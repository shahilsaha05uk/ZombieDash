using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using AdvancedSceneManager.Core;
using AdvancedSceneManager.Models.Enums;
using UnityEngine;
using AdvancedSceneManager.Utility;

#if INPUTSYSTEM
using UnityEngine.InputSystem.Utilities;
#endif

#if UNITY_EDITOR
using UnityEditor;
using AdvancedSceneManager.Editor.Utility;
#endif

namespace AdvancedSceneManager.Models
{

    /// <summary>Represents a collection of scenes.</summary>
    /// <remarks>Only one collection can be open at a time.</remarks>
    public class SceneCollection : ASMModel,
        ISceneCollection,
        ISceneCollection.IEditable, ISceneCollection.IOpenable,
        SceneCollection.IMethods, SceneCollection.IMethods.IEvent,
        ILockable
    {

        #region Startup

        void UpdateStartup()
        {

            if (FindProfile(out var profile))
                foreach (var collection in profile.collections.Cast<ISceneCollection>())
                    collection.OnPropertyChanged(nameof(isStartupCollection));

        }

        #endregion
        #region ISceneCollection

        public int count =>
            m_scenes.Count;

        public Scene this[int index] =>
            m_scenes.ElementAtOrDefault(index);

        public string title =>
            m_title;

        [HideInInspector]
        public string description
        {
            get => m_description;
            set => m_description = value;
        }

        public IEnumerable<string> scenePaths =>
            m_scenes?.Select(s => s.path) ?? Enumerable.Empty<string>();

        public IEnumerable<Scene> scenes =>
            m_scenes ?? Enumerable.Empty<Scene>();

        public IEnumerator<Scene> GetEnumerator() =>
            m_scenes?.GetEnumerator() ?? Enumerable.Empty<Scene>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();

        #endregion
        #region ISceneCollection.IEditable

        List<Scene> ISceneCollection.IEditable.sceneList => m_scenes;

        #endregion
        #region Name / title

#if UNITY_EDITOR

        /// <summary>Gets if name should be prefixed with <see cref="m_prefix"/>.</summary>
        protected virtual bool UsePrefix { get; } = true;

        internal void SetTitleAfterCreation(string prefix, string title)
        {
            m_title = title;
            ((ScriptableObject)this).name = $"{prefix}{title}";
        }

        internal void SetTitle(string title)
        {

            if (string.IsNullOrWhiteSpace(title))
                return;

            m_title = title;

            var prefix = UsePrefix ? m_prefix : "";
            var name = $"{prefix}{title}";

            while (name.EndsWith(".asset"))
                name = name.Remove(name.Length - ".asset".Length);

            Rename(name);

        }

        internal void SetPrefix(string prefix)
        {

            if (m_prefix == prefix)
                return;

            m_prefix = prefix;
            SetTitle(title);

        }

#endif

        #endregion
        #region Fields

#if UNITY_EDITOR

        protected override void OnValidate()
        {

            EditorApplication.delayCall += () =>
            {
                SetTitle(m_title);
                BuildUtility.UpdateSceneList();
            };

            base.OnValidate();

        }

#endif

        //Core variables
        [SerializeField] internal string m_title = "New Collection";
        [SerializeField] protected string m_description;
        [SerializeField] private List<Scene> m_scenes = new();
        [SerializeField] internal string m_prefix;

        //Extra scenes
        [SerializeField] private LoadingScreenUsage m_loadingScreenUsage = LoadingScreenUsage.UseDefault;
        [SerializeField] private Scene m_loadingScreen;
        [SerializeField] private Scene m_activeScene;

        //Collection open options
        [SerializeField] private bool m_unloadUnusedAssets = true;
        [SerializeField] private bool m_openAsPersistent = false;
        [SerializeField] private bool m_openAsDisabled = false;

        //Other
        [SerializeField] private ScriptableObject m_extraData;
        [SerializeField] private CollectionStartupOption m_startupOption = CollectionStartupOption.Auto;
        [SerializeField] private CollectionLoadingThreadPriority m_loadingPriority = CollectionLoadingThreadPriority.Auto;
        [SerializeField] private bool m_isIncluded = true;
        [SerializeField] private bool m_isLocked;
        [SerializeField] private string m_lockMessage;


        [SerializeField] private List<Scene> m_scenesThatShouldNotAutomaticallyOpen = new();

        [SerializeField] private InputBinding m_binding = new();

        #endregion
        #region Properties

        public override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {

            base.OnPropertyChanged(propertyName);

            if (propertyName == nameof(startupOption))
                UpdateStartup();

        }

        /// <summary>Gets both <see cref="scenes"/> and <see cref="loadingScreen"/>.</summary>
        /// <remarks><see langword="null"/> is filtered out.</remarks>
        public IEnumerable<Scene> allScenes =>
            m_scenes.
            Concat(new[] { loadingScreen }).
            Where(s => s);

        /// <summary>Gets if this collection has any scenes.</summary>
        public bool hasScenes => m_scenes.Where(s => s).Any();

        /// <summary>Gets if this is a startup collection.</summary>
        /// <remarks>Only available in editor.</remarks>
        public bool isStartupCollection => isIncluded && FindProfile(out var profile) && profile.IsStartupCollection(this);

        /// <summary>The extra data that is associated with this collection.</summary>
        /// <remarks>Use <see cref="UserData{T}"/> to cast it to the desired type.</remarks>
        public ScriptableObject userData
        {
            get => m_extraData;
            set { m_extraData = value; OnPropertyChanged(); }
        }

        /// <summary>Gets whatever this collection should be included in build.</summary>
        public bool isIncluded
        {
            get => SceneManager.settings.project.allowExcludingCollectionsFromBuild ? m_isIncluded : true;
            set { m_isIncluded = value; OnPropertyChanged(); }
        }

        /// <summary>The loading screen that is associated with this collection.</summary>
        public Scene loadingScreen
        {
            get => m_loadingScreen;
            set { m_loadingScreen = value; OnPropertyChanged(); }
        }

        /// <summary>Gets effective loading screen depending on <see cref="loadingScreenUsage"/>.</summary>
        public Scene effectiveLoadingScreen
        {
            get
            {
                if (loadingScreenUsage == LoadingScreenUsage.Override)
                    return loadingScreen;
                else if (loadingScreenUsage == LoadingScreenUsage.UseDefault)
                    return Profile.current.loadingScreen;
                else
                    return null;
            }
        }

        /// <summary>Specifies what loading screen to use.</summary>
        public LoadingScreenUsage loadingScreenUsage
        {
            get => m_loadingScreenUsage;
            set { m_loadingScreenUsage = value; OnPropertyChanged(); }
        }

        /// <summary>Specifies the scene that should be activated after collection is opened.</summary>
        public Scene activeScene
        {
            get => m_activeScene;
            set { m_activeScene = value; OnPropertyChanged(); }
        }

        /// <summary>Specifies startup option.</summary>
        public CollectionStartupOption startupOption
        {
            get => m_startupOption;
            set { m_startupOption = value; OnPropertyChanged(); }
        }

        /// <summary>Specifies the thread priority to use when opening this collection.</summary>
        public CollectionLoadingThreadPriority loadingPriority
        {
            get => m_loadingPriority;
            set { m_loadingPriority = value; OnPropertyChanged(); }
        }

        /// <summary>Specifies whatever this collection should be opened as persistent.</summary>
        public bool openAsPersistent
        {
            get => m_openAsPersistent;
            set { m_openAsPersistent = value; OnPropertyChanged(); }
        }

        /// <summary>Specifies whatever this collection should be opened as disabled.</summary>
        public bool openAsDisabled
        {
            get => m_openAsDisabled;
            set { m_openAsDisabled = value; OnPropertyChanged(); }
        }

        /// <summary>Calls <see cref="Resources.UnloadUnusedAssets"/> after collection is opened or closed.</summary>
        public bool unloadUnusedAssets
        {
            get => m_unloadUnusedAssets;
            set { m_unloadUnusedAssets = value; OnPropertyChanged(); }
        }

        /// <summary>Specifies scenes that should not open automatically.</summary>
        public List<Scene> scenesThatShouldNotAutomaticallyOpen =>
            m_scenesThatShouldNotAutomaticallyOpen;

        public IEnumerable<Scene> scenesToAutomaticallyOpen =>
            scenes.NonNull().Except(scenesThatShouldNotAutomaticallyOpen);

        /// <summary>Specifies bindings for this scene.</summary>
        public InputBinding binding => m_binding;

        /// <summary>Gets if this collection is locked.</summary>
        public bool isLocked
        {
            get => m_isLocked;
            set { m_isLocked = value; OnPropertyChanged(); }
        }

        /// <summary>Gets the lock message for this collection.</summary>
        public string lockMessage
        {
            get => m_lockMessage;
            set { m_lockMessage = value; OnPropertyChanged(); }
        }

        #endregion
        #region Runtime

        /// <summary>Gets if this collection is open.</summary>
        public bool isOpen => isOpenNonAdditive || isOpenAdditive;

        /// <summary>Gets if this collection is opened additively.</summary>
        public bool isOpenAdditive => SceneManager.runtime.openAdditiveCollections.Contains(this);

        /// <summary>Gets if this collection is opened additively.</summary>
        public bool isOpenNonAdditive => SceneManager.runtime.openCollection == this;

        #endregion
        #region Find

        /// <summary>Gets 't:AdvancedSceneManager.Models.SceneCollection', the string to use in <see cref="AssetDatabase.FindAssets(string)"/>.</summary>
        public readonly static string AssetSearchString = "t:" + typeof(SceneCollection).FullName;

        /// <summary>Gets if <paramref name="q"/> matches <see cref="ASMModel.name"/>.</summary>
        public override bool IsMatch(string q) =>
            base.IsMatch(q) || title == q;

        /// <summary>Finds a collection based on its title or id.</summary>
        public static SceneCollection Find(string q, bool activeProfile = true) =>
            activeProfile
            ? Profile.current.collections.FirstOrDefault(c => c && c.IsMatch(q))
            : SceneManager.assets.collections.Find(q);

        /// <summary>Finds a collection based on its title or id.</summary>
        public static bool TryFind(string q, out SceneCollection collection, bool activeProfile = true) =>
          collection =
            (activeProfile
            ? Profile.current.collections.FirstOrDefault(c => c && c.IsMatch(q))
            : SceneManager.assets.collections.Find(q));

        #endregion
        #region Methods

        #region Interfaces

        /// <summary>Defines a set of methods that is meant to be shared between: <see cref="SceneCollection"/>, <see cref="ASMSceneHelper"/>, and <see cref="SceneManager.runtime"/>.</summary>
        interface IXmlDocsHelper
        { }

        /// <inheritdoc cref="IXmlDocsHelper"/>
        /// <remarks>Specified methods to be used programmatically, on the collection itself.</remarks>
        public interface IMethods
        {

            /// <summary>Opens this collection.</summary>
            /// <param name="openAll">Specifies whatever scenes flagged to not open with collection, should.</param>
            /// <remarks>Reopens all non-persistent scenes.</remarks>
            public SceneOperation Open(bool openAll = false);

            /// <summary>Opens this collection as additive.</summary>
            /// <param name="openAll">Specifies whatever scenes flagged to not open with collection, should.</param>
            /// <remarks>Additive collections are not "opened", all scenes within are merely opened like normal scenes. Mostly intended for convenience.</remarks>
            public SceneOperation OpenAdditive(bool openAll = false);

            /// <summary>Toggles this collection open or closed.</summary>
            /// <param name="openState">Specifies whatever you have a preferred state to toggle to, this means collection will not be closed if <see langword="true"/> is passed. This can be used to ensure collection is open, without having an explicit check beforehand. The inverse is also the case for <see langword="false"/>.</param>
            /// <param name="openAll">Specifies whatever scenes flagged to not open with collection, should.</param>
            public SceneOperation ToggleOpen(bool? openState = null, bool openAll = false);

            /// <summary>Closes this collection.</summary>
            /// <remarks>No effect if collection is already closed. Note that "additive collections" are not actually opened, only its scenes are.</remarks>
            public SceneOperation Close();

            /// <inheritdoc cref="IXmlDocsHelper"/>
            /// <remarks>Specifies methods to be used in UnityEvent, using the collection itself.</remarks>
            public interface IEvent
            {

                /// <inheritdoc cref="Open(bool)"/>
                public void _Open(bool openAll = false);

                /// <inheritdoc cref="OpenAdditive(bool)"/>
                public void _OpenAdditive(bool openAll = false);

                /// <inheritdoc cref="ToggleOpen(bool?, bool)"/>
                public void _ToggleOpen(bool? openState = null);

                /// <inheritdoc cref="Close"/>
                public void _Close();

                //Not matched

                /// <inheritdoc cref="ToggleOpen(bool?, bool)"/>
                public void _ToggleOpenState();

            }

        }

        /// <inheritdoc cref="IXmlDocsHelper"/>
        /// <remarks>Specifies methods to be used programmatically, using collection as first parameter.</remarks>
        public interface IMethods_Target
        {

            /// <inheritdoc cref="IMethods.Open(bool)"/>
            public SceneOperation Open(SceneCollection collection, bool openAll = false);

            /// <inheritdoc cref="IMethods.OpenAdditive(bool)"/>
            public SceneOperation OpenAdditive(SceneCollection collection, bool openAll = false);

            /// <inheritdoc cref="IMethods.ToggleOpen(bool?, bool)"/>
            public SceneOperation ToggleOpen(SceneCollection collection, bool? openState = null, bool openAll = false);

            /// <inheritdoc cref="IMethods.Close"/>
            public SceneOperation Close(SceneCollection collection);

            /// <inheritdoc cref="IXmlDocsHelper"/>
            /// <remarks>Specifies methods to be used in UnityEvent, when not using collection itself.</remarks>
            public interface IEvent
            {

                /// <inheritdoc cref="IMethods.Open(bool)"/>
                public void _Open(SceneCollection collection);

                /// <inheritdoc cref="IMethods.OpenAdditive(bool)"/>
                public void _OpenAdditive(SceneCollection collection);

                /// <inheritdoc cref="IMethods.ToggleOpen(bool?, bool)"/>
                public void _ToggleOpen(SceneCollection collection);

                /// <inheritdoc cref="IMethods.Close"/>
                public void _Close(SceneCollection collection);

            }

        }

        #endregion
        #region IMethods

        public SceneOperation Open(bool openAll = false) => SceneManager.runtime.Open(this, openAll);
        public SceneOperation OpenAdditive(bool openAll = false) => SceneManager.runtime.OpenAdditive(this, openAll);
        public SceneOperation ToggleOpen(bool? openState = null, bool openAll = false) => SceneManager.runtime.ToggleOpen(this, openState, openAll);
        public SceneOperation Close() => SceneManager.runtime.Close(this);

        #endregion
        #region IMethods.IEvent

        public void _Open(bool openAll = false) => SpamCheck.EventMethods.Execute(() => Open());
        public void _OpenAdditive(bool openAll = false) => SpamCheck.EventMethods.Execute(() => OpenAdditive(openAll));
        public void _ToggleOpenState() => SpamCheck.EventMethods.Execute(() => ToggleOpen());
        public void _ToggleOpen(bool? openState = null) => SpamCheck.EventMethods.Execute(() => ToggleOpen(openState));
        public void _Close() => SpamCheck.EventMethods.Execute(() => Close());

        #endregion

        /// <summary>Find the <see cref="Profile"/> that this collection is associated with.</summary>
        public bool FindProfile(out Profile profile) =>
            profile = FindProfile();

        /// <summary>Find the <see cref="Profile"/> that this collection is associated with.</summary>
        public Profile FindProfile() =>
            SceneManager.assets.profiles.FirstOrDefault(p => p && p.Contains(this, true));

        /// <summary>Casts and returns <see cref="userData"/> as the specified type. Returns null if invalid type.</summary>
        public T UserData<T>() where T : ScriptableObject =>
            (T)userData;

        internal bool IsOpen(Scene scene) =>
            SceneManager.runtime.openScenes.Contains(scene);

        /// <summary>Gets if this collection contains <paramref name="scene"/>.</summary>
        public bool Contains(Scene scene) =>
            scenes.Contains(scene);

        /// <summary>Gets or sets whatever the scene should automatically open, when this collection is open. Default is <see langword="true"/>.</summary>
        public bool AutomaticallyOpenScene(Scene scene, bool? value = null)
        {

            if (value.HasValue)
            {

                scenesThatShouldNotAutomaticallyOpen.Remove(scene);

                if (value.Value)
                    scenesThatShouldNotAutomaticallyOpen.Add(scene);

                Save();

                OnPropertyChanged(nameof(scenesThatShouldNotAutomaticallyOpen));

            }

            return scenesThatShouldNotAutomaticallyOpen.Contains(scene);

        }

        #endregion

        public override string ToString(int indent) =>
            $"{base.ToString(indent)}Collection: {title}\n{string.Join("\n", scenes.NonNull().Select(s => s.ToString(indent + 1)))}\n";

    }

}

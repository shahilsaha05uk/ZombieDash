using UnityEngine;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using Object = UnityEngine.Object;
using AdvancedSceneManager.Core;
using System.Collections.Generic;
using System.Collections;
using AdvancedSceneManager.Utility;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AdvancedSceneManager.Models
{

    /// <summary>Specifies what loading screen to use, if any.</summary>
    public enum LoadingScreenUsage
    {
        /// <summary>Specifies no loading screen.</summary>
        DoNotUse,
        /// <summary>Specifies default loading screen, defined in profile settings.</summary>
        UseDefault,
        /// <summary>Specifies overriden loading screen, defined in <see cref="SceneCollection"/>.</summary>
        Override
    }

    /// <summary>Specifies what to do with a <see cref="SceneCollection"/> during startup.</summary>
    public enum CollectionStartupOption
    {
        /// <summary>Specifies that ASM should automatically decide if a <see cref="SceneCollection"/> should be opened during startup.</summary>
        /// <remarks>This means that if no collection in the list specifies either <see cref="Open"/> or <see cref="OpenAsPersistent"/>, then the first collection in the list that has <see cref="Auto"/> will be opened.</remarks>
        Auto,
        /// <summary>Specifies that a <see cref="SceneCollection"/> will open during startup.</summary>
        Open,
        /// <summary>Specifies that a <see cref="SceneCollection"/> will open and set all scenes within as persistent during startup.</summary>
        OpenAsPersistent,
        /// <summary>Specifies that a <see cref="SceneCollection"/> will not open during startup.</summary>
        DoNotOpen,
    }

    /// <summary>
    /// <para>Wrapper for <see cref="ThreadPriority"/>, adds <see cref="CollectionThreadPriority.Auto"/>.</para>
    /// <see cref="ThreadPriority"/>: <inheritdoc cref="ThreadPriority"/>
    /// </summary>
    public enum CollectionThreadPriority
    {

        /// <summary>Automatically decide <see cref="ThreadPriority"/> based on if loading screen is open.</summary>
        Auto = -2,

        /// <summary>Lowest thread priority.</summary>
        Low = ThreadPriority.Low,

        /// <summary>Below normal thread priority.</summary>
        BelowNormal = ThreadPriority.BelowNormal,

        /// <summary>Normal thread priority.</summary>
        Normal = ThreadPriority.Normal,

        /// <summary>Highest thread priority.</summary>
        High = ThreadPriority.High,

    }

    /// <summary>A <see cref="SceneCollection"/> contains a list of <see cref="Scene"/>, all of which are opened when the <see cref="SceneCollection"/> is opened (except for scenes tagged DoNotOpen).</summary>
    /// <remarks>Only one <see cref="SceneCollection"/> can be open at a time.</remarks>
    public class SceneCollection : ScriptableObject, IReadOnlyList<Scene>, IASMObject
#if UNITY_EDITOR
        , INotifyPropertyChanged
#endif
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
            title == name;

        #endregion
        #region IList<Scene>

        /// <summary>Gets the number of scenes in this <see cref="SceneCollection"/>.</summary>
        public int Count => scenes.Length;

        /// <summary>Gets the scene at the specified index.</summary>
        public Scene this[int index] => ((IList<Scene>)scenes)[index];

        /// <summary>Gets if the specified scene exists in this <see cref="SceneCollection"/>.</summary>
        public bool Contains(Scene item) => scenes.Contains(item);

        /// <summary>Gets an enumerator for the scenes contained in this <see cref="SceneCollection"/>.</summary>
        public IEnumerator<Scene> GetEnumerator() => ((IEnumerable<Scene>)scenes).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => scenes.GetEnumerator();

        #endregion
        #region Fields

#if UNITY_EDITOR
        //UI
        internal bool isDynamic;
#endif

        [SerializeField] private bool hasSetDefaultActiveScene;
        [SerializeField] private string m_description;
        [SerializeField] internal string[] m_scenes = Array.Empty<string>();
        [SerializeField] private LoadingScreenUsage m_loadingScreenUsage = LoadingScreenUsage.UseDefault;
        [SerializeField] internal string m_loadingScreen;
        [SerializeField] internal string m_activeScene;
        [SerializeField] private CollectionStartupOption m_startupOption = CollectionStartupOption.Auto;
        [SerializeField] private CollectionThreadPriority m_loadingPriority = CollectionThreadPriority.Auto;
        [SerializeField] private ScriptableObject m_extraData;
        [SerializeField] private bool m_isIncluded = true;
        [SerializeField] private bool m_unloadUnusedAssets = true;
        [SerializeField] private TagList m_tags = new TagList();

        #endregion
        #region Properties

        [SerializeField] internal string m_title = "New Collection";

        /// <summary>The title of this collection.</summary>
        /// <remarks>See also: <see cref="AdvancedSceneManager.Editor.Utility.AssetUtility.Rename{T}(T, string)"/>.</remarks>
        public string title => m_title;

        /// <summary>The description of this collection.</summary>
        public string description
        {
            get => m_description;
            set => m_description = value;
        }

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

        /// <summary>Gets the scenes in this collection, note that some might be null if no reference is added in scene manager window.</summary>
        public Scene[] scenes
        {
            get => m_scenes.Select(s => Scene.Find(s)).ToArray();
            set => m_scenes = value?.Select(s => s ? s.path : "")?.ToArray() ?? Array.Empty<string>();
        }

        /// <summary>The loading screen that is associated with this collection.</summary>
        public Scene loadingScreen
        {
            get => Scene.Find(m_loadingScreen);
            set { m_loadingScreen = value ? value.path : ""; OnPropertyChanged(); }
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
            get => Scene.Find(m_activeScene);
            set { m_activeScene = value ? value.path : ""; OnPropertyChanged(); }
        }

        /// <summary>Specifies startup option.</summary>
        public CollectionStartupOption startupOption
        {
            get => m_startupOption;
            set { m_startupOption = value; OnPropertyChanged(); }
        }

        /// <summary>The thread priority to use when opening this collection.</summary>
        public CollectionThreadPriority loadingPriority
        {
            get => m_loadingPriority;
            set { m_loadingPriority = value; OnPropertyChanged(); }
        }

        /// <summary>The label of this collection, can be used as a filter in object picker and project explorer to only show scenes that are contained within this collection.</summary>
        internal string label => "ASM:" + title.Replace(" ", "");

        /// <summary>Gets if this collection has any scenes.</summary>
        public bool hasScenes => m_scenes?.Any(s => !string.IsNullOrWhiteSpace(s)) ?? false;

        /// <summary>Calls <see cref="Resources.UnloadUnusedAssets"/> after collection is opened or closed.</summary>
        public bool unloadUnusedAssets
        {
            get => m_unloadUnusedAssets;
            set => m_unloadUnusedAssets = value;
        }

        public SceneTag Tag(Scene scene, SceneTag setTo = null)
        {

            //Tags have been moved from profile to collection, so lets move them if user has any defined in profile
            if (Profile.current.m_tags != null)
                RestoreTagsFromProfile();

            if (m_tags == null)
            {
                m_tags = new TagList();
                MarkAsDirty();
            }

            if (setTo != null)
            {
                _ = m_tags.Set(scene ? scene.path : "", setTo.id);
                MarkAsDirty();
#if UNITY_EDITOR
                scene.OnPropertyChanged();
#endif
            }

            return m_tags[scene ? scene.path : ""];

        }

        void RestoreTagsFromProfile()
        {

            if (Profile.current.m_tags == null)
                return;

            var didChange = false;
            foreach (var tag in Profile.current.m_tags.Where(t => m_scenes.Contains(t.Key)).ToArray())
            {
                _ = m_tags.Set(tag.Key, tag.Value);
                if (Profile.current.m_tags.Remove(tag.Key))
                    didChange = true;
            }

            if (Profile.current.m_tags.Count == 0)
            {
                Profile.current.m_tags = null;
                didChange = true;
            }

            if (didChange)
                Profile.current.MarkAsDirty();

        }

        #endregion
        #region Methods

        /// <inheritdoc cref="CollectionManager.Open"/>
        public SceneOperation Open() => SceneManager.collection.Open(this);

        /// <summary>Opens or reopens the collection, depending on whatever it is open or not.</summary>
        public SceneOperation OpenOrReopen() =>
            IsOpen()
            ? Reopen()
            : Open();

        /// <inheritdoc cref="CollectionManager.Toggle"/>
        public SceneOperation Toggle() => SceneManager.collection.Toggle(this);

        /// <inheritdoc cref="CollectionManager.Toggle"/>
        public SceneOperation Toggle(bool enabled) => SceneManager.collection.Toggle(this, enabled);

        /// <summary>Ensures collection is open. Does not reopen and does not throw error if collection already is open.</summary>
        public SceneOperation EnsureOpen() => Toggle(true);

        /// <inheritdoc cref="CollectionManager.Reopen"/>
        public SceneOperation Reopen() => SceneManager.collection.Reopen();

        /// <inheritdoc cref="CollectionManager.Close"/>
        public SceneOperation Close() => SceneManager.collection.Close();

        /// <inheritdoc cref="CollectionManager.IsOpen"/>
        public bool IsOpen() => SceneManager.collection.IsOpen(this);

        #region UnityEvent

        /// <inheritdoc cref="CollectionManager.Open"/>
        public void OpenEvent() => SpamCheck.EventMethods.Execute(() => Open());

        /// <inheritdoc cref="OpenOrReopen"/>
        public void OpenOrReopenEvent() => SpamCheck.EventMethods.Execute(() => OpenOrReopen());

        /// <inheritdoc cref="CollectionManager.Toggle"/>
        public void ToggleEvent() => SpamCheck.EventMethods.Execute(() => Toggle());

        /// <inheritdoc cref="CollectionManager.Toggle"/>
        public void ToggleEvent(bool enabled) => SpamCheck.EventMethods.Execute(() => Toggle(enabled));

        /// <inheritdoc cref="CollectionManager.Reopen"/>
        public void ReopenEvent() => SpamCheck.EventMethods.Execute(() => Reopen());

        /// <inheritdoc cref="CollectionManager.Close"/>
        public void CloseEvent() => SpamCheck.EventMethods.Execute(() => Close());

        #endregion

        /// <summary>Gets all scenes contained in this collection, including overriden loading screen, if set.</summary>
        public Scene[] AllScenes() =>
            (loadingScreenUsage == LoadingScreenUsage.Override && loadingScreen)
            ? scenes.Concat(new[] { loadingScreen }).ToArray()
            : scenes;

        /// <summary>Gets all scenes contained in this collection, including overriden loading screen, if set.</summary>
        public string[] AllScenePaths() =>
            (loadingScreenUsage == LoadingScreenUsage.Override && !string.IsNullOrWhiteSpace(m_loadingScreen))
            ? (m_scenes ?? Array.Empty<string>()).Concat(new[] { m_loadingScreen }).ToArray()
            : (m_scenes ?? Array.Empty<string>());

        /// <summary>Find the <see cref="Profile"/> that this collection is associated with.</summary>
        public Profile FindProfile() =>
            Profile.Find(p => p && p.collections.Contains(this)
#if UNITY_EDITOR
            || p.removedCollections.Contains(this)
#endif
            );

        /// <summary>Finds the <see cref="SceneCollection"/> with the specified name.</summary>
        public static SceneCollection Find(string title, bool onlyActiveProfile = true)
        {
            var collection = SceneManager.assets.allCollections.FirstOrDefault(c => c.title == title && (!onlyActiveProfile || c.FindProfile() == Profile.current));
            if (!collection && title.StartsWith(Profile.current.name + " - "))
                throw new NullReferenceException("Could not find collection. Are you using Scene.name? If so use Scene.title instead.");
            return collection;
        }

        /// <summary>Casts and returns <see cref="userData"/> as the specified type. Returns null if invalid type.</summary>
        public T UserData<T>() where T : ScriptableObject =>
            userData as T;

#if UNITY_EDITOR

        /// <summary>Adds an empty scene field to this <see cref="SceneCollection"/>.</summary>
        /// <remarks>Only available in editor.</remarks>
        public void AddScene()
        {
            ArrayUtility.Add(ref m_scenes, null);
            this.Save();
        }

        /// <summary>Adds a scene to this <see cref="SceneCollection"/>.</summary>
        /// <remarks>Only available in editor.</remarks>
        public void AddScene(Scene scene) =>
            AddScene(scene.path);

        /// <summary>Adds a scene to this <see cref="SceneCollection"/>.</summary>
        /// <remarks>Only available in editor.</remarks>
        public void AddScene(string scene)
        {
            ArrayUtility.Add(ref m_scenes, scene);
            this.Save();
        }

        /// <summary>Removes a scene from this <see cref="SceneCollection"/>.</summary>
        /// <remarks>Only available in editor.</remarks>
        public void RemoveScene(Scene scene) =>
            RemoveScene(scene.path);

        /// <summary>Removes a scene from this <see cref="SceneCollection"/>.</summary>
        /// <remarks>Only available in editor.</remarks>
        public void RemoveScene(string scene)
        {
            ArrayUtility.Remove(ref m_scenes, scene);
            this.Save();
        }

#endif

        #endregion

    }

}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Core.Actions;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using Lazy.Utility;
using UnityEngine;
using Scene = AdvancedSceneManager.Models.Scene;

namespace AdvancedSceneManager.Core
{

    #region Properties

    internal interface ISceneOperationProps
    {

        /// <summary>The scenes to open.</summary>
        ReadOnlyCollection<(Scene scene, bool force)> open { get; }

        /// <summary>The scenes to close.</summary>
        ReadOnlyCollection<(OpenSceneInfo scene, bool force)> close { get; }

        /// <summary>The scenes to reopen.</summary>
        ReadOnlyCollection<OpenSceneInfo> reopen { get; }

        /// <summary>The custom actions to run.</summary>
        ReadOnlyCollection<SceneAction> customActions { get; }

        /// <inheritdoc cref="customActions"/>
        ReadOnlyCollection<Callback> callbacks { get; }

        /// <summary>The collection that is associated with this scene operation.</summary>
        SceneCollection collection { get; }

        /// <summary>The loading screen that this loading screen will use, unless null or useLoadingScreen is false, in which case collection loading screen will be used, if one is associated.</summary>
        Scene loadingScreen { get; }

        /// <summary>Specifies whatever this scene operation will show a loading screen, if one is set through loadingScreen or associated collection.</summary>
        /// <remarks>In other words: If false, no loading screen will be shown regardless of loadingScreen or associated collection.</remarks>
        bool useLoadingScreen { get; }

        /// <summary>The scene manager that requested this scene operation.</summary>
        SceneManagerBase sceneManager { get; }

        /// <summary>Specifies whatever unused assets should be cleared to save memory.</summary>
        bool? clearUnusedAssets { get; }

        /// <summary>Specifies whatever <see cref="ICollectionOpen"/> and <see cref="ICollectionClose"/> callbacks are executed on the associated collection, if one is.</summary>
        bool doCollectionCallbacks { get; }

        /// <summary>Specifies the callback to use after loading screen scene is opened, but loading screen not yet shown. See <see cref="LoadingScreenUtility.OpenLoadingScreen{T}(Scene, float?, Action{T})"/>.</summary>
        Action<LoadingScreen> loadingScreenCallback { get; }

        /// <summary>Gets the loading priority for the background thread.</summary>
        /// <remarks>Defaults to <see cref="SceneCollection.loadingPriority"/> when collection is used, otherwise <see cref="Profile.backgroundLoadingPriority"/>.</remarks>
        ThreadPriority? loadingPriority { get; }

        /// <summary>The <see cref="SceneCloseBehavior"/> of the scenes opened.</summary>
        SceneCloseBehavior? closeBehavior { get; }

    }

    internal interface IFluent<T>
    {

        /// <inheritdoc cref="ISceneOperationProps.open"/>
        T Open(params Scene[] scene);
        /// <inheritdoc cref="ISceneOperationProps.open"/>
        T Open(IEnumerable<Scene> scene, bool force = true);
        /// <inheritdoc cref="ISceneOperationProps.close"/>
        T Close(params OpenSceneInfo[] scenes);
        /// <inheritdoc cref="ISceneOperationProps.close"/>
        T Close(IEnumerable<OpenSceneInfo> scenes, bool force = true);
        /// <inheritdoc cref="ISceneOperationProps.close"/>
        T Close(bool force, params OpenSceneInfo[] scenes);
        /// <inheritdoc cref="ISceneOperationProps.reopen"/>
        T Reopen(params OpenSceneInfo[] scene);
        /// <inheritdoc cref="ISceneOperationProps.reopen"/>
        T Reopen(params Scene[] scene);
        /// <inheritdoc cref="ISceneOperationProps.reopen"/>
        T Reopen(IEnumerable<OpenSceneInfo> scene);
        /// <inheritdoc cref="ISceneOperationProps.reopen"/>
        T Reopen(IEnumerable<Scene> scene);

        /// <inheritdoc cref="ISceneOperationProps.customActions"/>
        T WithAction(params SceneAction[] actions);
        /// <inheritdoc cref="ISceneOperationProps.customActions"/>
        T WithCallback(Callback actions);

        /// <inheritdoc cref="ISceneOperationProps.collection"/>
        T WithCollection(SceneCollection collection, bool withCallbacks = true);
        /// <inheritdoc cref="ISceneOperationProps.loadingScreen"/>
        T WithLoadingScreen(bool use);
        /// <inheritdoc cref="ISceneOperationProps.loadingScreen"/>
        T WithLoadingScreen(Scene scene);
        /// <inheritdoc cref="ISceneOperationProps.clearUnusedAssets"/>
        T WithClearUnusedAssets(bool enable);
        /// <inheritdoc cref="ISceneOperationProps.loadingScreenCallback"/>
        T WithLoadingScreenCallback(Action<LoadingScreen> callback);
        /// <inheritdoc cref="ISceneOperationProps.loadingPriority"/>
        T WithLoadingPriority(ThreadPriority priority);

        /// <inheritdoc cref="ISceneOperationProps.closeBehavior"/>
        T AsPersistent(SceneCloseBehavior closeBehavior);

    }

    internal class SceneOperationProps : ISceneOperationProps, IFluent<SceneOperation>
    {

        /// <summary>Gets whatever this <see cref="SceneOperationProps"/> is frozen, i.e. whatever properties can be changed.</summary>
        public bool IsFrozen { get; private set; }

        public Action onBeforeFreeze { get; set; }

        /// <summary>Freezes this <see cref="SceneOperationProps"/> so that properties cannot be changed.</summary>
        public void Freeze()
        {
            onBeforeFreeze?.Invoke();
            IsFrozen = true;
        }

        internal SceneOperation target { get; set; }

        SceneOperation Set(Action action, bool force = false)
        {
            if (IsFrozen && !force)
                throw new Exception("Cannot change SceneOperation properties once it has started executing!");
            action?.Invoke();
            return target;
        }

        public SceneOperationProps(SceneOperation target)
        {
            this.target = target;
            open = new ReadOnlyCollection<(Scene scene, bool force)>(m_open);
            close = new ReadOnlyCollection<(OpenSceneInfo scene, bool force)>(m_close);
            reopen = new ReadOnlyCollection<OpenSceneInfo>(m_reopen);
            customActions = new ReadOnlyCollection<SceneAction>(m_customActions);
            callbacks = new ReadOnlyCollection<Callback>(m_callbacks);
            reopenScene = new ReadOnlyCollection<Scene>(m_reopenScene);
        }

        #region Fields

        readonly List<(Scene scene, bool force)> m_open = new List<(Scene scene, bool force)>();
        readonly List<(OpenSceneInfo scene, bool force)> m_close = new List<(OpenSceneInfo scene, bool force)>();
        readonly List<OpenSceneInfo> m_reopen = new List<OpenSceneInfo>();
        readonly List<Scene> m_reopenScene = new List<Scene>();

        readonly List<SceneAction> m_customActions = new List<SceneAction>();
        readonly List<Callback> m_callbacks = new List<Callback>();

        #endregion
        #region Properties

        public ReadOnlyCollection<(Scene scene, bool force)> open { get; }
        public ReadOnlyCollection<(OpenSceneInfo scene, bool force)> close { get; }
        public ReadOnlyCollection<OpenSceneInfo> reopen { get; }
        public ReadOnlyCollection<Scene> reopenScene { get; }

        public ReadOnlyCollection<SceneAction> customActions { get; }
        public ReadOnlyCollection<Callback> callbacks { get; }

        public SceneCollection collection { get; private set; }
        public Scene loadingScreen { get; private set; }
        public bool useLoadingScreen { get; private set; }
        public SceneManagerBase sceneManager { get; private set; }
        public bool? clearUnusedAssets { get; private set; }
        public bool doCollectionCallbacks { get; private set; }
        public Action<LoadingScreen> loadingScreenCallback { get; private set; }
        public ThreadPriority? loadingPriority { get; private set; }
        public SceneCloseBehavior? closeBehavior { get; private set; }

        #endregion
        #region Fluent

        public SceneOperation Open(params Scene[] scene) =>
            Set(() => m_open.AddRange(scene.Select(s => (s, force: false))));

        public SceneOperation Close(bool force, params OpenSceneInfo[] scenes) =>
            Set(() => m_close.AddRange(scenes.Select(s => (s, force))));

        public SceneOperation Close(params OpenSceneInfo[] scenes) =>
            Close(force: false, scenes);

        public SceneOperation Reopen(params OpenSceneInfo[] scene) =>
            Set(() => m_reopen.AddRange(scene));

        public SceneOperation Reopen(params Scene[] scene) =>
            Set(() => m_reopenScene.AddRange(scene));

        public SceneOperation Open(IEnumerable<Scene> scene, bool force = false) =>
            Set(() => m_open.AddRange(scene.Select(s => (s, force))));

        public SceneOperation Close(IEnumerable<OpenSceneInfo> scenes, bool force = false) =>
            Set(() => m_close.AddRange(scenes.Select(s => (s, force))));

        public SceneOperation Reopen(IEnumerable<OpenSceneInfo> scene) =>
            Set(() => m_reopen.AddRange(scene));

        public SceneOperation Reopen(IEnumerable<Scene> scene) =>
            Set(() => m_reopenScene.AddRange(scene));

        public SceneOperation WithCollection(SceneCollection collection, bool withCallbacks = false) =>
            Set(() => { this.collection = collection; doCollectionCallbacks = withCallbacks; });

        public SceneOperation WithLoadingScreen(bool use) =>
            Set(() => useLoadingScreen = use);

        public SceneOperation WithLoadingScreen(Scene scene) =>
            Set(() => { loadingScreen = scene; useLoadingScreen = true; });

        public SceneOperation WithAction(params SceneAction[] actions) =>
            Set(() => m_customActions.AddRange(actions));

        public SceneOperation WithCallback(Callback actions) =>
            Set(() => m_callbacks.Add(actions));

        public SceneOperation WithClearUnusedAssets(bool enable) =>
            Set(() => clearUnusedAssets = enable);

        public SceneOperation WithLoadingScreenCallback(Action<LoadingScreen> callback) =>
            Set(() => loadingScreenCallback = callback);

        public SceneOperation WithLoadingPriority(ThreadPriority priority) =>
            Set(() => loadingPriority = priority);

        public SceneOperation AsPersistent(SceneCloseBehavior closeBehavior = SceneCloseBehavior.KeepOpenAlways) =>
            Set(() => this.closeBehavior = closeBehavior);

        #endregion

    }

    #endregion

    /// <summary>The phase that a <see cref="SceneOperation"/> is currently in.</summary>
    public enum Phase
    {
        /// <summary>The scene operation is currently executing close callbacks on the scenes that are being closed, if any.</summary>
        CloseCallbacks,
        /// <summary>The scene operation is currently unloading the scenes, if any.</summary>
        UnloadScenes,
        /// <summary>The scene operation is currently loading the scenes, if any.</summary>
        LoadScenes,
        /// <summary>The scene operation is currently executing open callbacks on the scenes that are being opened, if any.</summary>
        OpenCallbacks,
        /// <summary>The scene operation is currently executing custom actions, added through <see cref="SceneOperation.WithAction(SceneAction[])"/> or similar methods, if any.</summary>
        CustomActions
    }

    /// <inheritdoc cref="SceneOperation"/>
    /// <remarks>See also: <see cref="SceneOperation"/>.</remarks>
    public class SceneOperation<ReturnValue> : SceneOperation, IFluent<SceneOperation<ReturnValue>>
    {

        /// <summary>Gets a <see cref="SceneOperation"/> that has already completed.</summary>
        public new static SceneOperation<ReturnValue> done { get; } = FromResult(default);

        /// <summary>Gets a <see cref="SceneOperation"/> that has already completed.</summary>
        public static SceneOperation<ReturnValue> FromResult(ReturnValue value, SceneManagerBase sceneManager = null) =>
            new SceneOperation<ReturnValue>(sceneManager ? sceneManager : SceneManager.standalone) { isDone = true, value = value };

        /// <summary>The return value of this <see cref="SceneOperation{ReturnValue}"/>.</summary>
        public ReturnValue value { get; private set; }

        internal SceneOperation(SceneManagerBase sceneManager) : base(sceneManager)
        {
            props.target = this;
            props.onBeforeFreeze = () =>
            WithCallback(Callback.BeforeLoadingScreenClose().Do(() =>
            {

                if (action != null)
                    value = action.Invoke(this);
                else
                    Debug.LogError("Could not return value from SceneOperation since action is null.");

                isDone = true;

            }));
        }

        Func<SceneOperation<ReturnValue>, ReturnValue> action;

        /// <summary>Callback that is called when <see cref="SceneOperation"/> is done, that is meant to retrieve the return value of this operation.</summary>
        public SceneOperation<ReturnValue> Return(Func<SceneOperation<ReturnValue>, ReturnValue> action)
        {
            this.action = action;
            return this;
        }

        internal new SceneOperation<ReturnValue> SetParent(SceneOperation parent) =>
            (SceneOperation<ReturnValue>)base.SetParent(parent);

        public new SceneOperation<ReturnValue> WithFriendlyText(string text)
        {
            friendlyText = text;
            return this;
        }

        #region IFluent<SceneOperation<ReturnValue>>

        /// <summary>Closes the specified scene.</summary>
        public new SceneOperation<ReturnValue> Close(Scene scene, bool force = false)
        {
            if (scene && scene.GetOpenSceneInfo() is OpenSceneInfo osi)
                Close(force, osi);
            return this;
        }

        /// <inheritdoc cref="IFluent{T}.Open(IEnumerable{Scene}, bool)"/>
        public new SceneOperation<ReturnValue> Open(params Scene[] scene) => (SceneOperation<ReturnValue>)props.Open(scene);
        /// <inheritdoc cref="IFluent{T}.Open(IEnumerable{Scene}, bool)"/>
        public new SceneOperation<ReturnValue> Open(IEnumerable<Scene> scene, bool force = true) => (SceneOperation<ReturnValue>)props.Open(scene, force);
        /// <inheritdoc cref="IFluent{T}.Close(bool, OpenSceneInfo[])"/>
        public new SceneOperation<ReturnValue> Close(params OpenSceneInfo[] scenes) => (SceneOperation<ReturnValue>)props.Close(scenes);
        /// <inheritdoc cref="IFluent{T}.Close(bool, OpenSceneInfo[])"/>
        public new SceneOperation<ReturnValue> Close(IEnumerable<OpenSceneInfo> scenes, bool force = true) => (SceneOperation<ReturnValue>)props.Close(scenes, force);
        /// <inheritdoc cref="IFluent{T}.Close(bool, OpenSceneInfo[])"/>
        public new SceneOperation<ReturnValue> Close(bool force, params OpenSceneInfo[] scenes) => (SceneOperation<ReturnValue>)props.Close(force, scenes);
        /// <inheritdoc cref="IFluent{T}.Reopen(IEnumerable{OpenSceneInfo})"/>
        public new SceneOperation<ReturnValue> Reopen(params OpenSceneInfo[] scene) => (SceneOperation<ReturnValue>)props.Reopen(scene);
        /// <inheritdoc cref="IFluent{T}.Reopen(IEnumerable{OpenSceneInfo})"/>
        public new SceneOperation<ReturnValue> Reopen(IEnumerable<OpenSceneInfo> scene) => (SceneOperation<ReturnValue>)props.Reopen(scene);
        /// <inheritdoc cref="IFluent{T}.WithAction(SceneAction[])"/>
        public new SceneOperation<ReturnValue> WithAction(params SceneAction[] actions) => (SceneOperation<ReturnValue>)props.WithAction(actions);
        /// <inheritdoc cref="IFluent{T}.WithCallback(Callback)"/>
        public new SceneOperation<ReturnValue> WithCallback(Callback actions) => (SceneOperation<ReturnValue>)props.WithCallback(actions);
        /// <inheritdoc cref="IFluent{T}.WithCollection(SceneCollection, bool)"/>
        public new SceneOperation<ReturnValue> WithCollection(SceneCollection collection, bool withCallbacks = true) => (SceneOperation<ReturnValue>)props.WithCollection(collection, withCallbacks);
        /// <inheritdoc cref="IFluent{T}.WithLoadingScreen(bool)"/>
        public new SceneOperation<ReturnValue> WithLoadingScreen(bool use) => (SceneOperation<ReturnValue>)props.WithLoadingScreen(use);
        /// <inheritdoc cref="IFluent{T}.WithLoadingScreen(Scene)"/>
        public new SceneOperation<ReturnValue> WithLoadingScreen(Scene scene) => (SceneOperation<ReturnValue>)props.WithLoadingScreen(scene);
        /// <inheritdoc cref="IFluent{T}.WithClearUnusedAssets(bool)"/>
        public new SceneOperation<ReturnValue> WithClearUnusedAssets(bool enable = true) => (SceneOperation<ReturnValue>)props.WithClearUnusedAssets(enable);
        /// <inheritdoc cref="IFluent{T}.WithLoadingScreenCallback(Action{LoadingScreen})"/>
        public new SceneOperation<ReturnValue> WithLoadingScreenCallback(Action<LoadingScreen> callback) => (SceneOperation<ReturnValue>)props.WithLoadingScreenCallback(callback);
        /// <inheritdoc cref="IFluent{T}.WithLoadingPriority(ThreadPriority)"/>
        public new SceneOperation<ReturnValue> WithLoadingPriority(ThreadPriority priority) => (SceneOperation<ReturnValue>)props.WithLoadingPriority(priority);
        /// <inheritdoc cref="IFluent{T}.AsPersistent(SceneCloseBehavior)"/>
        public new SceneOperation<ReturnValue> AsPersistent(SceneCloseBehavior closeBehavior = SceneCloseBehavior.KeepOpenAlways) => (SceneOperation<ReturnValue>)props.AsPersistent(closeBehavior);
        /// <inheritdoc cref="IFluent{T}.Reopen(IEnumerable{OpenSceneInfo})"/>
        public new SceneOperation<ReturnValue> Reopen(params Scene[] scene) => (SceneOperation<ReturnValue>)props.Reopen(scene);
        /// <inheritdoc cref="IFluent{T}.Reopen(IEnumerable{OpenSceneInfo})"/>
        public new SceneOperation<ReturnValue> Reopen(IEnumerable<Scene> scene) => (SceneOperation<ReturnValue>)props.Reopen(scene);

        #endregion

        internal SceneOperation<ReturnValue> FlagAsLoadingScreen()
        {
            isLoadingScreen = true;
            return this;
        }

    }

    /// <summary>A scene operation is a queueable operation that can open or close scenes. See also: <see cref="SceneAction"/>.</summary>
    /// <remarks>See also: <see cref="SceneOperation{ReturnValue}"/>.</remarks>
    public class SceneOperation : CustomYieldInstruction, ISceneOperationProps, IFluent<SceneOperation>, IQueueable
    {

        #region Constructor / queue

        /// <summary>Gets a <see cref="SceneOperation"/> that has already completed.</summary>
        public static SceneOperation done { get; } = new SceneOperation(null) { isDone = true };

        internal SceneOperation(SceneManagerBase sceneManager)
        {
            this.sceneManager = sceneManager;
            props = new SceneOperationProps(this);
            actions = new ReadOnlyCollection<SceneAction>(m_actions);
        }

        static SceneOperation() =>
            QueueUtility<SceneOperation>.queueEmpty += ResetThreadPriority;

        /// <summary>Adds a new scene operation to the queue.</summary>
        /// <param name="ignoreQueue">Sets whatever this operation should ignore the queue, and start immediately.</param>
        /// <param name="sceneManager">The scene manager that should manage the scenes when opened.</param>
        public static SceneOperation Add(SceneManagerBase sceneManager, bool ignoreQueue = false) =>
            QueueUtility<SceneOperation>.Queue(new SceneOperation(sceneManager), ignoreQueue);

        /// <inheritdoc cref="Add(SceneManagerBase, bool)"/>
        public static SceneOperation<ReturnValue> Add<ReturnValue>(SceneManagerBase sceneManager, bool ignoreQueue = false) =>
            (SceneOperation<ReturnValue>)QueueUtility<SceneOperation>.Queue(new SceneOperation<ReturnValue>(sceneManager), ignoreQueue);

        /// <inheritdoc cref="Add(SceneManagerBase, bool)"/>
        public static SceneOperation<ReturnValue> Add<ReturnValue>(SceneManagerBase sceneManager, Func<SceneOperation, ReturnValue> @return, bool ignoreQueue = false) =>
            (SceneOperation<ReturnValue>)QueueUtility<SceneOperation>.Queue(new SceneOperation<ReturnValue>(sceneManager).Return(@return), ignoreQueue);

        void IQueueable.OnTurn(Action onComplete) => _
             = Run()?.StartCoroutine(description: friendlyText ?? "SceneOperation", onComplete: () =>
             {
                 isDone = true;
                 onComplete.Invoke();
             });

        void IQueueable.OnCancel() =>
            Cancel();

        bool IQueueable.CanQueue()
        {

            if (SceneManager.standalone.preloadedScene?.isStillPreloaded ?? false)
                throw new InvalidOperationException("Cannot queue a scene operation when a scene is preloaded. Please finish preload using SceneManager.standalone.preloadedScene.FinishPreload() / .Discard(). Scene helper can also be used.");

            return true;

        }

        static void ResetThreadPriority()
        {
            if (Profile.current && Profile.current.enableChangingBackgroundLoadingPriority)
                Application.backgroundLoadingPriority = Profile.current.backgroundLoadingPriority;
        }

        public string friendlyText { get; protected set; }

        public SceneOperation WithFriendlyText(string text)
        {
            friendlyText = text;
            return this;
        }

        #endregion
        #region ISceneOperationProps

        internal SceneOperationProps props { get; }

        /// <inheritdoc cref="ISceneOperationProps.open"/>
        public ReadOnlyCollection<(Scene scene, bool force)> open => ((ISceneOperationProps)props).open;
        /// <inheritdoc cref="ISceneOperationProps.close"/>
        public ReadOnlyCollection<(OpenSceneInfo scene, bool force)> close => ((ISceneOperationProps)props).close;
        /// <inheritdoc cref="ISceneOperationProps.reopen"/>
        public ReadOnlyCollection<OpenSceneInfo> reopen => ((ISceneOperationProps)props).reopen;
        /// <inheritdoc cref="ISceneOperationProps.customActions"/>
        public ReadOnlyCollection<SceneAction> customActions => ((ISceneOperationProps)props).customActions;
        /// <inheritdoc cref="ISceneOperationProps.callbacks"/>
        public ReadOnlyCollection<Callback> callbacks => ((ISceneOperationProps)props).callbacks;
        /// <inheritdoc cref="ISceneOperationProps.collection"/>
        public SceneCollection collection => ((ISceneOperationProps)props).collection;
        /// <inheritdoc cref="ISceneOperationProps.loadingScreen"/>
        public Scene loadingScreen => ((ISceneOperationProps)props).loadingScreen;
        /// <inheritdoc cref="ISceneOperationProps.useLoadingScreen"/>
        public bool useLoadingScreen => ((ISceneOperationProps)props).useLoadingScreen;
        /// <inheritdoc cref="ISceneOperationProps.clearUnusedAssets"/>
        public bool? clearUnusedAssets => ((ISceneOperationProps)props).clearUnusedAssets;
        /// <inheritdoc cref="ISceneOperationProps.doCollectionCallbacks"/>
        public bool doCollectionCallbacks => ((ISceneOperationProps)props).doCollectionCallbacks;
        /// <inheritdoc cref="ISceneOperationProps.loadingScreenCallback"/>
        public Action<LoadingScreen> loadingScreenCallback => ((ISceneOperationProps)props).loadingScreenCallback;
        /// <inheritdoc cref="ISceneOperationProps.loadingPriority"/>
        public ThreadPriority? loadingPriority => ((ISceneOperationProps)props).loadingPriority;
        /// <inheritdoc cref="ISceneOperationProps.closeBehavior"/>
        public SceneCloseBehavior? closeBehavior => ((ISceneOperationProps)props).closeBehavior;

        #endregion
        #region IFluent<SceneOperation>

        /// <summary>Closes the specified scene.</summary>
        public SceneOperation Close(Scene scene, bool force = false)
        {
            if (scene && scene.GetOpenSceneInfo() is OpenSceneInfo osi)
                Close(force, osi);
            return this;
        }

        /// <inheritdoc cref="IFluent{T}.Open(Scene[])"/>
        public SceneOperation Open(params Scene[] scene) => ((IFluent<SceneOperation>)props).Open(scene);
        /// <inheritdoc cref="IFluent{T}.Close(IEnumerable{OpenSceneInfo}, bool)"/>
        public SceneOperation Open(IEnumerable<Scene> scene, bool force = true) => ((IFluent<SceneOperation>)props).Open(scene, force);
        /// <inheritdoc cref="IFluent{T}.Close(OpenSceneInfo[])"/>
        public SceneOperation Close(params OpenSceneInfo[] scenes) => ((IFluent<SceneOperation>)props).Close(scenes);
        /// <inheritdoc cref="IFluent{T}.Close(IEnumerable{OpenSceneInfo}, bool)"/>
        public SceneOperation Close(IEnumerable<OpenSceneInfo> scenes, bool force = true) => ((IFluent<SceneOperation>)props).Close(scenes, force);
        /// <inheritdoc cref="IFluent{T}.Close(bool, OpenSceneInfo[])"/>
        public SceneOperation Close(bool force, params OpenSceneInfo[] scenes) => ((IFluent<SceneOperation>)props).Close(force, scenes);
        /// <inheritdoc cref="IFluent{T}.Reopen(OpenSceneInfo[])"/>
        public SceneOperation Reopen(params OpenSceneInfo[] scene) => ((IFluent<SceneOperation>)props).Reopen(scene);
        /// <inheritdoc cref="IFluent{T}.Reopen(IEnumerable{OpenSceneInfo})"/>
        public SceneOperation Reopen(IEnumerable<OpenSceneInfo> scene) => ((IFluent<SceneOperation>)props).Reopen(scene);
        /// <inheritdoc cref="IFluent{T}.Reopen(Scene[])"/>
        public SceneOperation Reopen(params Scene[] scene) => ((IFluent<SceneOperation>)props).Reopen(scene);
        /// <inheritdoc cref="IFluent{T}.Reopen(IEnumerable{Scene})"/>
        public SceneOperation Reopen(IEnumerable<Scene> scene) => ((IFluent<SceneOperation>)props).Reopen(scene);
        /// <inheritdoc cref="IFluent{T}.WithAction(SceneAction[])"/>
        public SceneOperation WithAction(params SceneAction[] actions) => ((IFluent<SceneOperation>)props).WithAction(actions);
        /// <inheritdoc cref="IFluent{T}.WithCallback(Callback)"/>
        public SceneOperation WithCallback(Callback actions) => ((IFluent<SceneOperation>)props).WithCallback(actions);
        /// <inheritdoc cref="IFluent{T}.WithCollection(SceneCollection, bool)"/>
        public SceneOperation WithCollection(SceneCollection collection, bool withCallbacks = true) => ((IFluent<SceneOperation>)props).WithCollection(collection, withCallbacks);
        /// <inheritdoc cref="IFluent{T}.WithLoadingScreen(bool)"/>
        public SceneOperation WithLoadingScreen(bool use) => ((IFluent<SceneOperation>)props).WithLoadingScreen(use);
        /// <inheritdoc cref="IFluent{T}.WithLoadingScreen(Scene)"/>
        public SceneOperation WithLoadingScreen(Scene scene) => ((IFluent<SceneOperation>)props).WithLoadingScreen(scene);
        /// <inheritdoc cref="IFluent{T}.WithClearUnusedAssets(bool)"/>
        public SceneOperation WithClearUnusedAssets(bool enable = true) => ((IFluent<SceneOperation>)props).WithClearUnusedAssets(enable);
        /// <inheritdoc cref="IFluent{T}.WithLoadingScreenCallback(Action{LoadingScreen})"/>
        public SceneOperation WithLoadingScreenCallback(Action<LoadingScreen> callback) => ((IFluent<SceneOperation>)props).WithLoadingScreenCallback(callback);
        /// <inheritdoc cref="IFluent{T}.WithLoadingPriority(ThreadPriority)"/>
        public SceneOperation WithLoadingPriority(ThreadPriority priority) => ((IFluent<SceneOperation>)props).WithLoadingPriority(priority);
        /// <inheritdoc cref="IFluent{T}.AsPersistent(SceneCloseBehavior)"/>
        public SceneOperation AsPersistent(SceneCloseBehavior closeBehavior) => ((IFluent<SceneOperation>)props).AsPersistent(closeBehavior);

        /// <summary>Closes the collection and its scenes.</summary>
        public SceneOperation Close(SceneCollection collection)
        {

            WithCollection(collection, withCallbacks: true);

            if (collection.IsOpen())
                Close(SceneManager.utility.openScenes.Reverse(), force: false);

            if (collection.IsOpen())
                Close(SceneManager.collection.openScenes.Reverse().Where(s => SceneManager.collection.current.Tag(s.scene).closeBehavior == SceneCloseBehavior.KeepOpenIfNextCollectionAlsoContainsScene), force: true);
            else
                Close(collection.scenes.Select(s => s.GetOpenSceneInfo()).OfType<OpenSceneInfo>().Reverse().Where(s => SceneManager.collection.current.Tag(s.scene).closeBehavior == SceneCloseBehavior.KeepOpenIfNextCollectionAlsoContainsScene), force: true);

            if (collection.IsOpen())
                WithCallback(Callback.BeforeLoadingScreenClose().Do(() =>
                {
                    SceneManager.collection.SetNull();
                    SceneManager.collection.InvokeClosed(SceneManager.collection.previous);
                }));

            return this;

        }

        /// <summary>Opens the collection and its scenes.</summary>
        public SceneOperation Open(SceneCollection collection, bool forceOpen = false, bool reopenPersistent = false)
        {

            WithCollection(collection, withCallbacks: true);
            Close(SceneManager.utility.openScenes.Reverse(), force: false);
            WithCallback(Callback.AfterLoadingScreenOpen().Do(() =>
            {
                if (SceneManager.collection.current)
                    SceneManager.collection.InvokeClosed(SceneManager.collection.current);
                SceneManager.collection.m_previous = SceneManager.collection.current;
                SceneManager.collection.m_current = collection;
                SceneManager.collection.InvokeOpen(collection);
            }));

            //If already open, then we want to reopen it, so we'll need to call Reopen() method instead of Open()
            if (collection != SceneManager.collection.current)
                Open(collection.scenes, force: forceOpen);
            else
                Reopen(collection.scenes.
                        Where(s => collection.Tag(s).openBehavior == SceneOpenBehavior.OpenNormally).
                        Where(s => !reopenPersistent || PersistentUtility.GetPersistentOption(s.GetOpenSceneInfo()?.unityScene ?? default) == SceneCloseBehavior.Close));

            return this;

        }

        #endregion
        #region Extensibility

        protected static readonly List<Callback> _extCallbacks = new List<Callback>();

        /// <summary>Adds the callback to every scene operation.</summary>
        public static void AddCallback(Callback callback)
        {
            _ = _extCallbacks.Remove(callback);
            _extCallbacks.Add(callback);
        }

        /// <summary>Removes a callback that was added to every scene operation.</summary>
        public static void RemoveCallback(Callback callback) =>
            _ = _extCallbacks.Remove(callback);

        #endregion
        #region Scene operation parenting

        /// <summary>Child operations progress is added to <see cref="totalProgress"/>.</summary>
        List<SceneOperation> ChildOperations { get; set; }

        /// <summary>Adds the <see cref="SceneOperation"/> as a child to this operation, causing this operation to report child progress in <see cref="totalProgress"/>.</summary>
        internal void AddChildOperation(SceneOperation operation)
        {
            if (ChildOperations == null)
                ChildOperations = new List<SceneOperation>();
            ChildOperations.Add(operation);
        }

        internal SceneOperation SetParent(SceneOperation parent)
        {
            parent?.AddChildOperation(this);
            return this;
        }

        #endregion
        #region Properties

        /// <summary>Specifies whatever this scene operation was started by ASM to open a loading screen.</summary>
        public bool isLoadingScreen { get; protected set; }

        /// <summary>Inherited from <see cref="CustomYieldInstruction"/>. Tells unity whatever the operation is done or not.</summary>
        public override bool keepWaiting => !isDone;

        /// <summary>The phase the this scene operation is currently in.</summary>
        public Phase phase { get; private set; }

        /// <summary>The scene manager that requested this scene operation.</summary>
        public SceneManagerBase sceneManager { get; private set; }

        /// <summary>The current action that is executing.</summary>
        public SceneAction current { get; private set; }

        /// <summary>Gets if this scene operation is cancelled.</summary>
        public bool cancelled { get; private set; }

        /// <summary>Gets the loading screen that was opened.</summary>
        public SceneOperation<LoadingScreenBase> openedLoadingScreen { get; private set; }

        public ReadOnlyCollection<SceneAction> actions { get; }
        List<SceneAction> m_actions = new List<SceneAction>();

        /// <summary>The total progress made by this operation.</summary>
        public float totalProgress
        {
            get
            {
                var actions = EnumerateActions().ToArray();
                return actions.Count() == 0 ? 0 : actions.Sum(a => a.progress) / actions.Length;
            }
        }

        /// <summary>Gets all actions, including those from child operations, on this operation.</summary>
        IEnumerable<SceneAction> EnumerateActions()
        {

            return Enumerate(this).Where(a => a.reportsProgress);

            IEnumerable<SceneAction> Enumerate(SceneOperation operation)
            {

                foreach (var action in operation.actions)
                {
                    if (action is null)
                        continue;
                    else if (action is AggregateAction aggregate)
                        foreach (var action1 in aggregate.actions)
                            yield return action1;
                    else
                        yield return action;
                }

                if (operation.ChildOperations != null)
                    foreach (var child in operation.ChildOperations)
                        foreach (var action in Enumerate(child))
                            yield return action;

            }

        }

        #endregion
        #region Run

        static bool IsDuplicate(SceneOperation left, SceneOperation right)
        {

            if (left.isLoadingScreen || right.isLoadingScreen)
                return false;

            if (left.open.Count + left.reopen.Count + left.close.Count == 0 ||
                right.open.Count + right.reopen.Count + right.close.Count == 0)
                return false;

            if (left.open.SequenceEqual(right.open) && left.reopen.SequenceEqual(right.reopen) && left.close.SequenceEqual(right.close))
                return true;

            return false;

        }

        Action cancelCallback; //Called in Run().Cancel()

        /// <summary>
        /// Cancel this operation.
        /// <para>Note that the operation might not be cancelled immediately, if user defined callbacks are currently running
        /// (WithAction(), WithCallback()) they will run to completion before operation is cancelled. 'cancelled' property can be used in callbacks to check whatever a operation is cancelled.</para>
        /// </summary>
        public void Cancel(Action callbackWhenFullyCancelled = null)
        {
            cancelCallback = callbackWhenFullyCancelled;
            cancelled = true;
        }

        GlobalCoroutine coroutine;

        protected bool isDone;
        bool areActionsDone;

        IEnumerator Run()
        {

            //Lets wait a bit so that users can change properties, since most cannot be changed once started
            yield return null;

            if (Profile.current.checkForDuplicateSceneOperations &&
                QueueUtility<SceneOperation>.running.Concat(QueueUtility<SceneOperation>.queue).Any(o => o != this && IsDuplicate(this, o)))
            {
                Debug.LogWarning("A duplicate scene operation was detected, it has been halted. This behavior can be changed in settings.");
                yield break;
            }

            //Evaluate current state and generate actions
            m_actions.AddRange(CreateActions());
            props.Freeze();

            //Set loading thread priority
            SetThreadPriority();

            yield return ShowLoadingScreen();
            yield return LoadingScreenCallback(Callback.When.Before);

            //Call collection close callbacks
            if (props.doCollectionCallbacks)
                yield return CallbackUtility.DoCollectionCloseCallbacks(SceneManager.collection.current);

            //Run actions one by one
            foreach (var action in actions)
            {

                if (Cancel())
                    yield break;

                yield return DoPhaseCallbacks(action, Callback.When.Before);

                //If action has invalid properties, it will call Done() early
                if (action.isDone)
                    continue;

                if (action.scene)
                    yield return SceneCallbacks(action.scene, Callback.When.Before);

                current = action;

                try
                {
                    coroutine = action.DoAction(sceneManager).StartCoroutine(description: "SceneOperation.Run(" + action.GetType().Name + ")");
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }

                while (coroutine.isRunning)
                    if (Cancel())
                        yield break;
                    else
                        yield return null;

                yield return DoPhaseCallbacks(action, Callback.When.After);
                if (action.scene)
                    yield return SceneCallbacks(action.scene, Callback.When.After);

                current = null;

            }

            areActionsDone = true;

            SetPersistent();

            SetActiveSceneForCollection(); //Set active scene in collections
            yield return LoadingScreenCallback(Callback.When.After);

            if (props.clearUnusedAssets ?? false ||
                (props.collection && props.collection.unloadUnusedAssets) ||
                (!props.collection && Profile.current.unloadUnusedAssetsForStandalone))
                yield return Resources.UnloadUnusedAssets();

            //Lets check if operation has been cancelled while running callbacks
            //(callbacks should run to completion before cancelling, callbacks can check cancelled property)
            if (Cancel())
                yield break;

            //Call collection open callbacks
            if (props.doCollectionCallbacks)
                yield return CallbackUtility.DoCollectionOpenCallbacks(collection);

            //Hide loading screen
            yield return HideLoadingScreen();

            bool Cancel()
            {

                if (cancelled)
                {
                    coroutine?.Stop();
                    cancelCallback?.Invoke();
                }

                return cancelled;
            }

        }

        #region Loading screen

        IEnumerator ShowLoadingScreen()
        {

            if (!useLoadingScreen)
                yield break;

            if (loadingScreen)
                openedLoadingScreen = LoadingScreenUtility.OpenLoadingScreen(loadingScreen, callbackBeforeBegin: Callback);
            else if (collection)
                openedLoadingScreen = LoadingScreenUtility.OpenLoadingScreen(collection, callbackBeforeBegin: Callback);

            if (openedLoadingScreen != null)
                yield return openedLoadingScreen;

            void Callback(LoadingScreenBase loadingScreen)
            {
                if (loadingScreen is LoadingScreen l)
                {
                    l.operation = this;
                    loadingScreenCallback?.Invoke(l);
                }
            }
        }

        IEnumerator HideLoadingScreen()
        {
            if (openedLoadingScreen?.value)
            {
                yield return LoadingScreenUtility.CloseLoadingScreen(openedLoadingScreen.value);
                openedLoadingScreen = null;
            }
        }

        #endregion
        #region Callbacks

        Type lastAction;
        Callback.When? lastWhen;

        readonly List<(Type type, Callback.When when)> callbacksRun = new List<(Type type, Callback.When when)>();

        IEnumerator DoPhaseCallbacks(SceneAction action, Callback.When when)
        {

            var type = action?.GetType();

            if (!HasRun() && lastWhen != when || lastAction != type)
            {
                callbacksRun.Add((type, when));
                var phase = GetPhase(action);
                if (when == Callback.When.Before)
                    yield return OnPhaseStart(phase);
                else if (when == Callback.When.After)
                    yield return OnPhaseEnd(phase);
            }

            lastAction = type;
            lastWhen = when;


            bool HasRun() =>
                callbacksRun.Contains((type, when));

        }

        Phase GetPhase(SceneAction action) =>
            phases.TryGetValue(action?.GetType(), out var phase)
            ? phase
            : Phase.CustomActions;

        static readonly Dictionary<Type, Phase> phases = new Dictionary<Type, Phase>()
        {
            { typeof(SceneCloseCallbackAction), Phase.CloseCallbacks },
            { typeof(SceneUnloadAction), Phase.UnloadScenes },
            { typeof(SceneLoadAction), Phase.LoadScenes },
            { typeof(SceneOpenCallbackAction), Phase.OpenCallbacks },
        };

        IEnumerator OnPhaseStart(Phase phase)
        {
            this.phase = phase;
            yield return CallCallbacks(scene: null, phase, Callback.When.Before);
        }

        IEnumerator OnPhaseEnd(Phase phase)
        {
            yield return CallCallbacks(scene: null, phase, Callback.When.After);
        }

        IEnumerator SceneCallbacks(Scene scene, Callback.When when)
        {
            yield return CallCallbacks(scene, phase, when);
        }

        IEnumerator LoadingScreenCallback(Callback.When when)
        {
            yield return CallCallbacks(scene: null, phase: null, when);
        }

        IEnumerator CallCallbacks(Scene scene, Phase? phase, Callback.When when)
        {
            yield return props.callbacks.Run(this, scene, phase, when);
            yield return _extCallbacks.Run(this, scene, phase, when); //Static extension callbacks
        }

        #endregion
        #region SetActiveSceneForCollection

        void SetActiveSceneForCollection()
        {

            if (collection)
            {

                if (openedScenes.Any())
                {

                    var target = collection.activeScene;
                    if (!target)
                        target = collection.scenes.First();

                    var uScene = openedScenes.FirstOrDefault(s => s?.scene == target)?.unityScene;
                    if (uScene.HasValue)
                        SceneManager.utility.SetActive(uScene.Value);

                }

            }

        }

        #endregion
        #region Thread priority

        void SetThreadPriority() =>
            SetThreadPriority(collection);

        internal SceneOperation SetThreadPriority(SceneCollection collection, bool ignoreQueueCheck = false)
        {

            //Set loading thread priority, if queued.
            //This property is global, and race conditions will occur if we allow non-queued operations to also set this

            if (!Profile.current || !Profile.current.enableChangingBackgroundLoadingPriority)
                return this;

            if (!QueueUtility<SceneOperation>.IsQueued(this) && !ignoreQueueCheck)
                return this;

            Application.backgroundLoadingPriority = GetPriority();

            return this;

            ThreadPriority GetPriority()
            {

                if (loadingPriority.HasValue)
                    return loadingPriority.Value;

                if (!collection)
                    return Profile.current.backgroundLoadingPriority;
                else
                {

                    if (collection.loadingPriority != CollectionThreadPriority.Auto)
                        return (ThreadPriority)collection.loadingPriority;
                    else
                    {

                        return LoadingScreenUtility.IsAnyLoadingScreenOpen
                            ? ThreadPriority.Normal
                            : ThreadPriority.Low;

                    }

                }

            }

        }

        #endregion
        #region Persistent

        void SetPersistent()
        {

            if (!props.closeBehavior.HasValue)
                return;

            foreach (var scene in FindActions<SceneLoadAction>().Select(a => a.unityScene).ToArray())
                PersistentUtility.Set(scene, props.closeBehavior.Value);

        }

        #endregion

        #endregion
        #region Generate actions

        bool ShouldOpen((Scene scene, bool force) scene)
        {

            if (!scene.scene)
                return false;

            if (SceneUtility.GetAllOpenUnityScenes().Any(s => s.path == scene.scene.path) && !close.Any(s => s.scene == scene.scene))
                return false;

            if (scene.force)
                return true;

            var option = collection ? collection.Tag(scene.scene) : SceneTag.Default;
            if (collection && option.openBehavior == SceneOpenBehavior.DoNotOpenInCollection)
                return false;
            else if (option.closeBehavior == SceneCloseBehavior.KeepOpenIfNextCollectionAlsoContainsScene && collection && collection.scenes.Contains(scene.scene) && scene.scene.isOpenInHierarchy)
                return false;

            return true;

        }

        bool ShouldClose((OpenSceneInfo scene, bool force) scene)
        {

            if (scene.scene == null)
                return false;

            if (!scene.scene.unityScene.HasValue)
                return false;
            else if (DefaultSceneUtility.IsDefaultScene(scene.scene.unityScene.Value))
                return false;
            else if (scene.force)
                return true;

            var option = PersistentUtility.GetPersistentOption(scene.scene.unityScene.Value);

            if (option == SceneCloseBehavior.KeepOpenAlways)
                return false;
            else if (option == SceneCloseBehavior.KeepOpenIfNextCollectionAlsoContainsScene && collection && collection.scenes.Contains(scene.scene.scene))
                return false;

            return true;

        }

        List<SceneAction> CreateActions()
        {

            foreach (var scene in props.reopenScene)
                _ = scene.GetOpenSceneInfo() is OpenSceneInfo info
                    ? props.Reopen(info)
                    : props.Open(scene);

            var reopen = this.reopen.Where(s => s != null && s.scene).ToArray();
            var open = this.open.Distinct().Where(ShouldOpen).Select(s => s.scene).Concat(reopen.Select(s => s.scene)).Distinct().ToArray();
            var close = this.close.Where(ShouldClose).Select(s => s.scene).Concat(reopen).GroupBy(s => s.scene).Select(g => g.First()).ToArray();

            //Construct list of actions, order:
            //Call close callbacks
            //Close scenes
            //Load scenes
            //Call post-activation callbacks

            var closeCallbacks = close.Select(s => new SceneCloseCallbackAction(s, collection)).ToArray();
            var unloadActions = close.Select(s => new SceneUnloadAction(s, collection)).ToArray();
            var loadActions = open.Select(s => new SceneLoadAction(s, collection)).ToArray();
            var openCallbacks = loadActions.Select(action => new SceneOpenCallbackAction(action, collection)).ToArray();

            var actions = new List<SceneAction>();

            actions.AddRange(closeCallbacks);
            actions.AddRange(unloadActions);
            actions.AddRange(loadActions);
            actions.AddRange(openCallbacks);
            actions.AddRange(props.customActions);

            return actions;

        }

        #endregion
        #region Find actions

        /// <summary>Finds the actions of a specified type that was used in this operation.</summary>
        public IEnumerable<SceneAction> FindActions<T>() where T : SceneAction =>
            actions.OfType<T>();

        /// <summary>Finds the last action of a specified type that was used in this operation.</summary>
        public SceneAction FindLastAction<T>() where T : SceneAction =>
            actions.OfType<T>().LastOrDefault();

        /// <summary>Gets the scenes that was opened in this operation.</summary>
        public IEnumerable<OpenSceneInfo> openedScenes
        {
            get
            {

                if (!areActionsDone)
                {
                    Debug.LogWarning("Cannot retrieve opened scenes when operation is not yet done.");
                    return Array.Empty<OpenSceneInfo>();
                }

                return actions.Select(a => a.GetTrackedScene()).Where(s => s?.isOpen ?? false);

            }
        }

        #endregion

    }

}

#if UNITY_2021_1_OR_NEWER && !ASM_LEGACY
using System;
using System.Collections.Generic;
using System.Linq;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using AdvancedSceneManager.Callbacks;
#endif

using UnityEngine;
using Object = UnityEngine.Object;
using System.Collections;

namespace AdvancedSceneManager.Defaults
{

    [AddComponentMenu("")]
    class InGameToolbar : MonoBehaviour
#if UNITY_2021_1_OR_NEWER && !ASM_LEGACY
        , ISceneOpenAsync, ISceneCloseAsync
#endif
    {

        public Object panelSettings;
        public Object visualTreeAsset;

#if UNITY_2021_1_OR_NEWER && !ASM_LEGACY

        VisualElement rootElement;

        void OnEnable()
        {

            //Add UIDocument, since unity asset store will not accept broken script references, and UIDocument was not available in 2019...
            var document = gameObject.AddComponent<UIDocument>();
            document.visualTreeAsset = (VisualTreeAsset)visualTreeAsset;
            document.panelSettings = (PanelSettings)panelSettings;

            rootElement = document.rootVisualElement;
            rootElement.style.opacity = 0;
            AddListeners();
            SetupButtons();
            SetupOperations();
            Refresh();
            this.ASMScene().keepOpenWhenCollectionsClose = true;

            if (EventSystem.current)
            {
                eventSystem = EventSystem.current;
                eventSystem.enabled = false;
            }

            this.EnsureCameraExists();

        }

        EventSystem eventSystem;

        public IEnumerator OnSceneOpen()
        {
            yield return LerpUtility.Lerp(0, 1, 0.25f, (t) => rootElement.style.opacity = t);
        }

        public IEnumerator OnSceneClose()
        {
            yield return LerpUtility.Lerp(1, 0, 0.25f, (t) => rootElement.style.opacity = t);
        }

        void OnDisable()
        {
            RemoveListeners();
            if (eventSystem)
            {
                eventSystem.enabled = true;
                eventSystem = null;
            }
        }

        void Update()
        {
            RefreshOperations();
        }

        #region Listeners

        void AddListeners()
        {
            RemoveListeners();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;
            SceneManager.runtime.sceneOpened += Runtime_sceneOpened;
            SceneManager.runtime.sceneClosed += Runtime_sceneOpened;
            SceneManager.runtime.collectionOpened += Runtime_collectionOpened;
            SceneManager.runtime.collectionClosed += Runtime_collectionOpened;
        }

        void RemoveListeners()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= SceneManager_sceneUnloaded;
            SceneManager.runtime.sceneOpened -= Runtime_sceneOpened;
            SceneManager.runtime.sceneClosed -= Runtime_sceneOpened;
            SceneManager.runtime.collectionOpened -= Runtime_collectionOpened;
            SceneManager.runtime.collectionClosed -= Runtime_collectionOpened;
        }

        void Runtime_sceneOpened(Scene obj) => Refresh();
        void SceneManager_sceneUnloaded(UnityEngine.SceneManagement.Scene arg0) => Refresh();
        void SceneManager_sceneLoaded(UnityEngine.SceneManagement.Scene arg0, UnityEngine.SceneManagement.LoadSceneMode arg1) => Refresh();

        void Runtime_collectionOpened(SceneCollection obj) => RefreshReopenButton();

        #endregion
        #region Buttons

        Button reopenCollectionButton;

        void SetupButtons()
        {

            rootElement.Q<Button>("button-restart").clicked += Restart;
            rootElement.Q<Button>("button-quit").clicked += Quit;

            reopenCollectionButton = rootElement.Q<Button>("button-reopen-collection");
            reopenCollectionButton.clicked += ReopenCollection;
            RefreshReopenButton();

        }

        void Restart() => SceneManager.app.Restart();
        void ReopenCollection() => SceneManager.openCollection.Open();
        void Quit() => SceneManager.app.Quit();

        void RefreshReopenButton() =>
            reopenCollectionButton.SetEnabled(SceneManager.openCollection);

        #endregion
        #region Operations

        Label queueText;
        Label runningText;

        void SetupOperations()
        {

            queueText = rootElement.Q<Label>("text-queued");
            runningText = rootElement.Q<Label>("text-running");
            RefreshOperations();

        }

        void RefreshOperations()
        {
            queueText.text = SceneManager.runtime.queuedOperations.Count().ToString();
            runningText.text = SceneManager.runtime.runningOperations.Count().ToString();
        }

        #endregion
        #region Lists

        void Refresh()
        {

            var scenes = SceneManager.runtime.openScenes.ToList();
            var splashScreens = Take(scenes, s => s.isSplashScreen);
            var loadingScreens = Take(scenes, s => s.isLoadingScreen);
#if ENABLE_INPUT_SYSTEM && INPUTSYSTEM
            var bindingScenes = Take(scenes, SceneBindingUtility.WasOpenedByBinding);
#endif
            var persistent = Take(scenes, s => s.isPersistent && !(s.openedBy && s.openedBy.isOpen));
            var collectionsScenes = Take(scenes, s => s.openedBy);
            var standalone = scenes.ToArray();
            var untracked =
                SceneUtility.GetAllOpenUnityScenes().
                Where(s => !FallbackSceneUtility.IsFallbackScene(s)).
                Where(s => !s.ASMScene() || !s.ASMScene().isOpen);

            AddToList(splashScreens.Select(s => s.name), rootElement.Q<Foldout>("group-splash-scenes"));
            AddToList(loadingScreens.Select(s => s.name), rootElement.Q<Foldout>("group-loading-scenes"));
            AddToList(persistent.Select(s => s.name), rootElement.Q<Foldout>("group-persistent"));
#if ENABLE_INPUT_SYSTEM && INPUTSYSTEM
            AddToList(bindingScenes.Select(s => s.name), rootElement.Q<Foldout>("group-binding-scenes"));
#endif
            AddToList(standalone.Select(s => s.name), rootElement.Q<Foldout>("group-standalone"));
            AddToList(untracked.Select(s => s.name), rootElement.Q<Foldout>("group-untracked"));

            var collectionList = rootElement.Q<Foldout>("group-collections");
            collectionList.Clear();
            var collections = collectionsScenes.GroupBy(s => s.openedBy).ToArray();
            foreach (var collection in collections)
                AddToList(
                    name: collection.Key.title + (collection.Key.isOpenAdditive ? " <color=#FFFFFF11>(additive)" : ""),
                    names: collection.Select(s => s.name + (s.isPersistent ? " <color=#FFFFFF11>(persistent)" : "")),
                    element: collectionList);

            rootElement.Query().ForEach(e => e.style.cursor = new(StyleKeyword.Initial));

        }

        Scene[] Take(List<Scene> scenes, Predicate<Scene> predicate)
        {
            var taken = scenes.Where(s => predicate(s)).ToArray();
            scenes.RemoveAll(predicate);
            return taken;
        }

        void AddToList(string name, IEnumerable<string> names, Foldout element)
        {
            var foldout = new Foldout { text = name };
            foldout.style.marginRight = 22;
            AddToList(names, foldout);
            element.Add(foldout);
            element.style.display = names.Count() > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void AddToList(IEnumerable<string> names, Foldout element)
        {

            element.Clear();
            foreach (var name in names)
            {
                var label = new Label(name);
                label.style.marginLeft = 22;
                label.style.marginRight = 22;
                element.Add(label);
            }

            element.style.display = names.Count() > 0 ? DisplayStyle.Flex : DisplayStyle.None;

        }

        #endregion

#endif

    }

}


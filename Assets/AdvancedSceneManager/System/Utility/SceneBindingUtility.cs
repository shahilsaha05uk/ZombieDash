#if ENABLE_INPUT_SYSTEM && INPUTSYSTEM

using System;
using System.Collections.Generic;
using System.Linq;
using AdvancedSceneManager.Models;
using UnityEngine;
using UnityEngine.UIElements;
using InputButton = AdvancedSceneManager.Models.InputButton;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AdvancedSceneManager.Utility
{

    /// <summary>Provides utility functions relating to scene bindings.</summary>
    /// <remarks>Only available if input system is installed.</remarks>
    public static class SceneBindingUtility
    {

        static SceneCollection m_openCollection;
        static List<Scene> m_standaloneScenes = new();

        /// <summary>Gets if <paramref name="collection"/> was opened by a binding.</summary>
        public static bool WasOpenedByBinding(SceneCollection collection) =>
            collection && collection == m_openCollection && SceneManager.openCollection == collection;

        /// <summary>Gets if the scene was opened by a binding.</summary>
        public static bool WasOpenedByBinding(Scene scene)
        {

            if (!scene)
                return false;

            if (!Profile.current)
                return false;

            if (m_standaloneScenes.Contains(scene))
            {
                Profile.current.standaloneScenes.GetBinding(scene);
                return true;
            }
            else if (m_openCollection && m_openCollection.Contains(scene))
                return true;
            else
                return false;

        }

        #region Tracking

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#endif

        [RuntimeInitializeOnLoadMethod]
        static void SetupTracking() =>
            SceneManager.OnInitialized(() =>
            {

                RestoreTrackedItems();

                SceneManager.runtime.sceneClosed += (s) =>
                {
                    m_standaloneScenes.Remove(s);
                    Persist();
                };

                SceneManager.runtime.collectionClosed += (c) =>
                {
                    if (c == m_openCollection)
                    {
                        m_openCollection = null;
                        Persist();
                    }
                };

            });

        static void Track(Scene scene)
        {
            m_standaloneScenes.Add(scene);
            Persist();
        }

        static void Track(SceneCollection collection)
        {
            m_openCollection = collection;
            Persist();
        }

        static void RestoreTrackedItems()
        {

#if UNITY_EDITOR
            EditorApplication.delayCall += () =>
            {

                var collection = SceneManager.assets.collections.Find(EditorPrefs.GetString("ASM.SceneBindings.Collection"));

                if (SceneManager.openCollection)
                    m_openCollection = collection;

                var ids = EditorPrefs.GetString("ASM.SceneBindings.Scenes").Split("\n");
                var scenes = ids?.Select(id => SceneManager.assets.scenes.Find(id))?.NonNull() ?? Enumerable.Empty<Scene>();

                m_standaloneScenes = scenes.Where(s => s.isOpen).ToList();

            };
#endif

        }

        static void Persist()
        {
#if UNITY_EDITOR
            EditorPrefs.SetString("ASM.SceneBindings.Collection", m_openCollection ? m_openCollection.id : "");
            EditorPrefs.SetString("ASM.SceneBindings.Scenes", string.Join("\n", m_standaloneScenes.Select(s => s.id)));
#endif
        }

        #endregion
        #region Enumerate

        /// <summary>Gets all bindings in the project.</summary>
        public static IEnumerable<(SceneCollection collection, Scene scene, Models.InputBinding binding)> GetBindings()
        {

            if (!Profile.current)
                yield break;

            var collections = Profile.current.collections.Where(c => c && (c.binding?.isValid ?? false));
            var scenes = Profile.current.standaloneScenes.sceneBindings.Where(s => s.scene && (s.binding?.isValid ?? false));

            foreach (var collection in collections)
                yield return (collection, null, collection.binding);

            foreach (var scene in scenes)
                yield return (null, scene.scene, scene.binding);

        }

        static IEnumerable<(SceneCollection collection, Scene scene, Models.InputBinding binding, InputAction action)> GetActions()
        {
            var bindings = GetBindings();
            foreach (var binding in bindings)
            {

                var name = "ASM:";
                if (binding.scene)
                    name += binding.scene.name;
                else if (binding.collection)
                    name += binding.collection.name;
                else
                    continue;

                var action = new InputAction(name);
                foreach (var button in binding.binding.buttons)
                    if (!bindings.IsDuplicate(button.path))
                        action.AddBinding(button.path);

                yield return (binding.collection, binding.scene, binding.binding, action);

            }
        }

        #endregion
        #region Listener

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static void OnLoadEditor() =>
            SceneManager.OnInitialized(() =>
            {
                EditorApplication.playModeStateChanged += (e) =>
                {
                    if (e == PlayModeStateChange.ExitingPlayMode)
                        DisableActions();
                };
            });
#endif

        [RuntimeInitializeOnLoadMethod]
        [InitializeInEditorMethod]
        static void StartListener() =>
            SceneManager.OnInitialized(() =>
            {
                if (Application.isPlaying)
                    EnableActions();
            });

        #region Input actions

        static readonly Dictionary<InputAction, (SceneCollection collection, Scene scene, Models.InputBinding binding)> activeActions = new();

        static void RestartActions()
        {
            DisableActions();
            EnableActions();
        }

        static void EnableActions()
        {
            var actions = GetActions().ToArray();
            foreach (var action in actions)
            {

                action.action.Enable();
                action.action.started += OnInputStarted;
                action.action.canceled += OnInputCancelled;
                activeActions.Add(action.action, (action.collection, action.scene, action.binding));

            }
        }

        static void DisableActions()
        {
            foreach (var action in activeActions.Keys)
            {
                action.started -= OnInputStarted;
                action.canceled -= OnInputCancelled;
                action.Disable();
                action.Dispose();
            }
            activeActions.Clear();
        }

        #endregion
        #region Button press

        static void OnInputStarted(InputAction.CallbackContext e)
        {
            if (activeActions.TryGetValue(e.action, out var binding))
            {
                if (binding.collection)
                    OnPress(binding.collection, binding.binding);
                else if (binding.scene)
                    OnPress(binding.scene, binding.binding);
            }
        }

        static void OnPress(SceneCollection collection, Models.InputBinding binding)
        {

            if (!collection.isOpen && CheckPreload())
            {
                Track(collection);
                if (binding.openCollectionAsAdditive)
                    collection.OpenAdditive();
                else
                    collection.Open();
            }
            else if (collection.isOpen && binding.interactionType == InputBindingInteractionType.Toggle && CheckPreload())
                collection.Close();

        }

        static void OnPress(Scene scene, Models.InputBinding binding)
        {

            if (!scene.isOpen && CheckPreload())
            {
                Track(scene);
                scene.Open();
            }
            else if (scene.isOpen && binding.interactionType == InputBindingInteractionType.Toggle && CheckPreload())
                scene.Close();

        }

        #endregion
        #region Button release

        static void OnInputCancelled(InputAction.CallbackContext e)
        {
            if (activeActions.TryGetValue(e.action, out var binding))
            {
                if (binding.collection)
                    OnRelease(binding.collection, binding.binding);
                else if (binding.scene)
                    OnRelease(binding.scene, binding.binding);
            }
        }

        static void OnRelease(SceneCollection collection, Models.InputBinding binding)
        {
            if (collection.isOpen && binding.interactionType == InputBindingInteractionType.Hold && CheckPreload())
                collection.Close();
        }

        static void OnRelease(Scene scene, Models.InputBinding binding)
        {
            if (scene.isOpen && binding.interactionType == InputBindingInteractionType.Hold && CheckPreload())
                scene.Close();
        }

        static bool CheckPreload()
        {

            if (SceneManager.runtime.preloadedScene)
            {
                Debug.LogError($"Cannot open scene / collection using binding, because a scene is currently being preloaded.");
                return false;
            }

            return true;

        }

        #endregion

        #endregion
        #region Listen and return input

        static IDisposable listener = null;

        public static bool isListening => listener is not null;

        /// <summary>Listen for input and calls <paramref name="onDone"/> to pass pressed binding. Ignores mouse bindings.</summary>
        /// <remarks>Only one listener can be active at a time.</remarks>
        public static void ListenForInput(Action<InputControl> onDone)
        {

            if (isListening)
                throw new InvalidOperationException("Cannot start listener when one already exists.");

            listener = InputSystem.onAnyButtonPress.Call(e =>
            {
                if (e.device != Mouse.current)
                {
                    CancelListenForInput();
                    //Debug.Log(e.path);
                    onDone.Invoke(e);
                }
            });

        }

        /// <summary>Cancels lister started with <see cref="ListenForInput(Action{InputControl})"/>.</summary>
        public static void CancelListenForInput()
        {
            listener?.Dispose();
            listener = null;
        }

        /// <summary>Gets if the binding is assigned to multiple scenes / collections.</summary>
        public static bool IsDuplicate(InputButton binding) =>
            IsDuplicate(binding.path);

        static bool IsDuplicate(string bindingPath) =>
            GetBindings().IsDuplicate(bindingPath);

        static bool IsDuplicate(this IEnumerable<(SceneCollection collection, Scene scene, Models.InputBinding binding)> list, string bindingPath)
        {

            if (!Profile.current)
                return false;

            var count = list.SelectMany(b => b.binding.buttons).Count(b => b.path == bindingPath);
            return count > 1;

        }

        #endregion
        #region UI

#if UNITY_EDITOR

        /// <summary>Setups up a binding field.</summary>
        public static void SetupBindingField(TemplateContainer template, SceneCollection collection) =>
            SetupBindingField(template, collection.binding, true, collection.Save);

        /// <summary>Setups up a binding field.</summary>
        public static void SetupBindingField(TemplateContainer template, Scene scene) =>
            SetupBindingField(template, Profile.current.standaloneScenes.GetBinding(scene), false, Profile.current.Save);

        static void SetupBindingField(TemplateContainer template, Models.InputBinding binding, bool isCollection, Action save)
        {

            template.Q("text-disabled").style.display = DisplayStyle.None;
            template.Q("interaction").style.display = DisplayStyle.Flex;
            template.Q("SceneBindingItem").style.display = DisplayStyle.Flex;

            var itemTemplateElement = template.Q<TemplateContainer>("SceneBindingItem");
            itemTemplateElement.style.display = DisplayStyle.None;
            var itemTemplate = itemTemplateElement.templateSource;

            var list = template.Q("list");
            list.Clear();
            for (int i = 0; i < binding.buttons.Count; i++)
                AddField(i);
            AddField();

            SetupBindingFieldInteraction(template, binding, save);

            if (isCollection)
            {
                var additiveCollectionToggle = template.Q<Toggle>("toggle-collection-additive");
                additiveCollectionToggle.style.display = DisplayStyle.Flex;
                additiveCollectionToggle.SetValueWithoutNotify(binding.openCollectionAsAdditive);
                additiveCollectionToggle.RegisterValueChangedCallback(e => binding.openCollectionAsAdditive = e.newValue);
            }

            void AddField(int? i = null)
            {
                var element = itemTemplate.Instantiate();
                SetupBindingFieldButtons(element, binding, i, () =>
                {
                    save?.Invoke();
                    SetupBindingField(template, binding, isCollection, save);
                    if (Application.isPlaying)
                        RestartActions();
                });
                list.Add(element);
            }

        }

        static void SetupBindingFieldButtons(TemplateContainer template, Models.InputBinding binding, int? index, Action saveAndReload)
        {

            var setBindingButton = template.Q<Button>("button-set-binding");
            var clearBindingButton = template.Q<Button>("button-clear-binding");

            setBindingButton.RegisterCallback<DetachFromPanelEvent>(e => CancelListenForInput());

            UpdateButtonText();

            setBindingButton.clicked += () =>
            {

                if (isListening)
                {
                    CancelListenForInput();
                    UpdateButtonText();
                }
                else
                {

                    setBindingButton.text = "Listening for input. Press button again to cancel.";

                    ListenForInput(action =>
                    {

                        setBindingButton.text = action.name;
                        if (index.HasValue)
                            binding.buttons[index.Value] = new(action);
                        else
                            binding.buttons.Add(new(action));

                        saveAndReload.Invoke();

                    });

                }

            };

            clearBindingButton.clicked += () =>
            {
                binding.buttons.RemoveAt(index.Value);
                saveAndReload.Invoke();
            };

            void UpdateButtonText()
            {

                var button = index.HasValue ? binding.buttons[index.Value] : default;

                if (!button.isValid)
                    SetText("Add binding...", false);

                else if (IsDuplicate(button))
                    SetText(button.name + " (duplicate binding)", true, true);

                else
                    SetText(button.name);

                void SetText(string text, bool clearButtonVisible = true, bool isTextRed = false)
                {
                    setBindingButton.text = text;
                    clearBindingButton.style.display = clearButtonVisible ? DisplayStyle.Flex : DisplayStyle.None;
                    setBindingButton.style.color = isTextRed ? new(Color.red) : new(Color.white);
                }

            }

        }

        static void SetupBindingFieldInteraction(TemplateContainer template, Models.InputBinding binding, Action save)
        {

            var closeTriggerGroup = template.Q<RadioButtonGroup>();
            closeTriggerGroup.choices = new[]
            {
                "Open", "Hold", "Toggle"
            };

            closeTriggerGroup.SetValueWithoutNotify((int)binding.interactionType);
            closeTriggerGroup.RegisterValueChangedCallback(e =>
            {

                if (e.newValue == -1)
                    return;

                binding.interactionType = (InputBindingInteractionType)e.newValue;
                save?.Invoke();

            });

        }

#endif

        #endregion

    }

}

#endif

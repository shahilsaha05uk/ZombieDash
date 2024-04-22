#pragma warning disable IDE0051 // Remove unused private members

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AdvancedSceneManager.Models;
using Lazy.Utility;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AdvancedSceneManager.Utility
{

    /// <summary>Contains functions for interacting with the default pause screen.</summary>
    [AddComponentMenu("")]
    public class PauseScreenUtility : MonoBehaviour
    {

        #region Static

        internal static void Initialize() =>
            CoroutineUtility.Run(when: () => SceneManager.runtime.isInitialized, action: () =>
            {

                coroutine?.Stop();
                Hide();

                if (Profile.current && Profile.current.useDefaultPauseScreen)
                    ListenForKey();

#if UNITY_EDITOR
                if (Profile.current)
                {
                    Profile.current.PropertyChanged -= Profile_PropertyChanged;
                    Profile.current.PropertyChanged += Profile_PropertyChanged;
                }
#endif

            });

        static void Profile_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Profile.useDefaultPauseScreen))
                Initialize();
        }

        /// <summary>Gets if the pause screen is currently open.</summary>
        public static bool isOpen =>
            current != null;

        static GlobalCoroutine coroutine;

        /// <summary>Starts listening keys and opens pause screen when keys pressed.</summary>
        public static void ListenForKey()
        {
            StopListening();
            coroutine = Listen().StartCoroutine(description: "Default Pause Screen");
        }

        /// <summary>Stops listening for keys, this will disable pause screen. (Manually calling <see cref="Show"/> will still work though)</summary>
        public static void StopListening() =>
            coroutine?.Stop();

        static IEnumerator Listen()
        {
            while (true)
            {

                yield return null;

                if (!LoadingScreenUtility.IsAnyLoadingScreenOpen)
                {

#if ENABLE_INPUT_SYSTEM
                    if ((UnityEngine.InputSystem.Keyboard.current?.escapeKey?.wasPressedThisFrame ?? false) ||
                        (UnityEngine.InputSystem.Gamepad.current?.startButton?.wasPressedThisFrame ?? false))
                        Toggle();
                    if (UnityEngine.InputSystem.Gamepad.current?.bButton?.wasPressedThisFrame ?? false)
                        Hide();
#else
                    if (Input.GetKeyDown(KeyCode.Escape))
                        Toggle();
#endif
                }

                if (PauseScreenInput.Current)
                {
                    if (EventSystem.current)
                        EventSystem.current.UpdateModules();
                    PauseScreenInput.Current.DoUpdate();
                }

            }
        }

        internal static PauseScreenUtility current;
        static bool IsOpeningOrClosing;
        static CursorLockMode cursorLockState;
        static bool cursorVisible;

        /// <summary>Shows the pause screen.</summary>
        public static void Show()
        {

            if (IsOpeningOrClosing || current)
                return;

            IsOpeningOrClosing = true;

            current = Instantiate(Resources.Load<GameObject>("AdvancedSceneManager/DefaultPauseScreen")).GetComponent<PauseScreenUtility>();
            DontDestroyOnLoad(current);

            if (current.GetComponent<Canvas>() is Canvas canvas)
            {
                canvas.PutOnTop();
                _ = canvas.gameObject.AddComponent<PauseScreenInput>();
            }

            _ = current.Begin().StartCoroutine(() => IsOpeningOrClosing = false);

            cursorLockState = Cursor.lockState;
            cursorVisible = Cursor.visible;

            if (!FindObjectOfType<Camera>())
            {
                Debug.LogWarning("No camera found, when opening pause screen, creating one temporarily", current);
                var camera = current.gameObject.AddComponent<Camera>();
                camera.backgroundColor = Color.black;
            }

        }

        void LateUpdate()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>Hides the pause screen.</summary>
        public static void Hide(bool ignoreAnimations = false)
        {

            if (IsOpeningOrClosing)
                return;

            if (!current)
                return;

            IsOpeningOrClosing = true;

            _ = DoHide().StartCoroutine();
            IEnumerator DoHide()
            {

                if (!ignoreAnimations)
                    yield return current.End();

                Destroy(current.gameObject);
                current = null;
                IsOpeningOrClosing = false;

            }

            Cursor.lockState = cursorLockState;
            Cursor.visible = cursorVisible;

        }

        /// <summary>Toggles the pause screen on / off.</summary>
        public static void Toggle()
        {
            if (!current)
                Show();
            else
                Hide();
        }

        #endregion

        public Button resume;
        public Button restartCollection;
        public Button restartGame;
        public Button quit;

        public CanvasGroup canvasGroup;

        public IEnumerator Begin()
        {
            if (canvasGroup)
            {
                canvasGroup.alpha = 0;
                yield return canvasGroup.Fade(1, 0.25f);
            }
        }

        public IEnumerator End()
        {
            if (canvasGroup)
                yield return canvasGroup.Fade(0, 0.25f);
        }

        #region Buttons

        public void RestartCollection()
        {

            _ = Wait().StartCoroutine();
            IEnumerator Wait()
            {

                canvasGroup.interactable = false;

                yield return SceneManager.collection.Reopen();

                if (canvasGroup)
                    canvasGroup.interactable = true;
                Resume();

            }

        }

        public void RestartGame()
        {
            canvasGroup.interactable = false;
            SceneManager.runtime.Restart();
        }

        public void Resume() =>
            Hide();

        public void Quit()
        {
            canvasGroup.interactable = false;
            SceneManager.runtime.Quit();
        }

        #endregion

    }

    class PauseScreenInput : MonoBehaviour
    {

        public static PauseScreenInput Current { get; private set; }

        int index = 0;

        List<Button> buttons;

        void Start()
        {

            if (!EventSystem.current)
            {
#if ENABLE_INPUT_SYSTEM
                gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
            }

            Current = this;

            buttons = new List<Button>()
            {
                PauseScreenUtility.current.resume,
                PauseScreenUtility.current.restartCollection,
                PauseScreenUtility.current.restartGame,
                PauseScreenUtility.current.quit,
            };

        }

        void OnDestroy()
        {
            if (Current == this)
                Current = null;
        }

        void MoveUp() =>
            MoveTo(index - 1);

        void MoveDown() =>
            MoveTo(index + 1);

        void MoveTo(int index)
        {

            if (index < 0)
                index = 0;
            if (index > 3)
                index = 3;

            this.index = index;
            EventSystem.current.SetSelectedGameObject(buttons[index].gameObject);

        }

        void Activate()
        {
            if (buttons.ElementAtOrDefault(index) is Button button)
                _ = ExecuteEvents.Execute(button.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        }

        void Deselect()
        {
            index = -1;
            EventSystem.current.SetSelectedGameObject(null);
        }

        public bool isUsingPointer;

#if !ENABLE_INPUT_SYSTEM
        Vector3 mousePos;
#endif

        //Update is not called for base class if we define it here, so instead we have to create our own update function
        //UpdateModule() does not work since there is some issue with input module activation
        public void DoUpdate()
        {

#if ENABLE_INPUT_SYSTEM

            if (UnityEngine.InputSystem.Pointer.current?.delta?.EvaluateMagnitude() > 1)
                isUsingPointer = true;
            else if (UnityEngine.InputSystem.InputSystem.devices.Where(d => !typeof(UnityEngine.InputSystem.Pointer).IsAssignableFrom(d.GetType())).Any(d => d.wasUpdatedThisFrame))
                isUsingPointer = false;

            if (!isUsingPointer)
            {

                if ((UnityEngine.InputSystem.Keyboard.current?.upArrowKey?.wasPressedThisFrame ?? false) ||
                    (UnityEngine.InputSystem.Gamepad.current?.dpad.up?.wasPressedThisFrame ?? false))
                    MoveUp();

                if ((UnityEngine.InputSystem.Keyboard.current?.downArrowKey?.wasPressedThisFrame ?? false) ||
                    (UnityEngine.InputSystem.Gamepad.current?.dpad.down?.wasPressedThisFrame ?? false))
                    MoveDown();

                if ((UnityEngine.InputSystem.Keyboard.current?.enterKey?.wasPressedThisFrame ?? false) ||
                    (UnityEngine.InputSystem.Keyboard.current?.numpadEnterKey?.wasPressedThisFrame ?? false) ||
                    (UnityEngine.InputSystem.Gamepad.current?.aButton?.wasPressedThisFrame ?? false))
                    Activate();

            }

#else

            if (Input.mousePresent && (mousePos - Input.mousePosition).magnitude > 1)
                isUsingPointer = true;
            else if (Input.anyKey)
                isUsingPointer = false;

            mousePos = Input.mousePosition;

            if (!isUsingPointer)
            {

                if (Input.GetKeyDown(KeyCode.UpArrow))
                    MoveUp();
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                    MoveDown();
                else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    Activate();

            }

#endif

            if (isUsingPointer)
                Deselect();

        }

    }

}

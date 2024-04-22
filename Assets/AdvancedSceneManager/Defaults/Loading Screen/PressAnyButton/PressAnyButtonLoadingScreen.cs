using System.Collections;
using Lazy.Utility;
using UnityEngine;

#if INPUTSYSTEM
using UnityEngine.InputSystem;
#endif

namespace AdvancedSceneManager.Defaults
{

    /// <summary>A default loading screen script. Requires the user to press any key before loading screen closes.</summary>
    public class PressAnyButtonLoadingScreen : FadeLoadingScreen
    {

        // This is best used with 
        // if (AdvancedSceneManager.Utility.LoadingScreenUtility.isAnyLoadingScreenOpen) { }
        // so you can start the game after loading screen is closed
        bool pressed;
        bool canPress;

        public override IEnumerator OnOpen()
        {
            yield return FadeIn();
        }

        public override IEnumerator OnClose()
        {

            // We don't want it to activate before it's loaded
            canPress = true;

            // Unity's coroutine doesn't work here, apply our. 
            yield return WaitUntil().StartCoroutine();
            yield return FadeOut();

        }

        void Update()
        {

            if (!canPress)
                return;

#if INPUTSYSTEM
            if (Keyboard.current.anyKey.wasPressedThisFrame) pressed = true;
#else
            if (Input.anyKey) pressed = true;
#endif

        }

        IEnumerator WaitUntil()
        {
            yield return new WaitUntil(() => pressed);
        }

    }

}

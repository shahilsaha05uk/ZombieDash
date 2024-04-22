#pragma warning disable CS0414

using System.Collections;
using UnityEngine;

namespace AdvancedSceneManager.Callbacks
{

    /// <summary>A class that contains callbacks for splash screens.</summary>
    /// <remarks><see cref="SplashScreen"/> and <see cref="LoadingScreen"/> cannot coexist within the same scene.</remarks>
    public abstract class SplashScreen : LoadingScreenBase
    {

        /// <summary>Called when scene manager is ready to display the splash screen.</summary>
        /// <remarks>Example: yielding new WaitForSeconds(5) will show the splash screen for 5 seconds.</remarks>
        public abstract IEnumerator DisplaySplashScreen();

        /// <summary>Called when the splash screen is opened.</summary>
        public override IEnumerator OnOpen()
        { yield break; }

        /// <summary>Called when the loading screen is about to close.</summary>
        /// <remarks>Calls <see cref="DisplaySplashScreen"/>, so make sure to call it manually or call base if overridden.</remarks>
        public override IEnumerator OnClose()
        { yield return DisplaySplashScreen(); }

        [SerializeField]
        private bool isSplashScreen = true;

        public virtual void OnValidate()
        {
            if (!isSplashScreen)
                isSplashScreen = true;
        }

    }

}

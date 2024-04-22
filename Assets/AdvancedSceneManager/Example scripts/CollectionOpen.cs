using System.Collections;
using AdvancedSceneManager.Core;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using UnityEngine;

namespace AdvancedSceneManager.ExampleScripts
{

    /// <summary>Contains examples for opening collections.</summary>
    public class CollectionOpen : MonoBehaviour
    {

        public SceneCollection collectionToOpen;

        #region Open

        public void Open()
        {
            collectionToOpen.Open();
            //Equivalent to:
            //SceneManager.runtime.Open(collectionToOpen);
        }

        #endregion
        #region Open with user data

        public void OpenWithUserData(ScriptableObject scriptableObject)
        {
            //Note: Overrides data set from scene manager window
            collectionToOpen.userData = scriptableObject;
            collectionToOpen.Open();
        }

        #endregion
        #region Open with loading screen

        //Overrides loading screen
        public void OpenWithLoadingScreen(Scene loadingScreen)
        {

            if (!loadingScreen)
            {
                //LoadingScreenUtility.fade will be null if default fade loading screen scene has been deleted or otherwise un-imported from ASM.
                loadingScreen = LoadingScreenUtility.fade;
            }

            collectionToOpen.Open().
                With(loadingScreen).
                DisableLoadingScreen(). //Disables loading screen if needed
                EnableLoadingScreen();

        }

        #endregion
        #region Fluent api / Chaining

        public void ChainingExample()
        {

            //Open(), and other similar ASM methods, return SceneOperation.
            //SceneOperation has a fluent api that can configure it within exactly one frame of it starting (note that operations are queued, so: queue time + 1 frame). 
            collectionToOpen.Open().
                With(ThreadPriority.High).      //Sets Application.backgroundLoadingPriority for the duration of the operation
                UnloadUsedAssets().             //Calls Resources.UnloadUnusedAssets() after all scenes have been loaded / unloaded
                Callback(Callback.AfterLoadingScreenOpen().Do(() => Debug.Log("Loading screen opened."))).
                Callback(Callback.After(Phase.LoadScenes).Do(DoStuffInCoroutine));

            //Note that all callbacks are still called, even if there no loading screen or any scenes loaded

        }

        IEnumerator DoStuffInCoroutine()
        {
            //ASM will wait for this coroutine to finish before continuing normal operation
            yield return new WaitForSeconds(1);
        }

        #endregion

        public void ToggleOpen() => collectionToOpen.ToggleOpen();

    }

}

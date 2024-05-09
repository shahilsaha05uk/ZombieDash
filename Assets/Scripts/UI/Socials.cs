using UnityEngine;
using Facebook.Unity;
using System;
using System.Threading;

public class Socials : MonoBehaviour
{

    private void Start()
    {
        if (!FB.IsInitialized)
        {
            // Initialize the Facebook SDK
            FB.Init(OnFBInitialised, OnHideIdentity);
        }
        else
        {
            // Already initialized, signal an app activation App Event
            FB.ActivateApp();
        }
    }
    public void OnPlayGamesButtonClick()
    {
        Login();
    }
    private void Login()
    {
        if (FB.IsInitialized) { FB.ActivateApp(); }
        else print("Failed to initialise");
    }


    private void OnHideIdentity(bool isGameShown)
    {
        if (!isGameShown)
        {
            Time.timeScale = 0;
        }
        else
        {
            Time.timeScale = 1;
        }
    }

    private void OnFBInitialised()
    {
        if(FB.IsLoggedIn)
        {
            print("fb login success");
            string s = "client token" + FB.ClientToken + " user id: " + AccessToken.CurrentAccessToken.UserId;
        }
        else
        {
            print("fb login failed");
        }
    }
}

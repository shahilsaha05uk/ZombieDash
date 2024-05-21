using UnityEngine;
using Facebook.Unity;
using System;
using System.Threading;
using System.Collections.Generic;

public class Socials : MonoBehaviour
{

    private void Awake()
    {
        TryFacebookLogin();
    }

    private void TryFacebookLogin()
    {
        if (!FB.IsInitialized)
            FB.Init(OnFBInitialised, OnHideIdentity);   // if not initialised
        else
        {
            FB.ActivateApp();   // if initialised, activate the app
        }
    }

    private void AuthCallback(ILoginResult result)
    {
        // AccessToken has the session details
        var aToken = Facebook.Unity.AccessToken.CurrentAccessToken;

        Debug.Log(aToken.UserId);

        foreach (string perm in aToken.Permissions)
        {
            Debug.Log(perm);
        }
    }

    public void OnPlayGamesButtonClick()
    {
        TryFacebookLogin();

        if (FB.IsLoggedIn)
        {
            var perms = new List<string>() { "public_profile", "email" };
            FB.LogInWithReadPermissions(perms, AuthCallback);
        }

    }
    private void Login()
    {
        if (!FB.IsInitialized)
            FB.Init(OnFBInitialised, OnHideIdentity);   // if not initialised
        else
            FB.ActivateApp();   // if initialised, activate the app
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

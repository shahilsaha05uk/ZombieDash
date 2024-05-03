using UnityEngine;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using System;


public class Socials : MonoBehaviour
{
    public void OnPlayGamesButtonClick()
    {
        Debug.Log("Connect to Play Games");

        PlayGamesPlatform.Activate();

        if (PlayGamesPlatform.Instance != null)
        {
            PlayGamesPlatform.Instance.Authenticate(ProcessLogin);
        }
        else
        {
            Debug.Log("Play Games Platform instance is null");
        }
    }

    private void ProcessLogin(SignInStatus status)
    {
        if(status == SignInStatus.Success)
        {

            string name = PlayGamesPlatform.Instance.GetUserDisplayName();
            string id = PlayGamesPlatform.Instance.GetUserId();
            string image = PlayGamesPlatform.Instance.GetUserImageUrl();
            Debug.Log("Sign in Success");
        }
        else
        {
            Debug.Log("Sign in Failed");
            PlayGamesPlatform.Instance.ManuallyAuthenticate(ProcessLogin);
        }
    }
}

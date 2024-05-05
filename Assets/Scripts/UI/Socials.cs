using UnityEngine;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using System;
using System.Threading.Tasks;
using System.Security.Authentication;
using Unity.Services.Authentication;
using Unity.Services.Core;
using GooglePlayGames.OurUtils;

public class Socials : MonoBehaviour
{

    private void Start()
    {
        PlayGamesPlatform.Activate();
    }
    public void OnPlayGamesButtonClick()
    {
        Login();
    }
    private void Login()
    {
        if (PlayGamesPlatform.Instance != null)
        {
            Debug.Log("Connect to Play Games");
            PlayGamesPlatform.Instance.Authenticate(success =>
            {
                switch (success)
                {
                    case SignInStatus.Success:
                        string name = PlayGamesPlatform.Instance.GetUserDisplayName();
                        string id = PlayGamesPlatform.Instance.GetUserId();
                        string image = PlayGamesPlatform.Instance.GetUserImageUrl();
                        Debug.Log("Sign in Success");

                        break;
                    case SignInStatus.InternalError:
                        break;
                    case SignInStatus.Canceled:
                        Debug.Log("Sign in Failed");
                        break;
                }

                Debug.Log("Sign in Failed");

            });
        }
        else
        {
            Debug.Log("Play Games Platform instance is null");
        }

    }
}

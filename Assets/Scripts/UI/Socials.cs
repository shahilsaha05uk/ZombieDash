using UnityEngine;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using System;
using System.Threading.Tasks;
using System.Security.Authentication;

public class Socials : MonoBehaviour
{

    private void Start()
    {
        PlayGamesPlatform.Activate();
    }
    public void OnPlayGamesButtonClick()
    {
        Debug.Log("Connect to Play Games");

        if (PlayGamesPlatform.Instance != null)
        {
            if(PlayGamesPlatform.Instance.IsAuthenticated())
            {


                return;
            }
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

            PlayGamesPlatform.Instance.RequestServerSideAccess(true, code =>
            {

            });
            Debug.Log("Sign in Success");
        }
        else
        {
            Debug.Log("Sign in Failed");
            PlayGamesPlatform.Instance.ManuallyAuthenticate(ProcessLogin);
        }
    }

/*    async Task SignInWithGooglePlayGamesAsync(string authCode)
    {
        try
        {
            await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(authCode);
        }
        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
    }
*/}

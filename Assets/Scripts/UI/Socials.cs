using UnityEngine;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;

public class Socials : MonoBehaviour
{
    [SerializeField] private int mWelcomeTreatAmount = 200;
    
    public Button mAchievementButton;

    private SignInStatus mInitialAuthenticationStatus;
    private void Awake()
    {
        // It tries to log in to the account initiially when the game starts
        PlayGamesPlatform.Activate();
        if (!PlayGamesPlatform.Instance.IsAuthenticated())
        {
            PlayGamesPlatform.Instance.Authenticate(status => { mInitialAuthenticationStatus = status; });
        }
    }

    public void Login()
    {
        // It tries to log in to log in when the player presses the login button
        if (mInitialAuthenticationStatus != SignInStatus.Success)
        {
            PlayGamesPlatform.Instance.Authenticate(status =>
            {
                // Reward them with some bonus money if they are logging in for the first time.
                if (status != SignInStatus.Success)
                {
                    PlayGamesPlatform.Instance.ManuallyAuthenticate(manualSuccess =>
                    {
                        if (manualSuccess == SignInStatus.Success)
                        {
                            if (AchievementManager.Instance.UpdateAchievement(EAchievement.WelcomeTreat))
                            {
                                ResourceComp.AddResources(mWelcomeTreatAmount);
                            }
                        }
                    });
                }
                else
                {
                    if (AchievementManager.Instance.UpdateAchievement(EAchievement.WelcomeTreat))
                    {
                        ResourceComp.AddResources(mWelcomeTreatAmount);
                    }
                }
            });
        }
    }
}

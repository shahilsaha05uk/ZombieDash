using System;
using System.Collections;
using System.Collections.Generic;
using AdvancedSceneManager.Models;
using EnumHelper;
using GooglePlayGames;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenu : BaseWidget
{
    public SceneCollection GamePersistentCollection;

    [SerializeField] private Socials mSocial;
    [SerializeField] private Button mPlayButton;
    [SerializeField] private Button mAchievementButton;
    [SerializeField] private Button mLoginButton;
    [SerializeField] private Button mExitButtonClick;

    private void OnEnable()
    {
        mUiType = EUI.MAIN_MENU;
        mPlayButton.onClick.AddListener(OnPlayButtonClick);
        mAchievementButton.onClick.AddListener(OnAchievementsButtonClick);
        mLoginButton.onClick.AddListener(OnLoginButtonClick);
        mExitButtonClick.onClick.AddListener(Application.Quit);

        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
    }

    private void OnLoginButtonClick()
    {
        mSocial.Login();
    }
    private void OnPlayButtonClick()
    {
        GamePersistentCollection.Open(true);
        LevelManager.Instance.OpenAdditiveScene(ELevel.GAME, true);
        DestroyWidget();
    }
    private void OnAchievementsButtonClick()
    {
        if (PlayGamesPlatform.Instance.IsAuthenticated())
        {
            PlayGamesPlatform.Instance.ShowAchievementsUI(status => { });
        }
    }
}

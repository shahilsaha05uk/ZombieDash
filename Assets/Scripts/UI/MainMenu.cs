using System;
using System.Collections;
using System.Collections.Generic;
using EnumHelper;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : BaseWidget
{
    public delegate void FOnPlayButtonClickSignature();

    public FOnPlayButtonClickSignature OnPlayButtonClicked;
    
    [SerializeField] private Button mPlayButton;
    [SerializeField] private Button mSettingsButton;

    protected override void OnEnable()
    {
        base.OnEnable();

        mUiType = EUI.MAIN_MENU;
        mPlayButton.onClick.AddListener(OnPlayButtonClick);
        mSettingsButton.onClick.AddListener(OnSettingsButtonClick);

        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
    }

    private void OnPlayButtonClick()
    {
        LevelManager.Instance.OpenScene(ELevel.GAME, true);
        
        DestroyWidget();
    }
    private void OnSettingsButtonClick()
    {
        
    }
}

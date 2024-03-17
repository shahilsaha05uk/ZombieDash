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
    private void Awake()
    {
        mUiType = EUI.MAIN_MENU;
        mPlayButton.onClick.AddListener(OnPlayButtonClick);
        mSettingsButton.onClick.AddListener(OnSettingsButtonClick);
    }
    
    private void OnPlayButtonClick()
    {
        OnPlayButtonClicked?.Invoke();
        OnPlayButtonClicked = null;
        
        DestroyWidget();
    }
    private void OnSettingsButtonClick()
    {
        
    }

}

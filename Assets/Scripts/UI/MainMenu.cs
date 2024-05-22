using System;
using System.Collections;
using System.Collections.Generic;
using AdvancedSceneManager.Models;
using EnumHelper;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenu : BaseWidget
{
    public SceneCollection GamePersistentCollection;
    
    [SerializeField] private Button mPlayButton;

    protected override void OnEnable()
    {
        base.OnEnable();

        mUiType = EUI.MAIN_MENU;
        mPlayButton.onClick.AddListener(OnPlayButtonClick);

        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
    }


    private void OnPlayButtonClick()
    {
        GamePersistentCollection.Open(true);
        LevelManager.Instance.OpenAdditiveScene(ELevel.GAME, true);
        DestroyWidget();
    }
    private void OnSettingsButtonClick()
    {
        
    }
}

using System;
using EnumHelper;
using Helpers;
using Interfaces;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : BaseWidget
{
    [SerializeField] private Button mResumeButtonClick;
    [SerializeField] private Button mMenuButtonClick;

    private BaseAnimatedUI mAnimUI;
    
    public delegate void FOnGameResumeSignature();
    public event FOnGameResumeSignature OnGameResume;

    private void Awake()
    {
         mResumeButtonClick.onClick.AddListener(OnResumeButtonClick);
         mMenuButtonClick.onClick.AddListener(OnMenuButtonClick);
    }
    private void OnEnable()
    {
        if (TryGetComponent(out mAnimUI))
        {
            mAnimUI.StartAnim(EAnimDirection.Forward);
            Time.timeScale = 0;
        }
    }

    
    // Button Bonded Methods
    public void OnResumeButtonClick()
    {
        Time.timeScale = 1;
        mAnimUI.StartAnim(EAnimDirection.Backward);
        OnGameResume?.Invoke();
    }
    public void OnMenuButtonClick()
    {
        LevelManager.Instance.OpenAdditiveScene(ELevel.MENU, true);
    }
}

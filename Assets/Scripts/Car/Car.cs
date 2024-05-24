using System;
using System.Collections;
using System.Collections.Generic;
using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Models;
using EnumHelper;
using Helpers;
using Interfaces;
using StructClass;
using UnityEngine;
using UnityEngine.InputSystem;

public class Car : BaseCar, ICollectionCloseAsync
{
    private bool bStartedWaitingTimer = false;
    private Coroutine WaitTimerCoroutine;
    protected PlayerHUD mPlayerHUD;
    

    protected override void Awake()
    {
        base.Awake();
        if (mPlayerHUD == null)
        {
            var uiManager = UIManager.Instance;
            if (uiManager != null)
            {
                mPlayerHUD = uiManager.SpawnWidget(EUI.PLAYERHUD).GetWidgetAs<PlayerHUD>();
                
                BaseCar car = this;
                mPlayerHUD.Init(ref car);
            }
        }
        mPlayerHUD.ActivatePanel(EPanelType.Upgrade);

        mCarManager.OnGoalReached += OnGoalReached;
        GameManager.OnResetLevel += OnReset;
    }

    protected override void OnStartDrive()
    {
        base.OnStartDrive();
        mController.ToggleInputContext(true);
        mPlayerHUD.ActivatePanel(EPanelType.Hud);
        mCarManager.StartManagement();
    }

    protected override void OnStopDrive()
    {
        mController.ToggleInputContext(false);

        mCarManager.StopManagement();
        mPlayerHUD.ActivatePanel(EPanelType.Review);
    }

    protected override void OnDriving()
    {
        if (mCarManager.Velocity.x <1f && !bStartedWaitingTimer && bStartedDriving)
        {
            WaitTimerCoroutine = StartCoroutine(WaitTimer());
            bStartedWaitingTimer = true;
        }
        mPlayerHUD.UpdateProgress(mCarManager.progress); 
    }

    private IEnumerator WaitTimer()
    {
        yield return new WaitForSeconds(3f);
        if (mCarManager.Velocity.x < 1f)
        {
            bStartedWaitingTimer = false;
            StopDrive();
        }
        else
        {
            bStartedWaitingTimer = false;
            StopCoroutine(WaitTimerCoroutine);
        }
    }

    public override void OnReset()
    {
        base.OnReset();
        mPlayerHUD.ActivatePanel(EPanelType.Upgrade);
        mController.ToggleInputContext(false);

    }

    private void OnGoalReached()
    {
        StartCoroutine(GoalReached());
    }

    private IEnumerator GoalReached()
    {
        yield return new WaitForSeconds(3f);
        mController.ToggleInputContext(false);
        carRb.velocity = Vector2.zero;
        mPlayerHUD.ActivatePanel(EPanelType.GameComplete);
    }

    public IEnumerator OnCollectionClose(SceneCollection collection)
    {
        mPlayerHUD.DestroyWidget();
        yield return null;
    }

    public void PauseGame()
    {
        mPlayerHUD.ActivatePanel(EPanelType.Pause);
    }
}
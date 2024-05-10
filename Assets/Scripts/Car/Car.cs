using System;
using System.Collections;
using System.Collections.Generic;
using EnumHelper;
using Interfaces;
using StructClass;
using UnityEngine;

public class Car : BaseCar
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

        GameManager.OnResetLevel += OnReset;
    }

    protected override void OnStartDrive()
    {
        base.OnStartDrive();
        mPlayerHUD.ActivatePanel(EPanelType.Hud);
        mCarManager.StartManagement();
    }

    protected override void OnStopDrive()
    {
        mCarManager.StopManagement();
        mCarManager.AwardResources();
        mPlayerHUD.ActivatePanel(EPanelType.Review);
    }

    protected override void OnDriving()
    {
        /*
         Three of the conditions need to be true in order to get the timer started:
         Current Velocity should be less than 1
         the Timer didnt start
         and the driving was started
         */
        if ((mCurrentVelocityMag <= 2f || bIsVelocityPositive == false) && (!bStartedWaitingTimer && bStartedDriving))
        {
            print("Expected: Current Vel = 1 or less; \n isVelocity = false; \n bStartedWaitingTimer = false; \n Started driving = true");
            print($"Actual: Current Vel = {mCurrentVelocityMag}; \n isVelocity = {bIsVelocityPositive}; \n bStartedWaitingTimer = {bStartedWaitingTimer}; \n Started driving = {bStartedDriving}");
            WaitTimerCoroutine = StartCoroutine(WaitTimer());
            bStartedWaitingTimer = true;
        }
        mPlayerHUD.UpdateProgress(mCarManager.progress);
    }

    private IEnumerator WaitTimer()
    {
        yield return new WaitForSeconds(1f);
        if (mCurrentVelocityMag <= 1f)
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
    }
}
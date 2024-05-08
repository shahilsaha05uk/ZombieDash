using System;
using System.Collections;
using System.Collections.Generic;
using EnumHelper;
using StructClass;
using UnityEngine;

public class Car : BaseCar
{
    #region Reset Properties
    private Vector3 pos;
    private Vector3 scale;
    private Quaternion rot;
    #endregion
    
    protected PlayerHUD mPlayerHUD;

    protected override void Awake()
    {
        base.Awake();
        
        var trans = transform;
        
        pos = trans.position;
        rot = trans.rotation;
        scale = trans.localScale;
        
        
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
        mPlayerHUD.ActivatePanel(EPanelType.Review);
        mCarManager.StopManagement();

        if (mCarManager.distanceDifference > 50)
        {
            ResourceComp.AddResources(100);
        }
        else
        {
            ResourceComp.AddResources(50);
        }
    }

    protected override void OnDriving()
    {
        base.OnDriving();
        mPlayerHUD.UpdateProgress(mCarManager.progress);
    }
    private void OnReset()
    {
        frontTireRb.velocity = backTireRb.velocity = carRb.velocity = Vector2.zero;

        transform.SetPositionAndRotation(pos, rot);
        transform.localScale = scale;
        
        mPlayerInput.Disable();
        mPlayerHUD.ActivatePanel(EPanelType.Upgrade);

        foreach (var c in ComponentsDic)
        {
            c.Value.ResetComponent();
        }
    }
}
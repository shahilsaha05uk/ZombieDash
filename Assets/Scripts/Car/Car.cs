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
        StartCoroutine(UpdateDistance());
    }

    protected override void OnResourcesExhausted()
    {
        mPlayerHUD.ActivatePanel(EPanelType.Review);
        mPlayerInput.Move.Disable();
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
    
    private IEnumerator UpdateDistance()
    {
        while (true)
        {
            float currentDistance = Mathf.Abs(endPos.transform.position.x - transform.position.x);
            float progress = 1 - (currentDistance / mTotalDistance);
            mPlayerHUD.UpdateDistance(progress);
         
            yield return null;
        }
    }

}

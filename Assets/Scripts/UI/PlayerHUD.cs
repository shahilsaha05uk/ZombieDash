using System;
using System.Collections;
using System.Collections.Generic;
using EnumHelper;
using StructClass;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PlayerHUD : BaseWidget
{
   // public GameObject startPos;
   // public GameObject flag;
   // public GameObject player;

    [SerializeField] private Slider mPlayerProgress;
    [SerializeField] private Slider mFuelMeter;
    [SerializeField] private Slider mBoostMeter;
    
    [SerializeField]private TextMeshProUGUI txtKPH;


    private float totalDistance;

    private void Awake()
    {
        mUiType = EUI.PLAYERHUD;
    }

    private void Start()
    {
        mFuelMeter.maxValue = 1;
        mFuelMeter.minValue = 0;
        mFuelMeter.value = 1;
        
        mBoostMeter.maxValue = 1;
        mBoostMeter.minValue = 0;
        mBoostMeter.value = 1;
        //totalDistance = Mathf.Abs(flag.transform.position.x - startPos.transform.position.x);
    }

    private void Update()
    {
        /*
        float currentDistance = Mathf.Abs(flag.transform.position.x - player.transform.position.x);
        float progress = 1 - (currentDistance / totalDistance);
        mPlayerProgress.value = progress;
    */
    }

    public void UpdateCarStatus(FHudValues hudStatus)
    {
        //txtKPH.text = ((int)(hudStatus.speed * 3.6f)).ToString();
        mFuelMeter.value = hudStatus.fuel;
        mBoostMeter.value = hudStatus.nitro;
    }
}

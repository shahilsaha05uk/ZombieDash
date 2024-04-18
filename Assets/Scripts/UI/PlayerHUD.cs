using System;
using System.Collections;
using System.Collections.Generic;
using EnumHelper;
using speedometer;
using StructClass;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PlayerHUD : BaseWidget
{
    [SerializeField] private Speedometer mNitro;
    [SerializeField] private Speedometer mFuel;
    private float totalDistance;

    private void Awake()
    {
        mUiType = EUI.PLAYERHUD;
    }

    private void Start()
    {
        /*        mFuelMeter.maxValue = 1;
                mFuelMeter.minValue = 0;
                mFuelMeter.value = 1;

                mBoostMeter.maxValue = 1;
                mBoostMeter.minValue = 0;
                mBoostMeter.value = 1;
        //totalDistance = Mathf.Abs(flag.transform.position.x - startPos.transform.position.x);

        */

    }


    public void UpdateCarStatus(FCarMetrics hudStatus, ECarPart partToUpdate = ECarPart.All_Comp)
    {
        switch (partToUpdate)
        {
            case ECarPart.All_Comp:
                mNitro.UpdateValue(hudStatus.nitro);
                mFuel.UpdateValue(hudStatus.fuel);
                break;
            case ECarPart.Fuel:
                mFuel.UpdateValue(hudStatus.fuel);
                break;
            case ECarPart.Nitro:
                mNitro.UpdateValue(hudStatus.nitro);
                break;
        }
    }
}

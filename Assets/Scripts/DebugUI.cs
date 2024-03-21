using System;
using System.Collections;
using System.Collections.Generic;
using EnumHelper;
using TMPro;
using UnityEngine;

public class DebugUI : MonoBehaviour
{
    public delegate void FOnMoveInputUpdate(float Value);
    public static FOnMoveInputUpdate OnMoveInputUpdate;

    public delegate void FOnValueUpdate(float Value, ECarPart partType);
    public static FOnValueUpdate OnValueUpdate;
    
    public delegate void FOnSpeedUpdate(int Value);
    public static FOnSpeedUpdate OnSpeedUpdate;
    public delegate void FOnMessageUpdate(string Value);
    public static FOnMessageUpdate OnMessageUpdate;

    public delegate void FOnSpeedRateUpdate(float Value);
    public static FOnSpeedRateUpdate OnSpeedRateUpdate;


    [SerializeField] private TextMeshProUGUI mMessage;
    [SerializeField] private TextMeshProUGUI mMoveInput;
    [SerializeField] private TextMeshProUGUI mSpeed;
    [SerializeField] private TextMeshProUGUI mSpeedRate;
    [SerializeField] private TextMeshProUGUI mFuel;
    [SerializeField] private TextMeshProUGUI mNitro;

    private void Awake()
    {
        OnValueUpdate += UpdatePartValue;
        
        OnMessageUpdate += UpdateMessage;
        OnMoveInputUpdate += UpdateMoveText;
        OnSpeedUpdate += UpdateSpeedText;
        OnSpeedRateUpdate += UpdateSpeedRateText;

    }

    private void UpdatePartValue(float value, ECarPart parttype)
    {
        string formattedValue;
        switch (parttype)
        {
            case ECarPart.Fuel:
                formattedValue = value.ToString("F2");
                mFuel.text = "Fuel: " + formattedValue;
                break;
            
            case ECarPart.Nitro:
                formattedValue = value.ToString("F2");
                mNitro.text = "Nitro: " + formattedValue;
                break;
        }
    }


    private void UpdateMessage(string value = "")
    {
        mMessage.text = value;
    }

    private void UpdateSpeedRateText(float value)
    {
        mSpeedRate.text = "Speed Rate: " + value;
    }

    private void UpdateSpeedText(int value)
    {
        mSpeed.text = "Speed: " + value;
    }

    private void UpdateMoveText(float value)
    {
        mMoveInput.text = "Move Input: " + value;
    }
}

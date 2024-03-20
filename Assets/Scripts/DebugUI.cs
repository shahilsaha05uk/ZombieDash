using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DebugUI : MonoBehaviour
{
    public delegate void FOnMoveInputUpdate(float Value);
    public static FOnMoveInputUpdate OnMoveInputUpdate;

    public delegate void FOnSpeedUpdate(int Value);
    public static FOnSpeedUpdate OnSpeedUpdate;
    public delegate void FOnFuelUpdate(float Value);
    public static FOnFuelUpdate OnFuelUpdate;
    public delegate void FOnNitroUpdate(float Value);
    public static FOnNitroUpdate OnNitroUpdate;

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
        OnMessageUpdate += UpdateMessage;
        OnMoveInputUpdate += UpdateMoveText;
        OnSpeedUpdate += UpdateSpeedText;
        OnSpeedRateUpdate += UpdateSpeedRateText;

        OnFuelUpdate += UpdateFuelText;
        OnNitroUpdate += UpdateNitroText;
    }

    private void UpdateMessage(string value = "")
    {
        mMessage.text = value;
    }

    private void UpdateNitroText(float value)
    {
        string formattedValue = value.ToString("F2");
        mNitro.text = "Nitro: " + formattedValue;
    }

    private void UpdateFuelText(float value)
    {
        string formattedValue = value.ToString("F2");
        mFuel.text = "Fuel: " + formattedValue;
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

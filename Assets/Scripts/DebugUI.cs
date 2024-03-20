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

    public delegate void FOnSpeedRateUpdate(float Value);
    public static FOnSpeedRateUpdate OnSpeedRateUpdate;


    [SerializeField] private TextMeshProUGUI mMoveInput;
    [SerializeField] private TextMeshProUGUI mSpeed;
    [SerializeField] private TextMeshProUGUI mSpeedRate;

    private void Awake()
    {
        OnMoveInputUpdate += UpdateMoveText;
        OnSpeedUpdate += UpdateSpeedText;
        OnSpeedRateUpdate += UpdateSpeedRateText;
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

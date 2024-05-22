using AYellowpaper.SerializedCollections;
using EnumHelper;
using Helpers;
using speedometer;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public enum EPanelType { None, Hud, Upgrade, Review}
public class PlayerHUD : BaseWidget, IDayCompleteInterface
{
    private UpgradeUI mUpgradeUI;

    private BaseCar mCarRef;

    [SerializedDictionary("Type", "Canvas")]
    [SerializeField] SerializedDictionary<EPanelType, CanvasGroup> CanvasMap;
    private EPanelType mCurrentlyOpenedPanel;
    
    [SerializeField] private Speedometer mNitro;
    [SerializeField] private Speedometer mFuel;
    [SerializeField] private DistanceMeter mDistanceMeter;

    [SerializeField] private Button mPedalButton;
    [SerializeField] private Button mRollClockButton;
    [SerializeField] private Button mRollAntiClock;
    [SerializeField] private Button mNitroButton;
    [SerializeField] private Button mPauseButton;

    private void Awake()
    {
        mUiType = EUI.PLAYERHUD;

        mUpgradeUI = CanvasMap[EPanelType.Upgrade].GetComponent<UpgradeUI>();
        if (mUpgradeUI)
        {
            mUpgradeUI.OnPlayClick += OnPlayClick;
        }
    }

    public void Init(ref BaseCar Car, EPanelType PanelToActivate = EPanelType.None)
    {
        if (Car != null)
        {
            mCarRef = Car;
            mCarRef.OnComponentUpdated += OnWidgetUpdateRequest;
        }
        ActivatePanel(PanelToActivate);
    }

    private void OnPlayClick()
    {
        mCarRef.StartDrive();
    }

    public void UpdateProgress(float NormalizedDistance)
    {
        mDistanceMeter.UpdateValue(NormalizedDistance);
    }

    private void OnWidgetUpdateRequest(ECarPart carpart, float value)
    {
        switch (carpart)
        {
            case ECarPart.All_Comp:
                mNitro.UpdateValue(value);
                mFuel.UpdateValue(value);
                break;
            case ECarPart.Fuel:
                mFuel.UpdateValue(value);
                break;
            case ECarPart.Nitro:
                mNitro.UpdateValue(value);
                break;
            case ECarPart.Speed:
                break;
        }
    }

    // Panel Togglers
    public void ActivatePanel(EPanelType Panel, bool deactivateLastPanel = true)
    {
        if(deactivateLastPanel)
        {
            DeactivatePanel(mCurrentlyOpenedPanel);
            mCurrentlyOpenedPanel = EPanelType.None;
        }
        if (CanvasMap.ContainsKey(Panel))
        {
            CanvasMap[Panel].gameObject.SetActive(true);
            CanvasMap[Panel].alpha = 1f;
            mCurrentlyOpenedPanel = Panel;
        }
    }

    public void DeactivatePanel(EPanelType Panel)
    {
        if (CanvasMap.ContainsKey(Panel))
        {
            CanvasMap[Panel].alpha = 0f;
            CanvasMap[Panel].gameObject.SetActive(false);
        }
    }

    public void OnDayComplete()
    {
        DeactivatePanel(EPanelType.Hud);
        DeactivatePanel(EPanelType.Review);
    }
}

using AYellowpaper.SerializedCollections;
using EnumHelper;
using Helpers;
using speedometer;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public enum EPanelType { None, Hud, Upgrade, Review, Pause, GameComplete}
public class PlayerHUD : BaseWidget, IDayCompleteInterface
{
    private BaseCar mCarRef;

    [SerializedDictionary("Type", "Canvas")]
    [SerializeField] SerializedDictionary<EPanelType, CanvasGroup> CanvasMap;
    private EPanelType mCurrentlyOpenedPanel;
    
    [SerializeField] private Speedometer mNitro;
    [SerializeField] private Speedometer mFuel;
    [SerializeField] private DistanceMeter mDistanceMeter;

    
    private void Awake()
    {
        mUiType = EUI.PLAYERHUD;

        if (CanvasMap.TryGetValue(EPanelType.Upgrade, out CanvasGroup upgradeMenu))
        {
            if(upgradeMenu.TryGetComponent(out UpgradeUI upgradeUI))
                upgradeUI.OnPlayClick += OnPlayClick;
        }

        if (CanvasMap.TryGetValue(EPanelType.Pause, out CanvasGroup pauseMenu))
        {
            if (pauseMenu.TryGetComponent(out PauseMenu pauseUI))
                pauseUI.OnGameResume += OnGameResume;
        }
    }

    private void OnGameResume()
    {
        ActivatePanel(EPanelType.Hud);
        DeactivatePanel(EPanelType.Pause);
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
            /*case ECarPart.All_Comp:
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
                break;*/
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

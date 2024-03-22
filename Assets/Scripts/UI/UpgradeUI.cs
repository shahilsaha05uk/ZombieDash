using EnumHelper;
using StructClass;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeUI : BaseWidget
{
    public delegate void FOnUpgradeSignature(ECarPart carComp, Upgrade upgradeStruct);
    public FOnUpgradeSignature OnUpgradeClick;

    [SerializeField] private Button btnPlay;

    [SerializeField] private Card mFuelCard;
    [SerializeField] private Card mNitroCard;
    [SerializeField] private Card mSpeedCard;
    
    private void Awake()
    {
        btnPlay.onClick.AddListener(OnPlay);
        
        mUiType = EUI.UPGRADE;
        mFuelCard.OnUpgradeButtonClick += OnUpgrade;
        mSpeedCard.OnUpgradeButtonClick += OnUpgrade;
        mNitroCard.OnUpgradeButtonClick += OnUpgrade;

    }

    private void OnPlay()
    {
        //TODO: Reset the Car Position
        DestroyWidget();
    }

    private void OnDestroy()
    {
        mSpeedCard.OnUpgradeButtonClick -= OnUpgrade;
        mNitroCard.OnUpgradeButtonClick -= OnUpgrade;
    }

    private void OnUpgrade(ECarPart carComp, Upgrade upgradeStruct)
    {
        OnUpgradeClick?.Invoke(carComp, upgradeStruct);
    }
}

using EnumHelper;
using StructClass;
using UnityEngine;

public class UpgradeUI : BaseWidget
{
    public delegate void FOnUpgradeSignature(ECarComponent carComp, FUpgradeStruct upgradeStruct);
    public FOnUpgradeSignature OnUpgradeClick;


    [SerializeField] private Card mSpeedCard;
    [SerializeField] private Card mNitroCard;
    
    private void Awake()
    {
        mUiType = EUI.UPGRADE;
        mSpeedCard.OnUpgradeButtonClick += OnUpgrade;
        mNitroCard.OnUpgradeButtonClick += OnUpgrade;

    }

    private void OnDestroy()
    {
        mSpeedCard.OnUpgradeButtonClick -= OnUpgrade;
        mNitroCard.OnUpgradeButtonClick -= OnUpgrade;
    }

    private void OnUpgrade(ECarComponent carComp, FUpgradeStruct upgradeStruct)
    {
        OnUpgradeClick?.Invoke(carComp, upgradeStruct);
    }
}

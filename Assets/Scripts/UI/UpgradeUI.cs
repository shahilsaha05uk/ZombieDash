using EnumHelper;
using StructClass;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeUI : BaseWidget
{
    public delegate void FOnUpgradeSignature(ECarPart carComp, FUpgradeStruct upgradeStruct);
    public FOnUpgradeSignature OnUpgradeClick;

    [SerializeField] private Button btnPlay;

    [SerializeField] private Card mSpeedCard;
    [SerializeField] private Card mNitroCard;
    
    private void Awake()
    {
        btnPlay.onClick.AddListener(OnPlay);
        
        mUiType = EUI.UPGRADE;
        mSpeedCard.OnUpgradeButtonClick += OnUpgrade;
        mNitroCard.OnUpgradeButtonClick += OnUpgrade;

    }

    private void OnPlay()
    {
        DestroyWidget();
    }

    private void OnDestroy()
    {
        mSpeedCard.OnUpgradeButtonClick -= OnUpgrade;
        mNitroCard.OnUpgradeButtonClick -= OnUpgrade;
    }

    private void OnUpgrade(ECarPart carComp, FUpgradeStruct upgradeStruct)
    {
        OnUpgradeClick?.Invoke(carComp, upgradeStruct);
    }
}

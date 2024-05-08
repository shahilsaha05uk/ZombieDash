using EnumHelper;
using StructClass;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeUI : BaseWidget
{
    public delegate void FOnPlayButtonCLickSignature();

    public FOnPlayButtonCLickSignature OnPlayClick;
    public delegate void FOnUpgradeSignature(ECarPart carComp, Upgrade upgradeStruct);
    public FOnUpgradeSignature OnUpgradeClick;

    [SerializeField] private Button btnPlay;
    [SerializeField] private TextMeshProUGUI mMoney;

    [SerializeField] private Card mFuelCard;
    [SerializeField] private Card mNitroCard;
    [SerializeField] private Card mSpeedCard;

    
    private void Awake()
    {
        btnPlay.onClick.AddListener(OnPlay);
        
        mUiType = EUI.UPGRADE;
        //mFuelCard.OnUpgradeButtonClick += OnUpgrade;
        //mSpeedCard.OnUpgradeButtonClick += OnUpgrade;
       // mNitroCard.OnUpgradeButtonClick += OnUpgrade;

    }
    protected override void OnEnable()
    {
        mMoney.text = "£" + ResourceComp.GetCurrentResources();
    }

    private void OnPlay()
    {
        Time.timeScale = 1f;
        OnPlayClick?.Invoke();
    }


}

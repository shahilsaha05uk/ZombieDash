using EnumHelper;
using StructClass;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeUI : BaseWidget
{
    public DA_UpgradeAsset mUpgradeAsset;
    public delegate void FOnPlayButtonCLickSignature();

    public FOnPlayButtonCLickSignature OnPlayClick;

    [SerializeField] private Button btnPlay;
    [SerializeField] private TextMeshProUGUI mMoney;
    
    private void Awake()
    {
        if (mUpgradeAsset)
            mUpgradeAsset.OnUpgradeRequested += OnUpgradeButtonClick;
        btnPlay.onClick.AddListener(OnPlay);
        
        mUiType = EUI.UPGRADE;
    }

    protected override void OnEnable()
    {
        UpdateMoney();
    }

    private void OnUpgradeButtonClick(ECarPart Part, int Index)
    {
        UpdateMoney();
    }

    private void UpdateMoney()
    {
        mMoney.text = "£" + ResourceComp.GetCurrentResources();
    }

    private void OnPlay()
    {
        Time.timeScale = 1f;
        OnPlayClick?.Invoke();
    }
}

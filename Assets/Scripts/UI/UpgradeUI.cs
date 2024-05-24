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
    [SerializeField] private Button btnMainMenu;
    [SerializeField] private TextMeshProUGUI mMoney;
    
    private void Awake()
    {
        btnPlay.onClick.AddListener(OnPlay);

        btnMainMenu.onClick.AddListener(OnMainMenuButtonClick);

        ResourceComp.OnResourceUpdated += UpdateMoney;
        mUiType = EUI.UPGRADE;
    }

    private void OnMainMenuButtonClick()
    {
        LevelManager.Instance.OpenAdditiveScene(ELevel.MENU, true);
    }

    private void OnEnable()
    {
        UpdateMoney(ResourceComp.GetCurrentResources());
    }

    private void UpdateMoney(int Value)
    {
        mMoney.text = "£" + Value;
    }

    private void OnPlay()
    {
        Time.timeScale = 1f;
        OnPlayClick?.Invoke();
    }
}

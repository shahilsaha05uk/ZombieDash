using System;
using System.Collections;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using EnumHelper;
using StructClass;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    public delegate void FOnUpgradeButtonClickSignature(ECarPart carComp, Upgrade upgradeStruct);

    public FOnUpgradeButtonClickSignature OnUpgradeButtonClick;
    
    [FormerlySerializedAs("CardComponent")] [SerializeField] private ECarPart cardPart;
    [SerializeField] private DA_UpgradeAsset UpgradeAsset;
    [SerializeField] private List<Upgrade> UpgradeList;

    [SerializeField] private Button btn;
    [SerializeField] private TextMeshProUGUI cost;

    private Upgrade mCurrentUpgrade;
    private int count = 0;

    private void Awake()
    {
        if (UpgradeAsset != null)
        {
            UpgradeList = new List<Upgrade>(UpgradeAsset.UpgradeList);
            ManageButtonInteractivity();
            btn.onClick.AddListener(OnCardButtonClick);
        }
    }

    private void OnCardButtonClick()
    {
        if((mCurrentUpgrade = GetNextUpgrade()) != null) OnUpgradeButtonClick?.Invoke(cardPart, mCurrentUpgrade);
        ManageButtonInteractivity();
    }

    private Upgrade GetNextUpgrade() { return UpgradeList[count++]; }
    private bool isUpgradeAvailable(){ return (count < UpgradeList.Count); }
    private void ManageButtonInteractivity() { btn.interactable = (isUpgradeAvailable()); } //TODO: Add the cost condition
}

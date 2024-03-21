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
    public delegate void FOnUpgradeButtonClickSignature(ECarPart carComp, FUpgradeStruct upgradeStruct);

    public FOnUpgradeButtonClickSignature OnUpgradeButtonClick;
    
    [FormerlySerializedAs("CardComponent")] [SerializeField] private ECarPart cardPart;
    [SerializeField] private DA_UpgradeAsset UpgradeAsset;
    
    [SerializeField] private Button btn;
    [SerializeField] private TextMeshProUGUI cost;

    private FUpgradeStruct mCurrentUpgrade;
    private void Awake()
    {
        btn.interactable = (UpgradeAsset != null && UpgradeAsset.isUpgradeAvailable());
        btn.onClick.AddListener(OnCardButtonClick);
    }

    private void OnCardButtonClick()
    {
        if(UpgradeAsset.GetNextUpgrade(out mCurrentUpgrade)) OnUpgradeButtonClick?.Invoke(cardPart, mCurrentUpgrade);
        btn.interactable = UpgradeAsset.isUpgradeAvailable();
    }

    private void UpdateCost(int value)
    {
        cost.text = value.ToString();
    }

}

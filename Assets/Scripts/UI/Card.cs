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
    public delegate void FOnUpgradeButtonClickSignature(ECarPart carComp, int UpgradeID);

    public FOnUpgradeButtonClickSignature OnUpgradeButtonClick;
    
    [SerializeField] private ECarPart cardPart;
    
    [SerializeField] private DA_UpgradeAsset UpgradeAsset;
    [SerializeField] private int TotalUpgrades;

    [SerializeField] private Button btn;
    [SerializeField] private TextMeshProUGUI cost;

    private int mCurrentIndex;

    private void Awake()
    {
        if (UpgradeAsset)
        {
            bool success = UpgradeAsset.GetUpgradeCount(cardPart, out TotalUpgrades);
            if(!success) return;
            btn.onClick.AddListener(OnCardButtonClick);
            mCurrentIndex = 0;
        }
        UpdateCardState();
    }

    private void OnCardButtonClick()
    {
        OnUpgradeButtonClick?.Invoke(cardPart, mCurrentIndex);
        TotalUpgrades--;
        
        UpdateCardState();
    }

    private void UpdateCardState()
    {
        if(UpgradeAsset == null) return;
        //int CurrentBalance = 0;
        Upgrade up = UpgradeAsset.GetUpgradeDetails(cardPart, mCurrentIndex);

        if (up != null)
        {
            // TODO: The Current Balance needs to be replaced with the player's money
            //btn.interactable= (up.cost < CurrentBalance)
        }
    }
}

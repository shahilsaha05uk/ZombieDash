using System;
using System.Collections;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using EnumHelper;
using StructClass;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    public delegate void FOnUpgradeButtonClickSignature(ECarComponent carComp, FUpgradeStruct upgradeStruct);

    public FOnUpgradeButtonClickSignature OnUpgradeButtonClick;
    
    [SerializeField] private ECarComponent CardComponent;
    [SerializeField] private DA_UpgradeAsset UpgradeAsset;
    
    [SerializeField] private Button btn;
    [SerializeField] private TextMeshProUGUI cost;
    
    private List<FUpgradeStruct> UpgradeList;
    private void Awake()
    {
        if (UpgradeAsset)
        {
            UpgradeList = new();
            UpgradeList = UpgradeAsset.UpgradeList;
            btn.interactable = (UpgradeList.Count > 0);
            btn.onClick.AddListener(OnCardButtonClick);
        }
        else
        {
            btn.interactable = false;
        }
    }


    private void OnCardButtonClick()
    {
        //TODO: On Button Click
        FUpgradeStruct up = GetNextUpgrade();
        OnUpgradeButtonClick?.Invoke(CardComponent, up);

        btn.interactable = (UpgradeList.Count > 0);

    }

    private void UpdateCost(int value)
    {
        cost.text = value.ToString();
    }

    private FUpgradeStruct GetNextUpgrade()
    {
        FUpgradeStruct up = (UpgradeList.Count > 0) ? UpgradeList[0] : default;
        UpgradeList.RemoveAt(0);
        return up;
    }
}

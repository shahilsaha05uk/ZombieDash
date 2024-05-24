using EnumHelper;
using StructClass;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    [SerializeField] private ECarPart cardPart;
    
    [SerializeField] private DA_UpgradeAsset mUpgradeAsset;
    [SerializeField] private int mTotalUpgrades;

    [SerializeField] private Button btn;
    [SerializeField] private TextMeshProUGUI txtCost;
    [SerializeField] private TextMeshProUGUI txtName;

    private int mCurrentIndex = 0;
    public bool bNonExhaustivePart;

    private void Awake()
    {
        ResourceComp.OnResourceUpdated += OnResourceUpdated;
        if (mUpgradeAsset)
        {
            bool success = mUpgradeAsset.GetUpgradeCount(cardPart, out mTotalUpgrades);
            if(!success) return;
            btn.onClick.AddListener(OnCardButtonClick);
            txtName.text = cardPart.ToString();
            mCurrentIndex = 0;
        }
        else
        {
            Debug.Log($"Upgrade asset is not available for the {cardPart} Button");
        }
    }
    private void OnDestroy()
    {
        ResourceComp.OnResourceUpdated -= OnResourceUpdated;
    }

    private void OnResourceUpdated(int CurrentBalance)
    {
        UpdateCardDetails();
    }

    private void OnEnable()
    {
        UpdateCardDetails();
    }

    private void OnCardButtonClick()
    {
        if (mUpgradeAsset == null || mCurrentIndex > mTotalUpgrades) return;

        var up = GetUpgrade();

        if (up == null) return;
        
        mUpgradeAsset.Trigger_UpgradeRequest(cardPart, mCurrentIndex);
        
        mCurrentIndex++;
        ResourceComp.SubtractResources(up.cost);

    }
    private void UpdateCardDetails()
    {
        var CurrentUpgrade = GetUpgrade();
        if (CurrentUpgrade != null)
        {
            int upCost = CurrentUpgrade.cost;
            txtCost.text = upCost.ToString();
            ToggleCard((upCost < ResourceComp.GetCurrentResources()), false);   // activate the button if the upgrade cost is less than the current balance
        }
        else
        {
            txtCost.text = "(MAX)";
            ToggleCard(false, true);
        }
    }

    private BaseUpgrade GetUpgrade()
    {
        BaseUpgrade up;
        if (!bNonExhaustivePart)
            up = mUpgradeAsset.GetUpgradeDetails(cardPart, mCurrentIndex);
        else
            up = mUpgradeAsset.GetNonExhaustiveUpgradeDetails(cardPart, mCurrentIndex);

        return up;
    }
    private void ToggleCard(bool Activate, bool removeListeners)
    {
        btn.interactable = Activate;

        if(removeListeners)
            btn.onClick.RemoveAllListeners();
    }
}

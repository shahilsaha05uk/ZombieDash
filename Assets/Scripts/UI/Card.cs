using EnumHelper;
using StructClass;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    [SerializeField] private ECarPart cardPart;
    
    [SerializeField] private DA_UpgradeAsset UpgradeAsset;
    [SerializeField] private int TotalUpgrades;

    [SerializeField] private Button btn;
    [SerializeField] private TextMeshProUGUI cost;

    private int mCurrentIndex = 0;

    private void Awake()
    {
        if (UpgradeAsset)
        {
            bool success = UpgradeAsset.GetUpgradeCount(cardPart, out TotalUpgrades);
            if(!success) return;
            btn.onClick.AddListener(OnCardButtonClick);
            mCurrentIndex = 0;
        }
        else
        {
            Debug.Log($"Upgrade asset is not available for the {cardPart} Button");
        }
    }
    private void OnEnable()
    {
        UpdateCardDetails();
    }

    private void OnCardButtonClick()
    {
        int cost = UpgradeAsset.GetUpgradeDetails(cardPart, mCurrentIndex).cost;
        ResourceComp.SubtractResources(cost);

        UpgradeAsset.Trigger_UpgradeRequest(cardPart, mCurrentIndex);

        TotalUpgrades--;
        mCurrentIndex++;

        UpdateCardDetails();
    }
    private void UpdateCardDetails()
    {
        if(UpgradeAsset == null) return;
        Upgrade up = UpgradeAsset.GetUpgradeDetails(cardPart, mCurrentIndex);

        if (up != null)
        {
            cost.text = up.cost.ToString();
            btn.interactable = (up.cost < ResourceComp.GetCurrentResources());
        }
        else
        {
            cost.text = "(MAX)";
            btn.interactable = false;
            btn.onClick.RemoveAllListeners();
        }
    }
}

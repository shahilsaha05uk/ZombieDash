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
    public bool bNonExhaustivePart;

    private BaseUpgrade upgrade;

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
        upgrade = GetUpgrade();
        UpdateCardDetails();
    }

    private void OnCardButtonClick()
    {
        if (UpgradeAsset == null) return;
        ResourceComp.SubtractResources(GetCost(upgrade));

        UpgradeAsset.Trigger_UpgradeRequest(cardPart, mCurrentIndex);

        TotalUpgrades--;
        mCurrentIndex++;

        UpdateCardDetails();
    }
    private void UpdateCardDetails()
    {
        if (upgrade != null)
        {
            int upCost = GetCost(upgrade);
            cost.text = upCost.ToString();
            btn.interactable = (upCost < ResourceComp.GetCurrentResources());
        }
        else MaxUpgradeReached();
    }

    private BaseUpgrade GetUpgrade()
    {
        BaseUpgrade up;
        if (!bNonExhaustivePart)
            up = UpgradeAsset.GetUpgradeDetails(cardPart, mCurrentIndex);
        else
            up = UpgradeAsset.GetNonExhaustiveUpgradeDetails(cardPart, mCurrentIndex);

        return up;
    }
    private int GetCost(BaseUpgrade up)
    {
        return (up == null)? 0 : up.cost;
    }
    private void MaxUpgradeReached()
    {
        cost.text = "(MAX)";
        btn.interactable = false;
        btn.onClick.RemoveAllListeners();
    }
}

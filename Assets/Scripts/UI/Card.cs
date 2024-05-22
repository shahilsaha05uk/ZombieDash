using EnumHelper;
using StructClass;
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

    private BaseUpgrade upgrade;

    private void Awake()
    {
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
    private void OnEnable()
    {
        upgrade = GetUpgrade();
        UpdateCardDetails();
    }

    private void OnCardButtonClick()
    {
        if (mUpgradeAsset == null) return;
        ResourceComp.SubtractResources(GetCost(upgrade));

        mUpgradeAsset.Trigger_UpgradeRequest(cardPart, mCurrentIndex);

        mTotalUpgrades--;
        mCurrentIndex++;

        UpdateCardDetails();
    }
    private void UpdateCardDetails()
    {
        if (upgrade != null)
        {
            int upCost = GetCost(upgrade);
            txtCost.text = upCost.ToString();
            btn.interactable = (upCost < ResourceComp.GetCurrentResources());
        }
        else MaxUpgradeReached();
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
    private int GetCost(BaseUpgrade up)
    {
        return (up == null)? 0 : up.cost;
    }
    private void MaxUpgradeReached()
    {
        txtCost.text = "(MAX)";
        btn.interactable = false;
        btn.onClick.RemoveAllListeners();
    }
}

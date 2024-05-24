using System;
using EnumHelper;
using Helpers;
using Interfaces;
using StructClass;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ReviewPanel : BaseWidget
{
    
    [FormerlySerializedAs("mTotalDistance")] public Field mDistanceCoveredField;
    [FormerlySerializedAs("mDistanceLeft")] public Field mDistanceLeftField;
    [FormerlySerializedAs("mDifference")] public Field mDifferenceField;
    [FormerlySerializedAs("mMoneyCollected")] public Field mEarningField;

    [SerializeField] private Button mDayCompleteButton;
    [SerializeField] private Button mMainMenuButton;
    
    private BaseAnimatedUI mAnimUI;

    private void OnEnable()
    {
        if (TryGetComponent(out mAnimUI))
        {
            mAnimUI.StartAnim(EAnimDirection.Forward);
        }
        UpdateValues();
    }

    private void Start()
    {
        mDayCompleteButton.onClick.AddListener(OnDayCompleteButtonClick);
        mMainMenuButton.onClick.AddListener(OnMenuButtonClick);
    }


    public void OnDayCompleteButtonClick()
    {
        mAnimUI.StartAnim(EAnimDirection.Backward);
        GameManager.Instance.DayComplete();
    }
    public void OnMenuButtonClick()
    {
        LevelManager.Instance.OpenAdditiveScene(ELevel.MENU, true);
    }
    
    public void UpdateValues()
    {
        if (SaveDataManager.GetPlayerData(out FPlayerData pData))
        {
            // Update the HUD
            string distanceCovered = $"{pData.DistanceCovered} / {pData.TotalDistance}";
            mDistanceCoveredField.UpdateText(distanceCovered);
            mDifferenceField.UpdateText(pData.DistanceDifference.ToString());

            int balanceBeforeAdd = ResourceComp.GetCurrentResources() - pData.AddedBalance;
            string resourceAdded = $"{balanceBeforeAdd} + {pData.AddedBalance}";
            mEarningField.UpdateText(resourceAdded);
        }
    }
}
 
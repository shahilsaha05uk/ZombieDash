using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using EnumHelper;
using Helpers;
using UnityEngine;
using UnityEngine.UI;

public class ReviewPanel : MonoBehaviour
{
    public Field mTotalDistance;
    public Field mLastDistance;
    public Field mDifference;
    public Field mMoneyCollected;

    private void OnEnable()
    {
        UpdateValues();
    }

    public void OnDayCompleteButtonClick()
    {
        GameManager.Instance.DayComplete();
    }

    public void OnMenuButtonClick()
    {
        LevelManager.Instance.OpenAdditiveScene(ELevel.MENU, true);
    }

    public void UpdateValues()
    {
        string path = "Assets/jsons/gameData.json";

        if (File.Exists(path))
        {
            string loadPlayerData = File.ReadAllText(path);
            var pData = JsonUtility.FromJson<PlayerData>(loadPlayerData);
            
            // Update the HUD
            mTotalDistance.UpdateText(pData.TotalDistance.ToString());
            mLastDistance.UpdateText(pData.LastDistance.ToString());
            mDifference.UpdateText(pData.DistanceDifference.ToString());
        }
        // mTotalDistance.UpdateText();
       // mLastDistance.UpdateText();
       // mDifference.UpdateText();
       // mMoneyCollected.UpdateText();
    }
}
 
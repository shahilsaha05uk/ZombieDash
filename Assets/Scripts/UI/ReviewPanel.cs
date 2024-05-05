using System;
using System.Collections;
using System.Collections.Generic;
using EnumHelper;
using Helpers;
using UnityEngine;
using UnityEngine.UI;

public class ReviewPanel : MonoBehaviour
{
    public void OnDayCompleteButtonClick()
    {
        GameManager.Instance.DayComplete();
    }

    public void OnMenuButtonClick()
    {
        LevelManager.Instance.OpenAdditiveScene(ELevel.MENU, true);
    }
}
 
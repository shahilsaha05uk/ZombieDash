using System;
using System.Collections;
using System.Collections.Generic;
using EnumHelper;
using Helpers;
using UnityEngine;
using UnityEngine.UI;

public class ReviewPanel : MonoBehaviour
{
    public void OnShopButtonClick()
    {
        LevelManager.Instance.OpenAdditiveScene(ELevel.UPGRADE, true);
    }

    public void OnMenuButtonClick()
    {
        LevelManager.Instance.OpenAdditiveScene(ELevel.MENU, true);
    }
}
 
using System.Collections;
using System.Collections.Generic;
using EnumHelper;
using UnityEngine;

public class GameComplete : MonoBehaviour
{
    public void OnMenuButtonClick()
    {
        LevelManager.Instance.OpenAdditiveScene(ELevel.MENU, true);
    }
}

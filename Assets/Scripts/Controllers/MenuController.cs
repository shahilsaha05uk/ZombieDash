using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuController : BaseController
{
    protected override void InitController()
    {
        base.InitController();

        UIManager uIManager = UIManager.Instance;
        if (uIManager != null)
        {
            BaseWidget widget = uIManager.SpawnWidget(EnumHelper.EUI.MAIN_MENU, true);
            LevelManager.Instance.MoveGameObjectToCurrentScene(widget.gameObject);
        }
    }
}
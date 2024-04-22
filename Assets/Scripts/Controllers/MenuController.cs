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
            Debug.Log("Manager is valid");  // Check passed
            BaseWidget widget = uIManager.SpawnWidget(EnumHelper.EUI.MAIN_MENU, true);
            LevelManager.MoveGameObjectToCurrentScene(widget.gameObject);
        }
    }
}
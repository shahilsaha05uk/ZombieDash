using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuController : BaseController
{
    private void Start()
    {
        UIManager.Instance.InitialiseWidget(EnumHelper.EUI.MAIN_MENU, true);
    }
}

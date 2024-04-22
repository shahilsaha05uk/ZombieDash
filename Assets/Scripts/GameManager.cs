using System;
using System.Collections;
using System.Collections.Generic;
using EnumHelper;
using UnityEngine;

public class GameManager : ParentManager
{
    public static GameManager Instance { get; private set; }

    protected override void InitManager()
    {
        base.InitManager();

        if (Instance == null) Instance = this;
    }

    public static APlayerStart GetPlayerStart()
    {
        return FindObjectOfType<APlayerStart>();
    }
}

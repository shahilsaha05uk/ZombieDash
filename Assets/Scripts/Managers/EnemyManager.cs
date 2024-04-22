using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : ParentManager
{
    public static EnemyManager Instance { get; private set; }
    protected override void InitManager()
    {
        base.InitManager();

        if (Instance == null) Instance = this;
    }
}

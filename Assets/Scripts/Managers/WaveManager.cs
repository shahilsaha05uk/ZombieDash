using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : ParentManager
{
    public static WaveManager Instance { get; private set; }

    protected override void InitManager()
    {
        base.InitManager();

        if(Instance == null) Instance = this;
    }
}

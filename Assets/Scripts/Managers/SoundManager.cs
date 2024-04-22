using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : ParentManager
{
    public static SoundManager Instance { get; private set; }
    protected override void InitManager()
    {
        base.InitManager();

        if (Instance == null) Instance = this;
    }

}

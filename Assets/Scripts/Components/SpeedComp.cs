using EnumHelper;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeedComp : CarComponent
{
    protected override void Start()
    {
        mPart = ECarPart.Speed;
        bIsExhaustiveComponent = false;
        base.Start();
    }
}

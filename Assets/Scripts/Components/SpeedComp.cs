using EnumHelper;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeedComp : CarComponent
{
    public float MaxSpeed;

    protected override void Start()
    {
        base.Start();
        mPart = ECarPart.Speed;
    }
}

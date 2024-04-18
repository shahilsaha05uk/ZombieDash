using EnumHelper;
using StructClass;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class FuelComp : CarComponent
{
    private float minSpeed = 0.05f;

    protected override void Start()
    {
        mPart = ECarPart.Fuel;
        base.Start();
    }

    private void FixedUpdate()
    {
        Vector2 vel = carRb.velocity;
        vel.Normalize();

        // check if the car's velocity is more than the required
        if(vel.x > minSpeed)
        {
            Consume();
        }
    }
    private void Consume()
    {
        if (mCurrent <= 0f) return;
        UpdateValue(EValueUpdateType.Decrease);
    }
}

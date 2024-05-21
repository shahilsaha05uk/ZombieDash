using EnumHelper;
using StructClass;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class FuelComp : CarComponent
{
    [Tooltip("This is the minimum speed after which the car will consume fuel")]
    [SerializeField] private float minSpeed = 0.05f;

    [SerializeField] private float mDecreaseRateOnNitroActivated;
    private float mDefaultDecreaseRate;

    protected override void Start()
    {
        mDefaultDecreaseRate = mDecreaseRate;
        mPart = ECarPart.Fuel;
        base.Start();
        mCarRef.OnNitroToggled += OnNitroToggled;

        StartComponent();
    }

    // This will manipulate the decrease rate if the player is using nitro
    private void OnNitroToggled(bool Value)
    {
        mDecreaseRate = (Value == true)? mDecreaseRateOnNitroActivated :mDefaultDecreaseRate;
    }

    public override void OnReset()
    {
        base.OnReset();
        StartComponent();
    }

    public override void StartComponent()
    {
        base.StartComponent();
        mComponentCoroutine= StartCoroutine(Consume());
    }

    private IEnumerator Consume()
    {
        WaitForSeconds timeInterval = new WaitForSeconds(mTimeInterval);
        while (mCurrent > 0.0f)
        {
            Vector2 vel = mCarRb.velocity;
            float speedX = vel.x; // Calculate the absolute value of the x component of velocity

            if (speedX > minSpeed)
            {
                UpdateValue(EValueUpdateType.Decrease); // Consume fuel only when the x-axis speed is beyond the required minimum
            }
            yield return timeInterval;
        }
    }
}

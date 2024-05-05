using EnumHelper;
using StructClass;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class FuelComp : CarComponent
{
    [Tooltip("This is the minimum speed after which the car will consume fuel")]
    [SerializeField] private float minSpeed = 0.05f;

    protected override void Start()
    {
        mPart = ECarPart.Fuel;
        base.Start();
        
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
            Vector2 vel = carRb.velocity;
            float speedX = Mathf.Abs(vel.x); // Calculate the absolute value of the x component of velocity
            if (speedX > minSpeed)
            {
                UpdateValue(EValueUpdateType.Decrease); // Consume fuel only when the x-axis speed is beyond the required minimum
            }
            yield return timeInterval;
        }
    }
}

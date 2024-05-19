using EnumHelper;
using StructClass;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class NitroComp : CarComponent
{
    [SerializeField] protected Rigidbody2D frontTireRb;
    [SerializeField] protected Rigidbody2D backTireRb;

    private CarManager mCarManager;
    private CheckGroundClearance mGroundClearanceComp;

    [SerializeField] private float mNitroImpulseOnWheels = 5000f;
    [SerializeField] private float mNitroImpulseOnCarRb = 5f;
    protected override void Start()
    {
        mPart = ECarPart.Nitro;
        mCarManager = GetComponent<CarManager>();
        mGroundClearanceComp = GetComponent<CheckGroundClearance>();
        base.Start();
    }
    
    public override void StartComponent()
    {
        base.StartComponent();
        mComponentCoroutine= StartCoroutine(Boost());
    }
    public override void StopComponent()
    {
        base.StopComponent();
    }
    private IEnumerator Boost()
    {
        if (mCurrent <= 0f) yield break;

        WaitForSeconds timeInterval = new WaitForSeconds(mTimeInterval);
        while (mCurrent > 0f)
        {
            UpdateValue(EValueUpdateType.Decrease);     // called in order to update the HUD

            // add force on the car only when the car is in the air; else add the force on the wheels
            if (!mGroundClearanceComp.bIsOnGround || mCarManager.VelocityMag < 10f)
            {
                carRb.AddForce(transform.right * (mNitroImpulseOnCarRb), ForceMode2D.Force);
            }
            else
            {
                float torqueVal = -mNitroImpulseOnWheels * Time.deltaTime;

                frontTireRb.AddTorque(torqueVal, ForceMode2D.Impulse);
                backTireRb.AddTorque(torqueVal, ForceMode2D.Impulse);
            }

            yield return timeInterval;
        }
    }

}

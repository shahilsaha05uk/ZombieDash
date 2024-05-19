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

    [SerializeField] private float mNitroImpulseInAir;
    [SerializeField] private float mNitroImpulseOnCarRb;
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
            float impulseVal = mGroundClearanceComp.bIsOnGround ? mNitroImpulseOnCarRb : mNitroImpulseInAir;
            UpdateValue(EValueUpdateType.Decrease);     // called in order to update the HUD

            carRb.AddForce(transform.right * (impulseVal), ForceMode2D.Force);

            yield return timeInterval;
        }
    }

}

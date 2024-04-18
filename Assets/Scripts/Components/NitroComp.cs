using EnumHelper;
using StructClass;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class NitroComp : CarComponent
{
    [SerializeField] private GameObject mNitro;

    [SerializeField] private float mNitroImpulse = 100f;

    protected override void Start()
    {
        mPart = ECarPart.Nitro;
        base.Start();
    }

    public override void StartComponent()
    {
        base.StartComponent();
        StartCoroutine(Boost());
    }

    private IEnumerator Boost()
    {
        if (mCurrent <= 0f) yield break;

        WaitForSeconds timeInterval = new WaitForSeconds(mTimeInterval);

        Vector2 nitroThrustPos = mNitro.transform.right * -1f;
        while (mCurrent > 0f)
        {
            carRb.AddForce(nitroThrustPos * mNitroImpulse, ForceMode2D.Force);

            UpdateValue(EValueUpdateType.Decrease);
            yield return timeInterval;
        }

    }

}

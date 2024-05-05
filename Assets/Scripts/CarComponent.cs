using System;
using EnumHelper;
using UnityEngine;

public abstract class CarComponent : MonoBehaviour
{
    /*NOTE: Every Value needs to be NORMALIZED!!!*/
    private float mInitial = 1f;
    [SerializeField] private Car mCarRef;

    [SerializeField] protected Rigidbody2D carRb;
    [SerializeField] protected float mCurrent = 1f;
    [SerializeField] protected float mLast = 0f;
    [SerializeField] protected float mDecreaseRate = 0.01f;
    [SerializeField] protected float mTolerance = 0.01f;
    [SerializeField] protected float mTimeInterval = 0.001f;
    [SerializeField] protected ECarPart mPart;
    [SerializeField] protected bool isExhaustiveComponent;
    public delegate void FOnRunningOutOfResourceSignature(ECarPart resource);
    public static FOnRunningOutOfResourceSignature OnRunningOutOfResources;

    protected Coroutine mComponentCoroutine;

    protected virtual void Start()
    {
        carRb = GetComponent<Rigidbody2D>();
        if (mCarRef)
        {
            mCarRef.RegisterComponent(mPart, this);
            mCarRef.UpdateCarMetrics(mPart, mCurrent);

            if (isExhaustiveComponent)
            {
                mCarRef.RegisterExhaustiveComponent(mPart, false);
            }
        }
    }

    public virtual void ResetComponent()
    {
        mCurrent = mInitial;
        mLast = 0f;
        mCarRef.UpdateCarMetrics(mPart, mCurrent);
        if (isExhaustiveComponent)
        {
            mCarRef.UpdateExhaustiveComponent(mPart, false);
        }
    }

    public virtual void StartComponent()
    {
        if (mComponentCoroutine != null) return;
    }
    public virtual void StopComponent()
    {
        if (mComponentCoroutine == null) return;
        
        StopCoroutine(mComponentCoroutine);
        mComponentCoroutine= null;
    }
    public void UpdateValue(EValueUpdateType updateType)
    {
        switch (updateType)
        {
            case EValueUpdateType.Increase: break;
            case EValueUpdateType.Decrease: DecreaseComponentValue(); break;
        }
        OnCarComponentUpdate(mPart, mCurrent);

        if (mCurrent <= 0f) PartExhaust();
    }

    protected virtual void DecreaseComponentValue()
    {
        if(mCurrent > 0f)
        {
            mCurrent -= mDecreaseRate;
        }
    }
    private void OnCarComponentUpdate(ECarPart carPart, float value)
    {
        float tolerance = mTolerance;

        var difference = Mathf.Abs(value - mLast);
        if (difference > tolerance || value <= 0.0f)
        {
            mLast = value;
            mCarRef.UpdateCarMetrics(carPart, value);
        }
    }

    protected virtual void PartExhaust()
    {
        if(mCurrent <= 0f) {
            OnRunningOutOfResources?.Invoke(mPart);
        }
    }
}

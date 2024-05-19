using System;
using System.Runtime.CompilerServices;
using EnumHelper;
using Interfaces;
using UnityEngine;

public abstract class CarComponent : MonoBehaviour, IResetInterface
{
    /*NOTE: Every Value needs to be NORMALIZED!!!*/
    
    public Car mCarRef { private set; get; }
    public Rigidbody2D carRb { private set; get; }

    private float mInitial = 1f;
    protected float mCurrent = 1f;
    protected float mLast = 0f;
    
    [SerializeField] protected float mDecreaseRate = 0.01f;
    [SerializeField] protected float mTolerance = 0.01f;
    [SerializeField] protected float mTimeInterval = 0.001f;
    protected ECarPart mPart;
    [SerializeField] protected bool isExhaustiveComponent;
    
    public bool mHasExhausted { private set; get; }

    protected Coroutine mComponentCoroutine;

    [SerializeField] private DA_UpgradeAsset mUpgradeAsset;

    protected virtual void Start()
    {
        if (mUpgradeAsset)
        {
            mUpgradeAsset.OnUpgradeRequested += OnUpgradeRequest;
        }

        carRb = GetComponent<Rigidbody2D>();
        mCarRef = GetComponent<Car>();
        
        if (mCarRef)
        {
            mCarRef.RegisterComponent(mPart, this);
            mCarRef.UpdateCarMetrics(mPart, mCurrent);
            GameManager.OnResetLevel += OnReset;
        }
        mHasExhausted = false;
    }


    public virtual void OnReset()
    {
        mHasExhausted = false;
        mCurrent = mInitial;
        mLast = 0f;
        mCarRef.UpdateCarMetrics(mPart, mCurrent);
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
        if(mCurrent <= 0f)
        {
            mHasExhausted = true;
        }
    }
    private void OnUpgradeRequest(ECarPart Part, int Index)
    {
        var up = mUpgradeAsset.GetUpgradeDetails(Part, Index);
        if(up != null)
        {
            mDecreaseRate = up.DecreaseRate;
        }
    }
}

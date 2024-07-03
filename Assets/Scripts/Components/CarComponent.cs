using System;
using System.Runtime.CompilerServices;
using EnumHelper;
using Interfaces;
using UnityEngine;
using UnityEngine.Serialization;

public abstract class CarComponent : MonoBehaviour, IResetInterface
{
    // delegate
    public delegate void FOnNonExhaustiveCarComponentUpgradeSignature(float Value, ECarPart part);

    public static FOnNonExhaustiveCarComponentUpgradeSignature OnNonExhaustiveCarComponentUpgrade;
    
    // publics
    public Car mCarRef { private set; get; }
    public Rigidbody2D mCarRb { private set; get; }
    public bool bHasExhausted { private set; get; }
    public int mValue { private set; get; }

    // privates
    private float mInitial = 1f;
    
    // protected
    [SerializeField] protected DA_UpgradeAsset mUpgradeAsset;
    protected Coroutine mComponentCoroutine;
    protected float mCurrent = 1f;
    protected float mLast;
    protected ECarPart mPart;
    [SerializeField] protected float mDecreaseRate ;
    [SerializeField] protected float mTolerance;
    [SerializeField] protected float mTimeInterval;
    protected bool bIsExhaustiveComponent;
    
    protected virtual void Start()
    {
        if (mUpgradeAsset)
        {
            mUpgradeAsset.OnUpgradeRequested += OnUpgradeRequest;
        }

        mCarRb = GetComponent<Rigidbody2D>();
        mCarRef = GetComponent<Car>();
        
        if (mCarRef)
        {
            mCarRef.RegisterComponent(mPart, this);
            GameManager.OnResetLevel += OnReset;
        }
        bHasExhausted = false;
    }
    
    public virtual void OnReset()
    {
        bHasExhausted = false;
        mCurrent = mInitial;
        mLast = 0f;
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
        }
    }
    protected virtual void PartExhaust()
    {
        if(mCurrent <= 0f)
        {
            bHasExhausted = true;
        }
    }
    protected virtual void OnUpgradeRequest(ECarPart Part, int Index)
    {
        if (!bIsExhaustiveComponent)
        {
            var up = mUpgradeAsset.GetNonExhaustiveUpgradeDetails(Part, Index);
            if (up != null)
            {
                mValue = up.Value;
                OnNonExhaustiveCarComponentUpgrade?.Invoke(mValue, Part);
            }
        }
        else
        {
            var up = mUpgradeAsset.GetUpgradeDetails(Part, Index);
            if (up != null)
            {
                mDecreaseRate = up.DecreaseRate;
            }
        }
    }
}

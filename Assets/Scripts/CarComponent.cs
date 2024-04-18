using EnumHelper;
using UnityEngine;

public abstract class CarComponent : MonoBehaviour
{
    /*NOTE: Every Value needs to be NORMALIZED!!!*/

    [SerializeField] private Car mCarRef;

    [SerializeField] protected Rigidbody2D carRb;

    [SerializeField] protected float mCurrent = 1f;
    [SerializeField] protected float mDecreaseRate = 0.01f;
    [SerializeField] protected float mTolerance = 0.01f;
    [SerializeField] protected float mTimeInterval = 0.001f;
    [SerializeField] protected ECarPart mPart;

    public delegate void FOnRunningOutOfResourceSignature(ECarPart resource);
    public static FOnRunningOutOfResourceSignature OnRunningOutOfResources;
    

    protected virtual void Start()
    {
        carRb = GetComponent<Rigidbody2D>();
        mCarRef.UpdateCarMetrics(mPart, mCurrent);
    }

    public virtual void StartComponent()
    {

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
        float tollerance = mTolerance, hudVal = mCarRef.mCarMetrics.getValue(carPart);

        var difference = Mathf.Abs(value - hudVal);
        if (difference > tollerance || value <= 0.0f)
        {
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

using System;
using System.Collections;
using System.Collections.Generic;
using EnumHelper;
using StructClass;
using UnityEngine;

public class CarComponent : MonoBehaviour
{
    /*NOTE: Every Value needs to be NORMALIZED!!!*/
    
    public delegate void FOnRunningOutOfResourceSignature(ECarComponent resource);
    public FOnRunningOutOfResourceSignature OnRunningOutOfResources;

    public delegate void FOnCarComponentUpdate(ECarComponent carComponent, float Value);
    public FOnCarComponentUpdate OnComponentUpdated;
    
    [Space(10)][Header("Development")]
    [SerializeField] private float mHudUpdateTimeInterval = 0.1f;
    [SerializeField] private Rigidbody2D carRb;

    [Space(5)]
    //[SerializeField] private FHudValues mHudValues;

    [Space(10)] [Header("Car Feature Modification")] 
    [HideInInspector] public float mCurrentFuel = 1f;
    [HideInInspector] public float mCurrentNitro = 1f;
    
    [Space(5)] [Header("Fuel")] 
    [SerializeField] private float mFuelDecreaseRate = 0.1f;
    [SerializeField] private float mFuelDecreaseInterval = 0.01f;
    [SerializeField] private float mFuelTolerance = 0.001f;
                     private bool mShouldConsumeFuel = false;
    
    [Space(5)] [Header("Nitro")]
    [SerializeField] private float mNitroDecreaseRate = 0.1f;
    [SerializeField] private float mNitroDecreaseInterval = 0.01f;
    [SerializeField] private float mNitroTolerance = 0.001f;
                     private bool mShouldConsumeNitro = false;
    

    private IEnumerator HudUpdater()
    {
        WaitForSeconds timeInterval = new WaitForSeconds(mHudUpdateTimeInterval);
        while (true)
        {
            yield return timeInterval;

            /*
            float distanceDifference = Vector2.Distance(mHudValues.position, transform.position);
            float speedDifference = Mathf.Abs(mHudValues.speed - carRb.velocity.magnitude);
            float fuelDifference = mCurrentFuel - mHudValues.fuel;
            
            if (distanceDifference > mInvokeTolerance || 
                speedDifference > mInvokeTolerance || 
                fuelDifference > mFuelTolerance)
            {
                //TODO: Invoke the event
                Debug.Log("Invoke the event to update the HUD");
               // OnCarStatusUpdate?.Invoke(mHudValues);
            }
        */
        }
    }

    public void StartFuelConsumption()
    {
        mShouldConsumeFuel = true;
        StartCoroutine(UpdateFuel());
    }

    public void StopFuelConsumption()
    {
        mShouldConsumeFuel = false;
    }

    public void StartNitroConsumption()
    {
        mShouldConsumeNitro = true;
        StartCoroutine(UpdateNitro());
    }

    public void StopNitroConsumption()
    {
        mShouldConsumeNitro = false;
    }

    private IEnumerator UpdateFuel()
    {
        WaitForSeconds timeInterval = new WaitForSeconds(mFuelDecreaseInterval);
        while (mCurrentFuel >= 0.0f && mShouldConsumeFuel)
        {
            mCurrentFuel -= mFuelDecreaseRate;
            OnComponentUpdated?.Invoke(ECarComponent.Fuel, mCurrentFuel);
            DebugUI.OnFuelUpdate?.Invoke(mCurrentFuel);

            yield return timeInterval;
        }
        OnComponentUpdated?.Invoke(ECarComponent.Fuel, mCurrentFuel);
        if (mCurrentFuel <= 0.0f)
        {
            //TODO: Call for the Game Update
            OnRunningOutOfResources?.Invoke(ECarComponent.Fuel);
            Debug.Log("Out of Fuel");
        }
    }

    private IEnumerator UpdateNitro()
    {
        WaitForSeconds timeInterval = new WaitForSeconds(mNitroDecreaseInterval);
        while (mCurrentNitro > 0f && mShouldConsumeNitro)
        {
            mCurrentNitro -= mFuelDecreaseRate;
            OnComponentUpdated?.Invoke(ECarComponent.Nitro, mCurrentNitro);
            DebugUI.OnNitroUpdate.Invoke(mCurrentNitro);

            yield return timeInterval;
        }
    }

}

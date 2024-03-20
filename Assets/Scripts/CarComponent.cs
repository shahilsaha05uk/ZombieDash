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
    
    [Space(10)] [Header("Car Feature Modification")] 
    [HideInInspector] public float mCurrentFuel = 1f;
    [HideInInspector] public float mCurrentNitro = 1f;
    
    [Space(5)] [Header("Fuel")] 
    [SerializeField] private float mFuelDecreaseRate = 0.1f;
    [SerializeField] private float mFuelDecreaseInterval = 0.01f;
                     private bool mShouldConsumeFuel = false;
    
    [Space(5)] [Header("Nitro")]
    private bool mShouldConsumeNitro = false;


    public void StartFuelConsumption()
    {
        mShouldConsumeFuel = true;
        StartCoroutine(UpdateFuel());
    }

    public void StopFuelConsumption()
    {
        mShouldConsumeFuel = false;
    }

    /*
    public void StartNitroConsumption()
    {
        mShouldConsumeNitro = true;
        StartCoroutine(UpdateNitro());
    }

    public void StopNitroConsumption()
    {
        mShouldConsumeNitro = false;
    }
    */

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

    public void UpdateNitro()
    {
        if (mCurrentNitro >= 0f)
        {
            mCurrentNitro -= mFuelDecreaseRate;
            OnComponentUpdated?.Invoke(ECarComponent.Nitro, mCurrentNitro);
            DebugUI.OnNitroUpdate.Invoke(mCurrentNitro);
        }
        else
        {
            //TODO: Call for the Game Update
            OnRunningOutOfResources?.Invoke(ECarComponent.Nitro);
            Debug.Log("Out of Nitro");
        }

    }

}

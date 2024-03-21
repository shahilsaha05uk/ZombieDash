using System;
using System.Collections;
using System.Collections.Generic;
using EnumHelper;
using StructClass;
using UnityEngine;


[System.Serializable]
public struct FCarPart
{
    public float current;
    public float decreaseRate;
}
public class CarComponent : MonoBehaviour
{
    /*NOTE: Every Value needs to be NORMALIZED!!!*/
    
    public delegate void FOnRunningOutOfResourceSignature(ECarPart resource);
    public FOnRunningOutOfResourceSignature OnRunningOutOfResources;
    public delegate void FOnCarComponentUpdate(ECarPart carPart, float Value);
    public FOnCarComponentUpdate OnComponentUpdated;
    
    [Space(5)] [Header("Value Rate")] 
    [SerializeField] private float mFuelDecreaseRate = 0.1f;
    [SerializeField] private float mNitroDecreaseRate = 0.1f;

    private IDictionary<ECarPart, FCarPart> mCurrentPartValueMap;

    private void Awake()
    {
        mCurrentPartValueMap = new Dictionary<ECarPart, FCarPart>()
        {
            { ECarPart.Fuel, new FCarPart() { current = 1f, decreaseRate = mFuelDecreaseRate } },
            { ECarPart.Nitro, new FCarPart() { current = 1f, decreaseRate = mNitroDecreaseRate } }
        };
    }

    public void UpdateValue(ECarPart partType)
    {
        FCarPart part = mCurrentPartValueMap[partType];
        if (part.current >= 0.0f)
        {
            part.current -= part.decreaseRate;
            mCurrentPartValueMap[partType] = part;
        }
        else OnRunningOutOfResources?.Invoke(partType);
        DebugUI.OnValueUpdate?.Invoke(part.current, partType);
        OnComponentUpdated?.Invoke(partType, part.current);
    }

    public float GetCurrentPartValue(ECarPart PartType, out bool isValid)
    {
        isValid = (mCurrentPartValueMap.TryGetValue(PartType, out var value));
        return value.current;
    }

    public void UpgradePart(ECarPart carcomp, FUpgradeStruct upgradestruct)
    {
        switch (carcomp)
        {
            case ECarPart.Fuel:
                mFuelDecreaseRate = upgradestruct.Value;
                break;
            case ECarPart.Nitro:
                mNitroDecreaseRate = upgradestruct.Value;
                break;
        }
    }
}

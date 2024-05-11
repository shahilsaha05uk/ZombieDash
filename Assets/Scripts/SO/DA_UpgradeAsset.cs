using System.Collections;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using EnumHelper;
using StructClass;
using UnityEngine;

[CreateAssetMenu(menuName = "Car", fileName = "DataAssets/DA_Upgrade", order = 2)]
public class DA_UpgradeAsset : ScriptableObject
{
    public delegate void FOnUpgradeRequestSignature(ECarPart Part, int Index);
    public event FOnUpgradeRequestSignature OnUpgradeRequested;

    [SerializedDictionary("Type", "UpgradeList")]
    [SerializeField] private SerializedDictionary<ECarPart, List<Upgrade>> UpgradeList;

    public bool GetUpgradeCount(ECarPart Part, out int Count)
    {
        if (UpgradeList.ContainsKey(Part))
        {
            Count = UpgradeList[Part].Count -1;
            return true;
        }

        Count = -1;
        return false;
    }

    public Upgrade GetUpgradeDetails(ECarPart part, int Index)
    {
        if (UpgradeList.ContainsKey(part))
        {
            return (Index < UpgradeList[part].Count) ? UpgradeList[part][Index] : null;
        }
        return null;
    }

    public void Trigger_UpgradeRequest(ECarPart part, int Index)
    {
        OnUpgradeRequested?.Invoke(part, Index);
    }
}


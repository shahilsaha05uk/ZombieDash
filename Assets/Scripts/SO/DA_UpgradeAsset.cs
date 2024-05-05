using System.Collections;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using EnumHelper;
using StructClass;
using UnityEngine;

[CreateAssetMenu(menuName = "Car", fileName = "DataAssets/DA_Upgrade", order = 2)]
public class DA_UpgradeAsset : ScriptableObject
{
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
            return (UpgradeList[part].Count - 1 > Index)? UpgradeList[part][Index] : null;
        }
        return null;
    }
}


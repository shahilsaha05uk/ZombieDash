using System.Collections;
using System.Collections.Generic;
using StructClass;
using UnityEngine;

[CreateAssetMenu(menuName = "Car", fileName = "DA_Upgrade", order = 0)]
public class DA_UpgradeAsset : ScriptableObject
{
    public string UpgradeName;
    [SerializeField] private List<FUpgradeStruct> UpgradeList;
    private int count = 0;
    public bool GetNextUpgrade(out FUpgradeStruct upgrade)
    {
        upgrade = default;
        if (UpgradeList != null && isUpgradeAvailable())
        {
            upgrade = UpgradeList[count];
            count++;

            return true;
        }
        return false;
    }

    public FUpgradeStruct GetCurrentUpgrade() { return UpgradeList[count - 1];}

    public bool isUpgradeAvailable(){ return (count < UpgradeList.Count); }

}


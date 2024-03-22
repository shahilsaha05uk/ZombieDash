using System.Collections;
using System.Collections.Generic;
using StructClass;
using UnityEngine;

[CreateAssetMenu(menuName = "Car", fileName = "DA_Upgrade", order = 0)]
public class DA_UpgradeAsset : ScriptableObject
{
    public string UpgradeName;
    public List<Upgrade> UpgradeList;
    
    
}


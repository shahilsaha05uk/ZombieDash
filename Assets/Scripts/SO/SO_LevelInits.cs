using AYellowpaper.SerializedCollections;
using EnumHelper;
using StructClass;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Level UI", menuName = "DataAssets/Level Init List", order = 0)]
public class SO_LevelInits : ScriptableObject
{
    [SerializedDictionary("Level", "List")]
    [SerializeField] private SerializedDictionary<ELevel, List<GameObject>> LevelInitList;


    public bool GetLevelInits(ELevel level, ref List<GameObject> initList)
    {
        return LevelInitList.TryGetValue(level, out initList);
    }    
}

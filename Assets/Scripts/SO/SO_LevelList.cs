using AYellowpaper.SerializedCollections;
using EnumHelper;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="LevelList", menuName = "DataAssets/LevelList", order=1)]
public class SO_LevelList : ScriptableObject
{
    [SerializedDictionary("Level", "Build ID")]
    [SerializeField] private SerializedDictionary<ELevel, int> LevelList;

    public SerializedDictionary<ELevel, int> GetList() { return LevelList; }
    public int GetBuildId(ELevel level) { return LevelList[level]; }
}

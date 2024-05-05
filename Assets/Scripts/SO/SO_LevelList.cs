using System;
using AdvancedSceneManager.Models;
using AYellowpaper.SerializedCollections;
using EnumHelper;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="LevelList", menuName = "DataAssets/LevelList", order=1)]
public class SO_LevelList : ScriptableObject
{
    [SerializedDictionary("Level", "Scene Collection")]
    [SerializeField] private SerializedDictionary<ELevel, SceneCollection> LevelList;

    public SceneCollection GetCollection(ELevel level)
    {
        return (LevelList != null && LevelList.TryGetValue(level, out var value))? value: null;
    }

    public void GetCollectionKey(SceneCollection collection, ref ELevel level)
    {
        var id = collection.id;
        foreach (var l in LevelList)
        {
            if (String.Compare(id, l.Value.id, StringComparison.Ordinal) == 0)
            {
                level = l.Key;
                break;
            }
        }
    }
}

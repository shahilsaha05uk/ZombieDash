using AYellowpaper.SerializedCollections;
using EnumHelper;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="Level UI", menuName = "DataAssets/Level UI List", order=0)]
public class SO_LevelUIList : ScriptableObject
{
    [SerializedDictionary("UI", "Prefab")]
    public SerializedDictionary<EUI, BaseWidget> WidgetClass;
}

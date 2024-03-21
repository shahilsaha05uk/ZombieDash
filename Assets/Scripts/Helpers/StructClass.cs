using System;
using System.Collections.Generic;
using EnumHelper;
using UnityEngine;
using UnityEngine.Serialization;

namespace StructClass
{
    public struct FLocationPoints
    {
        public Transform StartPos;
        public List<Transform> Checkpoints;
        public Transform EndPos;
    }

    public struct FHudValues
    {
        public float fuel;
        public float nitro;

        public void UpdateValue(ECarPart CarComp, float Value)
        {
            switch (CarComp)
            {
                case ECarPart.Fuel:
                    fuel = Value;
                    break;
                case ECarPart.Nitro:
                    nitro = Value;
                    break;
            }
        }
    }

    [System.Serializable]
    public struct FUpgradeStruct
    {
        public int id;
        public int cost;
        public float Value;
    }
}
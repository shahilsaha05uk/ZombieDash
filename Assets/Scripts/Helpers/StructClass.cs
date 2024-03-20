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

        public void UpdateValue(ECarComponent CarComp, float Value)
        {
            switch (CarComp)
            {
                case ECarComponent.Fuel:
                    fuel = Value;
                    break;
                case ECarComponent.Nitro:
                    nitro = Value;
                    break;
            }
        }
    }
}
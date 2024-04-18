using System;
using System.Collections.Generic;
using EnumHelper;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace StructClass
{
    public struct FLevelDetails
    {
        public int ID;
        public string Name;
        public ELevel LevelType;
        public Scene Level;
    }
    public struct FLocationPoints
    {
        public Transform StartPos;
        public List<Transform> Checkpoints;
        public Transform EndPos;
    }

    [System.Serializable]
    public struct FCarMetrics
    {
        public float fuel;
        public float nitro;

        public static void UpdateMetricValue(ref FCarMetrics metricToUpdate, ECarPart partToUpdate, float Value)
        {
            switch (partToUpdate)
            {
                case ECarPart.Fuel:
                    metricToUpdate.fuel = Value;
                    break;
                case ECarPart.Nitro:
                    metricToUpdate.nitro = Value;
                    break;
                case ECarPart.Speed:
                    break;
            }
        }

        /*        public void UpdateMetrics(ECarPart part, float Value)
                {
                    switch (part)
                    {
                        case ECarPart.Fuel:
                            fuel = Value;
                            break;
                        case ECarPart.Nitro:
                            nitro = Value;
                            break;
                        case ECarPart.Speed:
                            break;
                    }
                }*/

        public float getValue(ECarPart CarComp)
        {
            float val = 0.0f;
            switch(CarComp)
            {
                case ECarPart.Fuel: val = fuel; break;
                case ECarPart.Nitro: val = nitro; break;
            }
            return val;
        }
    }

    [System.Serializable]
    public class Upgrade
    {
        public int id;
        public int cost;
        public float Value;
    }
}
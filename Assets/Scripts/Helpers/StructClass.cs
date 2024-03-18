using System.Collections.Generic;
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
        
        public float fuel{ get; private set; }
        public float fuelNormalized { get; private set; }
        
        
        public float speed;
        public float positionMag;
        public float positionMagNormalized;
        public float nitro;
        public Vector2 position;
        public float totalFuel;

        public void UpdatePosition(Vector2 pos)
        {
            position = pos;
            positionMag = pos.magnitude;
            //positionMagNormalized = 
        }

        public void UpdateFuel(float Value)
        {
            fuel = Value;
            fuelNormalized = fuel = 1 - (fuel / totalFuel);
        }
    }
}
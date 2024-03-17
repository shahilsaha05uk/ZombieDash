using System.Collections.Generic;
using UnityEngine;

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
        public float speed;
        public float positionMag;
        public float boost;
        public float fuel;
        public Vector2 position;
        
        public FHudValues()
        {
            speed = 0f;
            positionMag = 0f;
            boost = 0f;
            fuel = 0f;
            position = Vector2.zero;
        }
    }
}
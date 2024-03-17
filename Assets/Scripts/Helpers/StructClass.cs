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
        public float positionMagNormalized;
        public float boost;
        public float fuel;
        public Vector2 position;

        public void UpdatePosition(Vector2 pos)
        {
            position = pos;
            positionMag = pos.magnitude;
            //positionMagNormalized = 
        }
    }
}
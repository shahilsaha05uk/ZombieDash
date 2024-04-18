using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace speedometer
{
    public class Speedometer : MonoBehaviour
    {
        public int MaxAngle = 90;    //This variable adjusts the maximum point to where the needle reaches

        public Transform needleArea;
        public void UpdateValue(float Value)
        {
            float val = Value * -MaxAngle;
            needleArea.eulerAngles = new Vector3(0, 0, val);
            //Debug.Log(val);
        }
    }
}
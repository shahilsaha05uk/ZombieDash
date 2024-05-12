using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HelperClass
{
    public static float NormalizeAngle(float angle)
    {
        angle %= 360;
        if (angle < 0)
        {
            angle += 360;
        }
        return angle;
    }
}

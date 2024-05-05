using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResettableTransform : MonoBehaviour
{
    private Vector3 pos;
    private Vector3 scale;
    private Quaternion rot;
    void Awake()
    {
        var trans = transform;
        
        pos = trans.position;
        rot = trans.rotation;
        scale = trans.localScale;

        GameManager.OnResetLevel += OnReset;
    }

    private void OnReset()
    {
        transform.SetPositionAndRotation(pos, rot);
        transform.localScale = scale;
    }
}

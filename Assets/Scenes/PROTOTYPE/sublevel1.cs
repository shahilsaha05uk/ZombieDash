using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor.Rendering;
using UnityEngine;

public class sublevel1 : MonoBehaviour
{
    public GameObject testObject;
    void Update()
    {
        if (Input.GetMouseButtonUp(1))
        {
            GameObject tmp = Instantiate(testObject, Vector3.zero, Quaternion.identity);
            LevelManager.MoveGameObjectToCurrentScene(tmp);
        }

    }
}

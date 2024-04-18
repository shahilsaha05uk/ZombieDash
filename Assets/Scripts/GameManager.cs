using System;
using System.Collections;
using System.Collections.Generic;
using EnumHelper;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Space(10)][Header("Prefabs")]
    [SerializeField] private Controller mControllerPrefab;
    
    [Space(10)][Header("Object References")]
    [SerializeField] private Controller mController;

    private void Start()
    {
        mController = Instantiate(mControllerPrefab);
    }
    private void OnDestroy()
    {

    }

    public static APlayerStart GetPlayerStart()
    {
        return FindObjectOfType<APlayerStart>();
    }
}

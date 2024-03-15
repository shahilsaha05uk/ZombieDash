using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    public GameObject startPos;
    public GameObject flag;
    public GameObject player;

    public Slider mPlayerProgress;
    public Slider mFuelMeter;
    
    public TextMeshProUGUI txtKPH;


    private float totalDistance;
    private void Start()
    {
        mFuelMeter.maxValue = 1;
        mFuelMeter.minValue = 0;
        mFuelMeter.value = 1;
        totalDistance = Mathf.Abs(flag.transform.position.x - startPos.transform.position.x);
    }

    private void Update()
    {
        float currentDistance = Mathf.Abs(flag.transform.position.x - player.transform.position.x);
        float progress = 1 - (currentDistance / totalDistance);
        mPlayerProgress.value = progress;
    }

    public void DecreaseFuel()
    {
        mFuelMeter.value -= 0.0001f;
    }
}

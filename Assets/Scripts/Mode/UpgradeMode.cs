using System;
using System.Collections;
using System.Collections.Generic;
using AdvancedSceneManager.Models;
using UnityEngine;

public class UpgradeMode : BaseMode
{
    public SO_GarageList GarageList;
    public static UpgradeMode Instance;

    private void Start()
    {
        if (Instance == null) Instance = this;
    }

    public void SetCar(CarSizeHandler CarRef)
    {
        
    }
}

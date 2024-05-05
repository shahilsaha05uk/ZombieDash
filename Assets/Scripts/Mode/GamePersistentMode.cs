using System;
using System.Collections;
using System.Collections.Generic;
using AdvancedSceneManager.Models;
using UnityEngine;

public class GamePersistentMode : BaseMode
{
    [SerializeField] private Car mCarPrefab;

    private Car mCar;

    public static GamePersistentMode Instance;
    private void Start()
    {
        if (Instance == null) Instance = this;
    }

    public void SpawnCar()
    {
        if (mCar != null) return;
        Transform spawnTransform = GameManager.GetPlayerStart().transform;
        mCar = Instantiate(mCarPrefab, spawnTransform.position, spawnTransform.rotation);
    }
}

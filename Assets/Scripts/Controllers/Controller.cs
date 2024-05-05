using System;
using System.Collections;
using System.Collections.Generic;
using AdvancedSceneManager.Utility;
using Cinemachine;
using EnumHelper;
using StructClass;
using UnityEngine;
using UnityEngine.SceneManagement;
using Scene = AdvancedSceneManager.Models.Scene;

public class Controller : BaseController
{
    [SerializeField] private Car mCarPrefab;
    [SerializeField] private Camera mMainCameraPrefab;
    [SerializeField] private CinemachineVirtualCamera mVirtualCameraPrefab;

    private Car mCar;
    private Camera mCamera;
    private CinemachineVirtualCamera mVirtualCamera;

    private FLocationPoints mFLocations;

    private bool isUIInitialised;

    protected override void InitController()
    {
        base.InitController();
        
        // Spawn and set up the car
        Transform spawnTransform = GameManager.GetPlayerStart().transform;
        mCar = Instantiate(mCarPrefab, spawnTransform.position, spawnTransform.rotation);
        mCar.Possess(this);

        // Set up the camera and attach it to the car to be able to follow it
        if (mVirtualCameraPrefab)
        {
            mVirtualCamera = Instantiate(mVirtualCameraPrefab);
            mVirtualCamera.Follow = mCar.transform;
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using EnumHelper;
using StructClass;
using UnityEngine;

public class Controller : MonoBehaviour
{
    public delegate BaseWidget FOnRequestUISignature(EUI ui);

    public FOnRequestUISignature OnRequestUI;

    [SerializeField] private Car mCarPrefab;
    [SerializeField] private Camera mMainCameraPrefab;
    [SerializeField] private CinemachineVirtualCamera mVirtualCameraPrefab;

    private Car mCar;
    [SerializeField] private Camera mCamera;
    [SerializeField] private CinemachineVirtualCamera mVirtualCamera;

    private PlayerHUD mPlayerHUD;
    
    private FLocationPoints mFLocations;

    

    //private Coroutine mPlayerHudCor;
    private void Start()
    {
        // Setting up the Locations
        /*
        mFLocations.StartPos = GameManager.GetPlayerStart().transform;
        mFLocations.EndPos = GameObject.FindWithTag("Finish").transform;
        GameObject[] Checkpoints = GameObject.FindGameObjectsWithTag("Checkpoint");
        foreach (var c in Checkpoints) {
            mFLocations.Checkpoints.Add(c.transform);
        }
        */
        
        if (mMainCameraPrefab) mCamera = Instantiate(mMainCameraPrefab);
        
        MainMenu mainMenu = OnRequestUI.Invoke(EUI.MAIN_MENU).GetWidgetAs<MainMenu>();
        if (mainMenu)
        {
            mainMenu.OnPlayButtonClicked += OnStartPlay;
            mainMenu.AddToViewport();
        }
    }

    
    private void OnStartPlay()
    {
        Debug.Log("Start Play");
        
        // get the Player HUD
        mPlayerHUD = OnRequestUI?.Invoke(EUI.PLAYERHUD).GetWidgetAs<PlayerHUD>();
        if (mPlayerHUD != null) {
            mPlayerHUD.AddToViewport();
        }

        // Spawn the player
        Transform spawnTransform = GameManager.GetPlayerStart().transform;
        mCar = Instantiate(mCarPrefab, spawnTransform);
        mCar.OnCarStatusUpdate += UpdateHUD;
        CameraSetup();
        mCar.Possess(this);
    }

    private void CameraSetup()
    {
        if(mCar == null) return;

        if (mVirtualCameraPrefab)
        {
            mVirtualCamera = Instantiate(mVirtualCameraPrefab);
            mVirtualCamera.Follow = mVirtualCamera.LookAt = mCar.transform;
        }
    }
    
    // HUD Methods
    private void UpdateHUD(FHudValues hudValues)
    {
        mPlayerHUD.UpdateCarStatus(hudValues);
    }
}
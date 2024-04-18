using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using EnumHelper;
using StructClass;
using UnityEngine;

public class Controller : BaseController
{
    [SerializeField] private Car mCarPrefab;
    [SerializeField] private Camera mMainCameraPrefab;
    [SerializeField] private CinemachineVirtualCamera mVirtualCameraPrefab;

    private Car mCar;
    [SerializeField] private Camera mCamera;
    [SerializeField] private CinemachineVirtualCamera mVirtualCamera;

    private PlayerHUD mPlayerHUD;
    
    private FLocationPoints mFLocations;

    private bool isUIInitialised;

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
    }

    /*    private void OnWidgetInitialised(bool isSuccess)
        {
            ELevel l = LevelManager.Instance.mCurrentLevel;

            switch (l)
            {
                case ELevel.MENU:
                    MainMenu mainMenu = UIManager.Instance.GetWidgetRef(EUI.MAIN_MENU).GetWidgetAs<MainMenu>();
                    if (mainMenu)
                    {
                        mainMenu.OnPlayButtonClicked += OnStartPlay;
                        mainMenu.AddToViewport();
                    }
                    break;
                case ELevel.GAME:

                    break;
            }
        }
    */
    private void OnStartPlay()
    {
        Debug.Log("Start Play");
        
        // get the Player HUD
       // mPlayerHUD = OnRequestUI?.Invoke(EUI.PLAYERHUD).GetWidgetAs<PlayerHUD>();
/*        mPlayerHUD = UIManager.Instance.GetWidgetRef(EUI.PLAYERHUD).GetWidgetAs<PlayerHUD>();
        if (mPlayerHUD != null) {
            mPlayerHUD.AddToViewport();
        }

*/        // Spawn the player
        Transform spawnTransform = GameManager.GetPlayerStart().transform;
        mCar = Instantiate(mCarPrefab, spawnTransform.position, spawnTransform.rotation);
        mCar.OnComponentUpdated += UpdateHUD;
        
        CameraSetup();
        mCar.Possess(this);
    }

    private void CameraSetup()
    {
        if(mCar == null) return;

        if (mVirtualCameraPrefab)
        {
            mVirtualCamera = Instantiate(mVirtualCameraPrefab);
            mVirtualCamera.Follow = mCar.transform;
        }
    }
    
    // HUD Methods
    private void UpdateHUD(ECarPart carPart, FHudValues hudValues)
    {
        mPlayerHUD.UpdateCarStatus(hudValues);
    }

    public void OpenUpgradeUI()
    {
        //UpgradeUI upgradeUI = OnRequestUI?.Invoke(EUI.UPGRADE).GetWidgetAs<UpgradeUI>();
/*        UpgradeUI upgradeUI = UIManager.Instance.GetWidgetRef(EUI.UPGRADE).GetWidgetAs<UpgradeUI>();
        if (upgradeUI != null)
        {
            upgradeUI.OnUpgradeClick += UpgradeCar;
            upgradeUI.AddToViewport();
        }
*/    }

    private void UpgradeCar(ECarPart carcomp, Upgrade upgradestruct)
    {
        mCar.Upgrade(carcomp, upgradestruct);
    }
}
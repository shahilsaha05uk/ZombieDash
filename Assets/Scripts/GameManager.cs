using System;
using System.Collections;
using System.Collections.Generic;
using EnumHelper;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Space(10)][Header("Prefabs")]
    [SerializeField] private Controller mControllerPrefab;
    
    [Header("Managers")]
    [SerializeField] private UIManager mUIManager;
    [SerializeField] private SoundManager mSoundManager;
    [SerializeField] private EnemyManager mEnemyManager;
    [SerializeField] private WaveManager mWaveManager;
    
    [Space(10)][Header("Object References")]
    [SerializeField] private Controller mController;

    private void Start()
    {
        mController = Instantiate(mControllerPrefab);
        mController.OnRequestUI += OnRequestUI;
    }
    private void OnDestroy()
    {
        mController.OnRequestUI -= OnRequestUI;
    }


    private BaseWidget OnRequestUI(EUI ui)
    {
        return mUIManager.InitialiseWidget(ui);
    }

    public static APlayerStart GetPlayerStart()
    {
        return FindObjectOfType<APlayerStart>();
    }
}

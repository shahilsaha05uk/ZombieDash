using System;
using System.Collections;
using System.Collections.Generic;
using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Models;
using EnumHelper;
using UnityEngine;

public class GameManager : ParentManager
{
    // This is triggered when the day in the game scene starts
    public delegate void FDayBeginSignature();
    public event FDayBeginSignature OnDayBegin;
    
    // This is triggered before the Day Complete is called... This is to allow the entities to prepare themselves before the level is unloaded
    public delegate void FDayPreCompleteSignature();
    public event FDayPreCompleteSignature OnDayPreComplete;
    
    // This is triggered when the Day is finally completed
    public delegate void FDayCompleteSignature();
    public event FDayCompleteSignature OnDayComplete;

    public static GameManager Instance { get; private set; }

    protected override void InitManager()
    {
        base.InitManager();

        if (Instance == null) Instance = this;
    }

    public static APlayerStart GetPlayerStart()
    {
        return FindObjectOfType<APlayerStart>();
    }

    #region Day Management
    
    //NOTE: THESE METHODS SHOULD ONLY BE CALLED BY THE ENTITIES IN THE GAME SCENE
    public void DayBegin()
    {
        OnDayBegin?.Invoke();
    }

    public void DayComplete()
    {
        OnDayPreComplete?.Invoke();
        OnDayComplete?.Invoke();

        LevelManager.Instance.OpenAdditiveScene(ELevel.GAME, true);
    }

    #endregion

}

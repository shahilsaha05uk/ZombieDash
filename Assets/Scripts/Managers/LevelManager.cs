using AYellowpaper.SerializedCollections;
using EnumHelper;
using StructClass;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private SO_LevelList mLevelList;

    public delegate void FOnLevelLoadedSignature(ELevel currentLevel);
    public static event FOnLevelLoadedSignature OnLevelLoaded;

    public delegate void FSetupOnLevelLoadedSignature(ELevel currentLevel);
    public static event FSetupOnLevelLoadedSignature InitOnLevelLoaded;

    public delegate void FOnLevelUnloadedSignature();
    public static event FOnLevelUnloadedSignature OnLevelUnload;

    public static FLevelDetails mCurrentLevel;
    private LoadSceneParameters loadParams;

    public static LevelManager Instance;

    private void OnEnable()
    {
        if(Instance == null) Instance = this;
    }
    private void Start()
    {
        loadParams.loadSceneMode = LoadSceneMode.Additive;

        mCurrentLevel.LevelType = ELevel.NONE;
        mCurrentLevel.Level = SceneManager.GetActiveScene();
        
        OnLevelLoaded?.Invoke(mCurrentLevel.LevelType);
    }

    public void LoadScene(ELevel level, bool shouldUnload = false)
    {
        int id = mLevelList.GetBuildId(level);
        if (id != -1)
        {
            if(shouldUnload && mCurrentLevel.Level != default) StartCoroutine(UnloadScene());

            StartCoroutine(LoadScene(level, id));
        }
    }
    private IEnumerator LoadScene(ELevel level, int buildID)
    {
        int id = buildID;

        AsyncOperation op = SceneManager.LoadSceneAsync(id, loadParams);
        while (!op.isDone)
        {
            yield return null;
        }
        mCurrentLevel.Level = SceneManager.GetSceneByBuildIndex(id);
        mCurrentLevel.LevelType = level;
        InitOnLevelLoaded?.Invoke(level);
        OnLevelLoaded?.Invoke(level);

    }

    private IEnumerator UnloadScene()
    {
        OnLevelUnload?.Invoke();
        
        AsyncOperation op = SceneManager.UnloadSceneAsync(mCurrentLevel.Level);

        while (!op.isDone)
        {
            yield return null;
        }
        mCurrentLevel.Level = default;
    }

    public static void MoveGameObjectToCurrentScene(GameObject actor)
    {
        SceneManager.MoveGameObjectToScene(actor, mCurrentLevel.Level);
    }
}

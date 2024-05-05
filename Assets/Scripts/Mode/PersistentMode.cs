using System;
using AdvancedSceneManager.Models;
using UnityEngine;

public class PersistentMode : BaseMode
{
    [SerializeField] private SceneCollection StartingCollection;
    [SerializeField] private bool bShouldOpenAllScenesInTheStartingCollection;

    private void Start()
    {
        LevelManager levelManager = LevelManager.Instance;

        if (levelManager != null)
        {
            levelManager.OpenAdditiveScene(EnumHelper.ELevel.MENU, bShouldOpenAllScenesInTheStartingCollection);
        }
    }
}

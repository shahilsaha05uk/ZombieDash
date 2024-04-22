using AdvancedSceneManager.Models;
using UnityEngine;

public class PersistentMode : BaseMode
{
    [SerializeField] private SceneCollection StartingCollection;
    [SerializeField] private bool bShouldOpenAllScenesInTheStartingCollection;

    protected override void InitMode(SceneCollection collection)
    {
        base.InitMode(collection);

        LevelManager levelManager = LevelManager.Instance;

        if (levelManager != null)
        {
            levelManager.OpenAdditiveScene(EnumHelper.ELevel.MENU, bShouldOpenAllScenesInTheStartingCollection);
        }

    }
}

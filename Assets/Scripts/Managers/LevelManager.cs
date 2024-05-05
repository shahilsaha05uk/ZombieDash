using System.Collections;
using AdvancedSceneManager;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using EnumHelper;
using System.Collections.Specialized;
using AdvancedSceneManager.Callbacks;
using Unity.VisualScripting;
using UnityEngine;

public class LevelManager : ParentManager
{
    
    public static LevelManager Instance { get; private set; }
    [SerializeField] private SO_LevelList mLevelList;
    
    protected override void InitManager()
    {
        base.InitManager();

        if (Instance == null) Instance = this;
    }

    public void OpenScene(ELevel levelToOpen, bool ShouldOpenAllScenes)
    {
        SceneCollection collection = mLevelList.GetCollection(levelToOpen);
        if(collection != null)
        {
            StartCoroutine(LoadScene(collection, ShouldOpenAllScenes));
        }
    }
    private IEnumerator LoadScene(SceneCollection collection, bool ShouldOpenAllScenes)
    {
        collection.Open(ShouldOpenAllScenes);
        while (!collection.activeScene.isOpen)
        {
            yield return null;
        }
        collection.activeScene.SetActive();
    }

    public void OpenAdditiveScene(ELevel levelToOpen, bool ShouldOpenAllScenes)
    {
        SceneCollection collection = mLevelList.GetCollection(levelToOpen);
        if(collection != null)
        {
            StartCoroutine(LoadAdditiveScene(collection, ShouldOpenAllScenes));
        }
    }
    private IEnumerator LoadAdditiveScene(SceneCollection collection, bool ShouldOpenAllScenes)
    {
        collection.Open(ShouldOpenAllScenes);
        while (!collection.activeScene.isOpen)
        {
            yield return null;
        }
        collection.activeScene.SetActive();
    }

    public void MoveGameObjectToCurrentScene(GameObject actor)
    {
        Scene s;
        actor.ASMScene(out s);

        SceneUtility.Move(actor, s);
    }
    public void MoveGameObjectToCurrentScene(GameObject actor, SceneCollection collection)
    {
        SceneUtility.Move(actor, collection.activeScene);
    }

}
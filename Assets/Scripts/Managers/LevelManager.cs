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
    
    public delegate void FOnLevelUnloadedSignature();
    public static event FOnLevelUnloadedSignature OnLevelUnload;


    protected override void InitManager()
    {
        base.InitManager();

        if (Instance == null) Instance = this;
    }

    /*    

        public delegate void FOnLevelLoadedSignature(ELevel currentLevel);
        public static event FOnLevelLoadedSignature OnLevelLoaded;

        public delegate void FSetupOnLevelLoadedSignature(ELevel currentLevel);
        public static event FSetupOnLevelLoadedSignature InitOnLevelLoaded;

        public delegate void FOnLevelUnloadedSignature();
        public static event FOnLevelUnloadedSignature OnLevelUnload;

        public static FLevelDetails mCurrentLevel;
        private LoadSceneParameters loadParams;

        public static LevelManager Instance;

    /*   

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
    */
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
    public static void MoveGameObjectToCurrentScene(GameObject actor)
    {
        Scene s;
        actor.ASMScene(out s);

        SceneUtility.Move(actor, s);
    }
    public static void MoveGameObjectToCurrentScene(GameObject actor, SceneCollection collection)
    {
        SceneUtility.Move(actor, collection.activeScene);
    }

}
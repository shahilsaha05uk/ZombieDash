using UnityEngine;

public class GameManager : ParentManager
{
    [SerializeField] private int StartingBalance;
    
    public delegate void FOnResetLevelSignature();
    public static event FOnResetLevelSignature OnResetLevel;
    public static GameManager Instance { get; private set; }

    protected override void InitManager()
    {
        base.InitManager();

        if (Instance == null) Instance = this;
        
        ResourceComp.AddResources(StartingBalance);
    }

    public static APlayerStart GetPlayerStart()
    {
        return FindObjectOfType<APlayerStart>();
    }

    #region Day Management
    
    //NOTE: THESE METHODS SHOULD ONLY BE CALLED BY THE ENTITIES IN THE GAME SCENE
    public void DayBegin()
    {
        
    }

    public void DayComplete()
    {
        OnResetLevel?.Invoke();
    }

    #endregion
}

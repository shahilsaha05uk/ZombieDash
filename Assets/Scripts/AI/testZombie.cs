using AYellowpaper.SerializedCollections;
using Helpers;
using Interfaces;
using UnityEngine;

public enum EEnemyState { IDLE, PATROL, DEAD };
public enum EEnemyType { IdleEnemy, PatrollingEnemy};

public class testZombie : MonoBehaviour, IResetInterface
{
    [SerializedDictionary("State", "Ref")]
    [SerializeField] private SerializedDictionary<EEnemyState, BaseState> StateList;

    public EEnemyType EnemyType;
    private EEnemyState CurrentState;

    private void Start()
    {
        GameManager.OnResetLevel += OnReset;

        switch (EnemyType)
        {
            case EEnemyType.IdleEnemy: UpdateState(EEnemyState.IDLE); break;
            case EEnemyType.PatrollingEnemy: UpdateState(EEnemyState.PATROL); break;
        }
    }
    private void UpdateState(EEnemyState type)
    {
        if(StateList.ContainsKey(CurrentState))
            StateList[CurrentState].ExitState();
        
        if (StateList.ContainsKey(type))
            StateList[type].EnterState();

        CurrentState = type;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            GetComponent<Animator>().SetBool(AnimationParametersDictionary.Trigger_IsDead, true);
            UpdateState(EEnemyState.DEAD);  
        }
    }

    public void OnReset()
    {
        GetComponent<Animator>().SetBool(AnimationParametersDictionary.Trigger_IsDead, false);
    }
}


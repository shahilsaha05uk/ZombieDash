using BehaviorDesigner.Runtime.Tasks;

public class T_RandomActionSelect : Action
{
    private EnemyStates mState;

    public override void OnAwake()
    {
        System.Array values = System.Enum.GetValues(typeof(EnemyStates));
        System.Random rand = new System.Random();
        mState = (EnemyStates)values.GetValue(rand.Next(values.Length));

        UnityEngine.Debug.Log("Random Enemy State: " + mState);
    }
}

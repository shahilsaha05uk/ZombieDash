using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

public class T_RandomActionSelect : Action
{
    public bool bShouldRandomize;
    public SharedBool bShouldPatrol;
    public bool bValue;
    public override void OnAwake()
    {
        bShouldPatrol.Value = (bShouldRandomize) ? Random.Range(0, 2) == 1 : bValue;
    }
}

using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using BehaviorDesigner.Runtime.Tasks.Unity.UnityTransform;

public class T_Action : Action
{
	public SharedBool bShouldPatrol;

    public override void OnStart()
	{
		Animator anim = Owner.GetComponent<Animator>();
        UnityEngine.Debug.Log("Enemy should Patrol?: " + bShouldPatrol);

        Debug.Log(Owner.name + " Controller name: " + anim.name);
        anim.SetBool("bShouldPatrol", bShouldPatrol.Value);
	}
}
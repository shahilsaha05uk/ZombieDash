using BehaviorDesigner.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyStates
{
    IDLE,
    WALK,
    RUN
};

public class ZombieController : MonoBehaviour
{
    private BehaviorTree mBehaviorTree;

    public Animator mAnimator;

    private void Start()
    {
        mBehaviorTree = GetComponent<BehaviorTree>();
        //mAnimator = GetComponent<Animator>();
        //mAnimator.SetBool("bWalk", false);
    }
}

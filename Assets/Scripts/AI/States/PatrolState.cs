using Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class PatrolState : BaseState
{
    public PatrolRoute mRoute;
    private List<GameObject> mPoints;

    private int mCurrentPointID;
    private bool bShouldPatrol;
    [SerializeField] private float mDistanceThreshold = 0.1f;
    [SerializeField] private float mMoveSpeed = 1f;
    [SerializeField] private float mWaitDuration = 3f;


    private Animator mAnimator;
    private string mPatrolAnimParam;
    private string mPatrolSpeedMultiplierAnimParam;
    protected override void Start()
    {
        mAnimator = GetComponent<Animator>();
        mPatrolAnimParam = AnimationParametersDictionary.Bool_ShouldPatrol;
        mPatrolSpeedMultiplierAnimParam = AnimationParametersDictionary.Float_SpeedMultiplier;
        mAnimator.SetFloat(mPatrolSpeedMultiplierAnimParam, mMoveSpeed);
    }

    public override void EnterState()
    {
        base.EnterState();
        mPoints = mRoute.Route;
        bShouldPatrol = true;
        StateCoroutine = StartCoroutine(Patrol());
    }
    private IEnumerator Patrol()
    {
        WaitForSeconds timeInterval = new WaitForSeconds(mWaitDuration);
        while (bShouldPatrol)
        {
            var point = GetNextPoint();
            Flip(point);
            mAnimator.SetBool(mPatrolAnimParam, true);
            while (GetDistance(point.x) >= mDistanceThreshold && bShouldPatrol)
            {
                Vector3 direction = new Vector3(Mathf.Sign(point.x - transform.position.x), 0f, 0f);

                transform.position += direction * mMoveSpeed * Time.deltaTime;

                yield return null;
            }

            mAnimator.SetBool(mPatrolAnimParam, false);
            yield return timeInterval;
        }
    }

    public override void ExitState()
    {
        base.ExitState();
        bShouldPatrol = false;

        if (StateCoroutine != null)
        {
            StopCoroutine(StateCoroutine);
            StateCoroutine = null;
        }
    }

    private float GetDistance(float TargetX)
    {
        return Mathf.Abs(transform.position.x - TargetX);
    }
    private Vector3 GetNextPoint()
    {
        mCurrentPointID++;

        if (mCurrentPointID >= mPoints.Count)
            mCurrentPointID = 0;

        var pos = mPoints[mCurrentPointID].transform.position;
        pos.y = pos.z = 0;
        return pos;
    }
    private void Flip(Vector3 nextPoint)
    {
        float directionX = Mathf.Sign(nextPoint.x - transform.position.x);
        if (directionX != 0)
        {
            // Get the current local scale
            Vector3 currentScale = transform.localScale;

            // Calculate the new local scale based on the direction while preserving the magnitude on the x-axis
            Vector3 newScale = new Vector3(Mathf.Abs(currentScale.x) * directionX, currentScale.y, currentScale.z);

            // Set the local scale
            transform.localScale = newScale;
        }
    }
}

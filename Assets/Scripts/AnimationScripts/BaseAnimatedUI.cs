using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseAnimatedUI : MonoBehaviour
{
    public delegate void FOnAnimationFinishSignature(EAnimDirection Direction);
    public event FOnAnimationFinishSignature OnAnimEnd;

    [SerializeField]protected string AnimationParameter;
    [SerializeField]protected float mRate;

    protected string bIn = "bIn";
    protected string bOut = "bOut";
    protected EAnimDirection mDirection;
    public void StartAnim(EAnimDirection direction)
    {
        mDirection = direction;
        var anim = GetComponent<Animator>();
        if (mDirection == EAnimDirection.Forward)
        {
            anim.SetBool(bIn, true);
            anim.SetBool(bOut, false);
            //mTarget = 1.0f;
        }

        if (mDirection == EAnimDirection.Backward)
        {
            anim.SetBool(bIn, false);
            anim.SetBool(bOut, true);
            //mTarget = 0.0f;
        }
        //StartCoroutine(Animate());
    }

    protected virtual IEnumerator Animate()
    {
        yield return null;
        OnAnimEnd?.Invoke(mDirection);
    }

}

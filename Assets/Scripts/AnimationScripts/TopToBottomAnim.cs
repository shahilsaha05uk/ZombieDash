using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TopToBottomAnim : BaseAnimatedUI
{
    protected override IEnumerator Animate()
    {
        yield break;
        /*if (!TryGetComponent(out Animator anim)) yield break;


        // Start `t` from the current value of the animation parameter
        float t = anim.GetFloat(AnimationParameter);
        WaitForSeconds interval = new WaitForSeconds(Time.deltaTime); // Using a small time interval for smooth update
    
        // Continue updating until the difference is small enough
        while (Math.Abs(t - mTarget) > 0.01f)
        {
            // Move `t` towards `mTarget` by `mRate` each frame
            t = Mathf.MoveTowards(t, mTarget, mRate * Time.deltaTime);
            anim.SetFloat(AnimationParameter, t);
            print(anim.GetFloat(AnimationParameter));
            yield return interval;
        }

        // Ensure the target value is set at the end
        anim.SetFloat(AnimationParameter, mTarget);
        yield return base.Animate();*/
    }
}

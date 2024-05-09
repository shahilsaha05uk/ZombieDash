using Helpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testZombie : MonoBehaviour
{

    private void Update()
    {
        if(Input.GetKeyUp(KeyCode.F))
        {
            GetComponent<Animator>().SetBool(AnimationParametersDictionary.Trigger_IsDead, true);
        }
        if(Input.GetKeyUp(KeyCode.R))
        {
            GetComponent<Animator>().SetBool(AnimationParametersDictionary.Trigger_IsDead, false);
        }
    }
}

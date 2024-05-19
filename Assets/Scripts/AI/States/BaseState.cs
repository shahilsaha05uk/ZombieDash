using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseState : MonoBehaviour
{
    protected Coroutine StateCoroutine;

    protected virtual void Start()
    {

    }
    public virtual void EnterState()
    {

    }
    public virtual void ExitState() 
    {

    }
}

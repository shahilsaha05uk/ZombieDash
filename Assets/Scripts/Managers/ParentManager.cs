using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ParentManager : MonoBehaviour
{
    private void OnEnable()
    {
        InitManager();
    }
    protected virtual void InitManager()
    {

    }
}

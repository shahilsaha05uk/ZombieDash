using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseController : MonoBehaviour, ICollectionOpenAsync
{
    IEnumerator ICollectionOpenAsync.OnCollectionOpen(SceneCollection collection)
    {
        while (!collection.activeScene.isOpen) { yield return null; }

        InitController();
    }

    protected virtual void InitController()
    {

    }
}

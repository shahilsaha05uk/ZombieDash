using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Models;
using System.Collections;
using System.Collections.Generic;
using System;
using EnumHelper;
using UnityEngine;
using Unity.VisualScripting;

public abstract class BaseMode : MonoBehaviour, ICollectionOpenAsync
{
    [SerializeField] protected Canvas SceneCanvas;

    IEnumerator ICollectionOpenAsync.OnCollectionOpen(SceneCollection collection)
    {
        while (!collection.activeScene.isOpen) { yield return null; }

        InitMode(collection);
    }


    protected virtual void InitMode(SceneCollection collection)
    {

    }
}

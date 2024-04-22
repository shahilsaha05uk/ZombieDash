using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitOnCollectionOpen : MonoBehaviour, ICollectionOpenAsync
{
    public delegate void FOnCollectionOpenedSignature();
    public static event FOnCollectionOpenedSignature OnCollectionOpened;

    public IEnumerator OnCollectionOpen(SceneCollection collection)
    {
        while (!collection.activeScene.isOpen) { yield return null; }

        OnCollectionOpened?.Invoke();
    }

}

using System;
using System.Collections;
using System.Collections.Generic;
using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Models;
using EnumHelper;
using UnityEngine;

public class GameMode : BaseMode, ICollectionOpenAsync
{
    public IEnumerator OnCollectionOpen(SceneCollection collection)
    {
        while (!collection.activeScene.isOpen) yield return null;

        GamePersistentMode.Instance.SpawnCar();
    }
}

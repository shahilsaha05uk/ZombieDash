using System.Collections;
using System.Collections.Generic;
using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Models;
using UnityEngine;

public class ToggleVisibilityOnSceneLoad : MonoBehaviour, ICollectionCloseAsync, ICollectionOpenAsync
{
    public IEnumerator OnCollectionClose(SceneCollection collection)
    {
        gameObject.SetActive(false);
        yield return null;
    }

    public IEnumerator OnCollectionOpen(SceneCollection collection)
    {
        gameObject.SetActive(true);
        yield return null;
    }
}

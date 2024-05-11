using Helpers;
using Interfaces;
using UnityEngine;

public class testZombie : MonoBehaviour, IResetInterface
{
    private void Start()
    {
        GameManager.OnResetLevel += OnReset; 
    }

    public void OnReset()
    {
        GetComponent<Animator>().SetBool(AnimationParametersDictionary.Trigger_IsDead, false);
    }
}
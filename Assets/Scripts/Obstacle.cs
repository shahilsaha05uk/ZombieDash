using System.Collections;
using System.Collections.Generic;
using Helpers;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [SerializeField] private float mRestrainForce;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            GetComponent<Animator>().SetBool(AnimationParametersDictionary.Trigger_IsDead, true);
            other.gameObject.GetComponent<Rigidbody2D>().AddForce(-other.transform.right * mRestrainForce);
        }
    }
}

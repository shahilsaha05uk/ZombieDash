using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosive : MonoBehaviour
{
    public ParticleSystem mExplosiveParticle;
    private void Start()
    {
        this.gameObject.SetActive(true);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            Explode();
        }
    }

    private void Explode()
    {
        mExplosiveParticle.Play();
        //TODO: Add the Visual Effect activate code
        //this.gameObject.SetActive(false);
        Debug.Log("Explode");
    }
}

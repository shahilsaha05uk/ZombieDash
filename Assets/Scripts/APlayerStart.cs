using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class APlayerStart : MonoBehaviour
{
    public ParticleSystem ParticleSystem;

    private void Start()
    {
        StartCoroutine(StartParticle());
    }
    IEnumerator StartParticle()
    {
        while (true)
        {
            ParticleSystem.Play();

            yield return new WaitForSeconds(1f);
        }
    }
}

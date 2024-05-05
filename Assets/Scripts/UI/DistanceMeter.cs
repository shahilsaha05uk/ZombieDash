using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class DistanceMeter : MonoBehaviour
{
    [SerializeField] private Slider mSlider;

    private Transform StartTransform;
    private Transform EndTransform;
    private void Start()
    {
        mSlider = GetComponent<Slider>();
        mSlider.minValue = 0;
        mSlider.maxValue = 1;
    }

    public void UpdateValue(float NormalizedDistance)
    {
        mSlider.normalizedValue = NormalizedDistance;
    }
}

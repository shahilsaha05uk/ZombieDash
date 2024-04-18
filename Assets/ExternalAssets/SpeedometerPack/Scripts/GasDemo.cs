using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace speedometer
{
public class GasDemo : MonoBehaviour
{
    public Slider slider;
    private float power;
    public Image fillSlider;

    public Color amarillo;
    public Color verde;
    public Color azul;

    private bool gas=false;
    private bool slid=false;

    void Update()
    {
        if(gas==true)
        {
            if(power < 1)
            {
                power=power+0.0008f;
                fillSlider.color = verde;
            }
        }
        else
        {
            if(power > 0)
            {
                power=power-0.001f;
                fillSlider.color = amarillo;
            }
        }

        if(slid == false)
        {
            slider.value = power;
        }
        else
        {
            fillSlider.color = azul;
            power = slider.value;
        }
    }

    public void OnGas()
    {
        gas=true;
    }

    public void OffGas()
    {
        gas=false;
    }

    public void OnSlider()
    {
        slid=true;
    }

    public void OffSlider()
    {
        slid=false;
    }
}
}

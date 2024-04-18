using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace speedometer
{
public class Speedometer2 : MonoBehaviour
{
    [Range(0f, 1f)]
    public float Value;

    public float MaxFill=0.75f;     //This variable adjusts the maximum point to where the needle reaches
    public Image fillArea;
    public Slider slider;
    public Text velocidad;
    public Gradient gradiente;

    private float time;
    public float refresh = 0.1f;
    private float framecount;

    private float time2;
    private float refresh2 = 0.1f;
    private float framecount2;
    private int top;

    public Color topRpm = new Color(1, 1, 1, 1);

    void Update()
    {
        time+=Time.deltaTime;
        framecount++;

        if(time>=refresh)
        {
            velocidad.text = (int)(Value*200) + "";
            time-=refresh;
            framecount=0;
        }


        time2+=Time.deltaTime;
        framecount2++;

        if(time2>=refresh2)
        {
            if(top==0 && Value == 1)
            {
                top=1;
            }
            else
            {
                top=0;
            }
            time2-=refresh2;
            framecount2=0;
        }

        if(top==0)
        {
            fillArea.color = gradiente.Evaluate(Value);
        }
        else
        {
            fillArea.color = topRpm;
        }

        Value = slider.value;

        fillArea.fillAmount = Value*MaxFill; 
    }
}
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GarageManager : MonoBehaviour
{
    public delegate void FGetCarReferenceSignature(GarageCar garageCar);
    public static event FGetCarReferenceSignature OnCarReference;

    public GameObject Car1;
    public GameObject Car2;
    public GameObject Car3;

    public Vector3 Car1Position;
    public Vector3 Car2Position;
    public Vector3 Car3Position;

    public Vector3 originalCarPosition;
    public Vector3 scaledCarPosition;
    public GameObject activeCar;

    private void Update()
    {
        if(Input.GetKeyUp(KeyCode.Alpha1))
        {
            Car1.SetActive(true);
            Car2.SetActive(false);
            Car3.SetActive(false);

            activeCar = Car1;
            originalCarPosition = activeCar.transform.position;
            activeCar.transform.position = scaledCarPosition;
        }
        if(Input.GetKeyUp(KeyCode.Alpha2))
        {
            Car1.SetActive(false);
            Car2.SetActive(true);
            Car3.SetActive(false);
            
            activeCar = Car2;
            originalCarPosition = activeCar.transform.position;
            activeCar.transform.position = scaledCarPosition;
        }
        if (Input.GetKeyUp(KeyCode.Alpha3))
        {
            Car1.SetActive(false);
            Car2.SetActive(false);
            Car3.SetActive(true);

            activeCar = Car3;
            originalCarPosition = activeCar.transform.position;
            activeCar.transform.position = scaledCarPosition;
        }

        if (Input.GetKeyUp(KeyCode.O))
        {
            AdjustCarTransforms(true);
        }
        if (Input.GetKeyUp(KeyCode.U))
        {
            AdjustCarTransforms(false);
        }
    }



    public void AdjustCarTransforms(bool bOriginalPos)
    {
        if (bOriginalPos) activeCar.transform.position = originalCarPosition;
        else activeCar.transform.position = scaledCarPosition;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarManager : MonoBehaviour
{
    private Coroutine CarManagementCor;
    private Car mCar;

    private Vector2 startPos;
    private Vector2 endPos;

    private bool bDayComplete;

    public float nowDistance { private set; get; }
    public float lastDistance{ private set; get; }
    public float distanceDifference{private set; get;}
    public float distance{private set; get;}
    public float totalDistance{private set; get;}
    public float progress{private set; get;}

    private void Start()
    {
        mCar = GetComponent<Car>();

        startPos = transform.position;
        endPos = GameObject.FindWithTag("Finish").transform.position;

        totalDistance = Mathf.Abs(endPos.x - startPos.x);
    }

    public void StartManagement()
    {
        bDayComplete = false;
        lastDistance = nowDistance;
        CarManagementCor = StartCoroutine(Managing());

    }

    public void StopManagement()
    {
        bDayComplete = true;
        nowDistance = distance;

        distanceDifference = Mathf.Abs(lastDistance - nowDistance);
    }

    private IEnumerator Managing()
    {
        while (!bDayComplete)
        {
            // Calculates the distance
            distance = Mathf.Abs(endPos.x - transform.position.x);
            // Calculates the progress (distance from the target)
            progress = 1 - (distance / totalDistance);
            yield return null;
        }
    }


}

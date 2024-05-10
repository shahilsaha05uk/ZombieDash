using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AdvancedSceneManager.Core;
using Facebook.MiniJSON;
using UnityEngine;

public struct PlayerData
{
    public float LastDistance, NowDistance, DistanceDifference, TotalDistance, Progress;
    public int ZombiesKilled, TotalZombiesKilled;
}


public class CarManager : MonoBehaviour
{
    private Coroutine CarManagementCor;
    private Car mCar;

    private Vector2 startPos;
    private Vector2 endPos;

    private bool bDayComplete;

    // Physics Properties
    public float nowDistance { private set; get; }
    public float lastDistance{ private set; get; }
    public float distanceDifference{private set; get;}
    public float distance{private set; get;}
    public float totalDistance{private set; get;}
    public float progress{private set; get;}

    // Scores
    public int TotalZombieKills { private set; get; }
    public int ZombieKills { private set; get; }

    private string GameDataPath;
    private void Start()
    {
        GameDataPath = "Assets/jsons/gameData.json";
        mCar = GetComponent<Car>();

        startPos = transform.position;
        endPos = GameObject.FindWithTag("Finish").transform.position;

        totalDistance = Mathf.Abs(endPos.x - startPos.x);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            ZombieKills++;
        }
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

        TotalZombieKills += ZombieKills;
        print("Total zombies killed this round: " + ZombieKills);

        PlayerData pData = new PlayerData
        {
            Progress = progress,
            DistanceDifference = distanceDifference,
            ZombiesKilled = ZombieKills,
            TotalDistance = totalDistance,
            TotalZombiesKilled = TotalZombieKills,
            NowDistance = nowDistance,
            LastDistance = lastDistance
        };
        string savePlayerData = JsonUtility.ToJson(pData);
        File.WriteAllText(GameDataPath, savePlayerData);

        ZombieKills = 0;
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


    public void AwardResources()
    {
        if (distanceDifference > 50)
        {
            ResourceComp.AddResources(100);
        }
        else
        {
            ResourceComp.AddResources(50);
        }
    }
}

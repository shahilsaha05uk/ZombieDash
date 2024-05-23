using System.Collections;
using System.IO;
using UnityEngine;

public struct PlayerData
{
    public int LastDistance, NowDistance, DistanceDifference, TotalDistance, Progress;
    public int ZombiesKilled, TotalZombiesKilled;
}

public class CarManager : MonoBehaviour
{
    #region Properties

    #region Privates
    private string mGoalTag = "Finish";
    private Transform mGoal;

    private Rigidbody2D rb;
    private Coroutine CarManagementCor;
    private Car mCar;
    
    private Vector2 startPos;

    private bool bDayComplete;

    #endregion

    #region Delegates

    public delegate void OnGoalReachedSignature();
    public event OnGoalReachedSignature OnGoalReached;

    #endregion

    #region Physics
    // Physics Properties
    public float nowDistance { private set; get; }
    public float lastDistance { private set; get; }
    public float distanceDifference { private set; get; }
    public float distance { private set; get; }
    public float totalDistance { private set; get; }
    public float progress { private set; get; }

    // Rigidbody vals
    public Vector2 Velocity { private set; get; }
    public float VelocityMag { private set; get; }

    #endregion


    #endregion

    // Scores
    public int TotalZombieKills { private set; get; }
    public int ZombieKills { private set; get; }

    private string GameDataPath;
    private void Start()
    {
        mGoal = GameObject.FindGameObjectWithTag(mGoalTag).transform;

        GameDataPath = "Assets/jsons/gameData.json";
        mCar = GetComponent<Car>();
        rb = GetComponent<Rigidbody2D>();
        startPos = transform.position;

        totalDistance = Mathf.Abs(mGoal.position.x - startPos.x);
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

        PlayerData pData = new PlayerData
        {
            Progress = Mathf.CeilToInt(progress),
            DistanceDifference = Mathf.CeilToInt(distanceDifference),
            ZombiesKilled = ZombieKills,
            TotalDistance = Mathf.CeilToInt(totalDistance),
            TotalZombiesKilled = TotalZombieKills,
            NowDistance = Mathf.CeilToInt(nowDistance),
            LastDistance = Mathf.CeilToInt(lastDistance)
        };
        AchievementManager.Instance.UpdateAchievement(EAchievement.ZombieKiller, TotalZombieKills);
        
        string savePlayerData = JsonUtility.ToJson(pData);
        File.WriteAllText(GameDataPath, savePlayerData);

        ZombieKills = 0;
    }

    private IEnumerator Managing()
    {
        while (!bDayComplete)
        {
            // Gets the velocity mag
            Velocity = rb.velocity;
            VelocityMag = Velocity.magnitude;

            // Calculates the distance
            distance = Mathf.Abs(mGoal.position.x - transform.position.x);
            // Calculates the progress (distance from the target)
            progress = 1 - (distance / totalDistance);


            // when the player is near the goal
            if(distance < 15f)
            {
                // near to the goal
                Debug.Log("Reached near goal");
                OnGoalReached?.Invoke();
            }
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

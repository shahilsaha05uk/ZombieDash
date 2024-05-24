using System.Collections;
using System.IO;
using StructClass;
using UnityEngine;

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
    public float mDistanceFromStart{ private set; get; }
    public float mDistanceDifferenceFromGoal { private set; get; }   // this is the distance of the player from the goal 
    public float distanceDifferenceFromLastDistance { private set; get; }   // this is the present distance of the player from the last one 
    public float totalDistance { private set; get; }
    public float progress { private set; get; }

    private Vector2 LastPos, CurrentPos;
    
    // Rigidbody vals
    public Vector2 Velocity { private set; get; }
    public float VelocityMag { private set; get; }

    #endregion


    #endregion

    // Scores
    public int TotalZombieKills { private set; get; }
    public int ZombieKills { private set; get; }
    
    // Resources
    private int lastBalanceAdded;

    private void Start()
    {
        mGoal = GameObject.FindGameObjectWithTag(mGoalTag).transform;

        rb = GetComponent<Rigidbody2D>();
        startPos = transform.position;
        totalDistance = Mathf.Abs(mGoal.position.x - startPos.x);
        LastPos = startPos;
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
        CarManagementCor = StartCoroutine(Managing());
    }

    public void StopManagement()
    {
        bDayComplete = true;
        
        TotalZombieKills += ZombieKills;
        
        // Calculations
        mDistanceFromStart = Mathf.Abs(transform.position.x - startPos.x);
        distanceDifferenceFromLastDistance = Mathf.Abs(transform.position.x - LastPos.x);

        // Adding resources to the back
        AwardResources();
        
        // Saving the values
        AchievementManager.Instance.UpdateAchievement(EAchievement.ZombieKiller, TotalZombieKills);
        SaveDataManager.Save(CreateSaveData());
        
        // Resetting the values
        ZombieKills = 0;
        LastPos = transform.position;
    }

    private IEnumerator Managing()
    {
        while (!bDayComplete)
        {
            // Gets the velocity mag
            Velocity = rb.velocity;
            VelocityMag = Velocity.magnitude;

            // Calculates the distance
            mDistanceDifferenceFromGoal = Mathf.Abs(mGoal.position.x - transform.position.x);
            // Calculates the progress (distance from the target)
            progress = 1 - (mDistanceDifferenceFromGoal / totalDistance);


            // when the player is near the goal
            if(mDistanceDifferenceFromGoal < 15f)
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
        lastBalanceAdded = (distanceDifferenceFromLastDistance > 50) ? 600 : 400;

        lastBalanceAdded = Mathf.CeilToInt(lastBalanceAdded * 1.85f);
        ResourceComp.AddResources(lastBalanceAdded);
    }

    private FPlayerData CreateSaveData()
    {
        return new FPlayerData
        {
            DistanceCovered = Mathf.CeilToInt(mDistanceFromStart),
            DistanceLeft = Mathf.CeilToInt(mDistanceDifferenceFromGoal),
            DistanceDifference = Mathf.CeilToInt(distanceDifferenceFromLastDistance),
            TotalDistance = Mathf.CeilToInt(totalDistance),
            AddedBalance = lastBalanceAdded,
            ZombiesKilled = ZombieKills,
            TotalZombiesKilled = TotalZombieKills,
        };
    }

}

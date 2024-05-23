using System;
using System.Collections;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using GooglePlayGames;
using GooglePlayGames.OurUtils;
using UnityEngine;
using UnityEngine.SocialPlatforms;


public enum EAchievement
{
    WelcomeTreat,
    ZombieKiller,
}

public class AchievementManager : ParentManager
{
    private IDictionary<EAchievement, string> mAchievementIDMap = new Dictionary<EAchievement, string>()
    {
        { EAchievement.WelcomeTreat, "CgkIx4fY-4AVEAIQAA"},
        { EAchievement.ZombieKiller, "CgkIx4fY-4AVEAIQAg"}
    };
    public static AchievementManager Instance;

    private void Awake()
    {
        if(Instance == null) Instance = this;
    }

    public bool UpdateAchievement(EAchievement AchievementToIncrement, int Value)
    {
        bool isValidID = mAchievementIDMap.TryGetValue(AchievementToIncrement, out var ID);
        if (isValidID && AchievementComplete(ID)) return false;
            
        PlayGamesPlatform.Instance.IncrementAchievement(ID, Value, success =>{});
        PlayGamesPlatform.Instance.ReportProgress(ID, Value, success =>{});
        return true;
    }
    public bool UpdateAchievement(EAchievement AchievementToIncrement)
    {
        bool isValidID = mAchievementIDMap.TryGetValue(AchievementToIncrement, out var ID);
        if (isValidID && AchievementComplete(ID)) return false;
        PlayGamesPlatform.Instance.ReportProgress(ID, 100.0f, success =>{});
        return true;
    }

    public bool AchievementComplete(string id)
    {
        bool isComplete = false;
        PlayGamesPlatform.Instance.LoadAchievements(achievements =>
        {
            if (achievements != null)
            {
                foreach (var a in achievements)
                {
                    if (String.Compare(a.id, id, StringComparison.Ordinal) == 0)
                    {
                        isComplete = a.completed;
                        break;
                    }
                }
            }
        });
        return isComplete;
    }
}

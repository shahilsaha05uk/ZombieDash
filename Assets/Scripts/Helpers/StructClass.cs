using System;
using System.Collections.Generic;
using EnumHelper;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StructClass
{
    [Serializable]
    public struct FDirectoryDetails
    {
        public string FullDirectory;
        public string FullDirectoryWithFile;
        public string FileName;
        public string Directory;
    }

    [Serializable]
    public struct FPlayerData
    {
        // Distance-related variables
        public int DistanceCovered;
        public int DistanceLeft;
        public int DistanceDifference;
        public int TotalDistance;

        // Zombie-related variables
        public int ZombiesKilled;
        public int TotalZombiesKilled;
        
        //Resources
        public int AddedBalance;
    }

    public struct FLevelDetails
    {
        public int ID;
        public string Name;
        public ELevel LevelType;
        public Scene Level;
    }
    public struct FLocationPoints
    {
        public Transform StartPos;
        public List<Transform> Checkpoints;
        public Transform EndPos;
    }
    
    [System.Serializable]
    public abstract class BaseUpgrade
    {
        public int ID;
        public int cost;

    }
    [System.Serializable]
    public class Upgrade : BaseUpgrade
    {
        public float DecreaseRate;
    }

    [System.Serializable]
    public class NonExhaustiveUpgrade : BaseUpgrade
    {
        public int Value;
    }
}
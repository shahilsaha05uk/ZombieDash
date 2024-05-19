using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using EnumHelper;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StructClass
{
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
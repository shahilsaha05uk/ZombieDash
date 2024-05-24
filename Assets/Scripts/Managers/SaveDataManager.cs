using System;
using System.IO;
using StructClass;
using UnityEngine;

public static class SaveDataManager
{
    public static bool GetPlayerData(out FPlayerData PlayerData)
    {
        PlayerData = default;
        const string filename = "SaveGame.json";
        string dir = Application.persistentDataPath + "/SaveData/";

        if (!Directory.Exists(dir) || !File.Exists(dir + filename)) return false;
        
        string data = File.ReadAllText(dir + filename);
        PlayerData = JsonUtility.FromJson<FPlayerData>(data);
        return true;
    }
    public static bool Save(object Data)
    {
        const string filename = "SaveGame.json";
        string dir = Application.persistentDataPath + "/SaveData/";

        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string json = JsonUtility.ToJson(Data, true);
        File.WriteAllText(dir + filename, json);
        return true;
    }


}
    

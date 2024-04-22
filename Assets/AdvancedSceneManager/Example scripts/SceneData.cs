using System;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using UnityEngine;

namespace AdvancedSceneManager.ExampleScripts
{

    /// <summary>Contains examples for working with <see cref="SceneData"/>.</summary>
    public static class SceneData
    {

        const string key1 = "MyKey1";
        const string key2 = "MyKey2";
        const string key3 = "MyKey3";

        [Serializable]
        class ComplexType
        {
            public string name;
            public Vector2 position;
        }

        static Scene scene => SceneManager.assets.scenes.Find("SampleScene");

        static void SetData()
        {
            SceneDataUtility.Set(scene, key1, "This is a string");
            SceneDataUtility.Set(scene, key2, new ComplexType() { name = "test", position = Vector2.one });
            scene.SetUserData(key3, "This is a string");
            scene.SetUserData("This is a string"); //Uses typeof(string).FullName as key
            scene.SetUserData(new ComplexType() { name = "test", position = Vector2.one }); //Uses typeof(ComplexType).FullName as key
        }

        static void UnsetData()
        {
            SceneDataUtility.Unset(scene, key1);
            SceneDataUtility.Unset(scene, key2);
            scene.UnsetUserData(key3); //Uses typeof(string).FullName as key
            scene.UnsetUserData<ComplexType>(); //Uses typeof(ComplexType).FullName as key
        }

        static void LogData()
        {
            var sceneData = SceneDataUtility.Enumerate<ComplexType>(key2);
            foreach (var scene in sceneData)
                Debug.Log("The following data is associated with '" + scene.scene.name + "':\n\n" + JsonUtility.ToJson(scene.data));
        }

    }

}

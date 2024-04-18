using Cinemachine;
using EnumHelper;
using StructClass;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GarageController : MonoBehaviour
{
    public GarageCar mCar;

    private void Start()
    {

    }
    private void Update()
    {
        if(Input.GetKeyUp(KeyCode.U))
        {
            CreateSceneParameters sceneParams = default;
            sceneParams.localPhysicsMode = LocalPhysicsMode.None;
            Scene s = SceneManager.CreateScene("CustomScene", sceneParams);
            //s.
        }
    }
}

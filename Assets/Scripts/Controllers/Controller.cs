using UnityEngine;

public class Controller : BaseController
{

    public static Controller Instance;

    private void Start()
    {
        if (Instance == null) Instance = this;
    }

}
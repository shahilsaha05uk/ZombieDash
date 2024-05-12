using Interfaces;
using System.Collections;
using UnityEngine;

public class destructBox : MonoBehaviour
{
    private void Update()
    {
        if(Input.GetKeyUp(KeyCode.R)) GetComponent<ResetScript>().OnReset();
    }
}

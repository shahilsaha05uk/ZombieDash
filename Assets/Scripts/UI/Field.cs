using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Field : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textRef;

    public void UpdateText(string text)
    {
        textRef.text = text;
    }
}

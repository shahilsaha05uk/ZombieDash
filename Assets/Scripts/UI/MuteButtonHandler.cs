using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MuteButtonHandler : MonoBehaviour, IPointerClickHandler
{
    private bool bIsSelected = false;

    public void OnPointerClick(PointerEventData eventData)
    {
        bIsSelected = !bIsSelected;

        if (bIsSelected)
        {
            print("Pause button selected");
        }
        else
        {
            print("Pause button deselected");
            EventSystem.current.SetSelectedGameObject(null);  // Deselect the button
        }
    }

}

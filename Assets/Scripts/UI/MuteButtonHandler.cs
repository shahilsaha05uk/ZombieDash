using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;

public class MuteButtonHandler : MonoBehaviour, IPointerClickHandler
{
    private bool bIsSelected = false;
    public AudioMixer mMixer;

    private string mVolumeParam = "VolumeParam";
    private float mDefaultVolume;

    private void Start()
    {
        mMixer.GetFloat(mVolumeParam, out mDefaultVolume);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        bIsSelected = !bIsSelected;

        if (bIsSelected)
        {
            print("Pause button selected");
            mMixer.SetFloat(mVolumeParam, -100f);
        }
        else
        {
            print("Pause button deselected");
            EventSystem.current.SetSelectedGameObject(null);  // Deselect the button
            mMixer.SetFloat(mVolumeParam, mDefaultVolume);

        }
    }

}

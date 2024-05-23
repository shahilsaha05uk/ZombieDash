using System;
using EnumHelper;
using Helpers;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private Button mResumeButtonClick;
    [SerializeField] private Button mMenuButtonClick;

    private void Awake()
    {
         mResumeButtonClick.onClick.AddListener(OnResumeButtonClick);
         mMenuButtonClick.onClick.AddListener(OnMenuButtonClick);
    }
    public void OnResumeButtonClick()
    {
        Time.timeScale = 1;
        string param = AnimationParametersDictionary.Trigger_Bottom_To_Center_Panel;
        GetComponent<Animator>().ResetTrigger(param);
    }

    public void OnMenuButtonClick()
    {
        LevelManager.Instance.OpenAdditiveScene(ELevel.MENU, true);
    }
}

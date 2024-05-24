using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using EnumHelper;
using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : BaseController
{
    private PlayerInputMappingContext mPlayerInput;
    private Car mCar;

    [SerializeField] private CinemachineVirtualCamera mVirtualCameraPrefab;
    private CinemachineVirtualCamera mVirtualCamera;

    private void Start()
    {
        InitController();

        mCar = GetComponent<Car>();
        SetupInputComponent();

        if (mVirtualCamera == null)
        {
            mVirtualCamera = Instantiate(mVirtualCameraPrefab);
            mVirtualCamera.Follow = transform;
        }
    }

    private void SetupInputComponent()
    {
        mPlayerInput = new PlayerInputMappingContext();

        mPlayerInput.Move.Right.started += Move;
        mPlayerInput.Move.Right.canceled += Move;

        mPlayerInput.Move.Roll.started += Roll;
        mPlayerInput.Move.Roll.canceled += Roll;

        mPlayerInput.Trigger.Nitro.started += Nitro;
        mPlayerInput.Trigger.Nitro.canceled += Nitro;

        mPlayerInput.UI.Pause.started += Pause;
        mPlayerInput.Enable();
    }

    private void Pause(InputAction.CallbackContext obj)
    {
        mCar.PauseGame();
        Time.timeScale = (Time.timeScale == 0)? 1 : 0;
    }

    public void ToggleInputContext(bool Value)
    {
        if (Value) mPlayerInput.Enable();
        else mPlayerInput.Disable();
    }

    private void Move(InputAction.CallbackContext InputValue)
    {
        mCar.Move(InputValue.ReadValue<float>());
    }

    private void Roll(InputAction.CallbackContext InputValue)
    {
        mCar.Roll(InputValue.ReadValue<float>());
    }

    private void Nitro(InputAction.CallbackContext InputValue)
    {
        mCar.Nitro(InputValue.ReadValueAsButton());
    }

}

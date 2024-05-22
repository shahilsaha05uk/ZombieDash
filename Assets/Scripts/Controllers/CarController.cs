using Cinemachine;
using System.Collections;
using System.Collections.Generic;
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

        mPlayerInput.Move.RightLeft.started += Move;
        mPlayerInput.Move.RightLeft.canceled += Move;

        mPlayerInput.Move.Roll.started += Roll;
        mPlayerInput.Move.Roll.canceled += Roll;

        mPlayerInput.Trigger.Nitro.started += Nitro;
        mPlayerInput.Trigger.Nitro.canceled += Nitro;

        mPlayerInput.Disable();
    }

    public void ToggleInputContext(bool Value)
    {
        if (Value) mPlayerInput.Enable();
        else mPlayerInput.Disable();
    }

    private void Move(InputAction.CallbackContext InputValue)
    {
        Debug.Log("Button input fetched!!");
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

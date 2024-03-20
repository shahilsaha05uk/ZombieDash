using System;
using System.Collections;
using System.Collections.Generic;
using EnumHelper;
using StructClass;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public abstract class BaseCar : MonoBehaviour
{
    [Header("Debugging Methods")] 
    public WheelJoint2D mFrontWheel;
    public WheelJoint2D mRearWheel;
    
    //public DebugUI mDebugUI;
    /*
    public delegate void FOnCarStatusUpdateSignature(FHudValues hudValues);
    public FOnCarStatusUpdateSignature OnCarStatusUpdate;
    
    */
    public delegate void FOnCarComponentUpdate(ECarComponent carComponent, FHudValues hudValues);
    public FOnCarComponentUpdate OnComponentUpdated;


    private Controller PC;
    protected PlayerInputMappingContext mPlayerInput;

    [Space(20)]
    [SerializeField] private Rigidbody2D frontTireRb;
    [SerializeField] private Rigidbody2D backTireRb;
    [SerializeField] private Rigidbody2D carRb;
    
    
    [Space(5)] [Header("Car Engine Manipulation")]
    [SerializeField] private float mAccelarationRate = 20f;
    [SerializeField] private float mDecelerationRate = 300f;
    [Space(5)]
    [SerializeField] private float mDragValueWhenStopping;
    [SerializeField] private float mDragValueWhenMoving;
    [Space(5)]
    [SerializeField] private float mRotateSpeed = 300f;

    [SerializeField] private double mFuelTolerance;

    [SerializeField] private float mMaxSpeed;
    private float mSpeedRate;
    private float mRotationInput;
    private float mMoveInput;

    [FormerlySerializedAs("mHud")]
    [Space(10)][Header("Development")]
    [SerializeField] private CarComponent mComponent;
    [SerializeField] private FHudValues mHudValues;

    private Coroutine mPlayerHudCoroutine;

    // On Spawn
    public void Possess(Controller controller)
    {
        PC = controller;
        
        SetupInputComponent();
        mComponent.OnComponentUpdated += OnCarComponentUpdate;
        mComponent.OnRunningOutOfResources += OnResourcesOver;
        mHudValues.nitro = mComponent.mCurrentNitro;
        mHudValues.fuel = mComponent.mCurrentFuel;
        
        OnComponentUpdated.Invoke(ECarComponent.All_Comp, mHudValues);
    }

    private void OnResourcesOver(ECarComponent resource)
    {
        string s = "Out of " + resource;
        DebugUI.OnMessageUpdate?.Invoke(s);
        
        if(resource == ECarComponent.Fuel) mPlayerInput.Move.Disable();
    }

    private void OnCarComponentUpdate(ECarComponent carComponent, float value)
    {
        switch (carComponent)
        {
            case ECarComponent.Fuel:
            {
                float fuelDifference = Mathf.Abs(value - mHudValues.fuel);
                if (fuelDifference > mFuelTolerance || value <= 0.0f)
                {
                    Debug.Log("Hud Fuel: " + mHudValues.fuel + " Recieved Value: " + value + " Difference: " + fuelDifference);
                    OnComponentUpdated.Invoke(carComponent, mHudValues);
                    
                    mHudValues.UpdateValue(carComponent, value);
                }
            }
                break;
            case ECarComponent.Nitro:
                break;
        }
    }

    private void SetupInputComponent()
    {
        mPlayerInput = new PlayerInputMappingContext();
        
        mPlayerInput.Move.RightLeft.started += Move;
        mPlayerInput.Move.RightLeft.canceled += Move;
        
        mPlayerInput.Move.Roll.started += Roll;
        mPlayerInput.Move.Roll.canceled += Roll;

        mPlayerInput.Move.Nitro.started += Nitro;
        mPlayerInput.Move.Nitro.canceled += Nitro;
        
        mPlayerInput.Enable();
    }

    private void Update()
    {
        Accelarate();
        Decelarate();
        Rotate();
        
        // Updating the Car Values
        /*
        float currentDistance = Mathf.Abs(flag.transform.position.x - player.transform.position.x);
        float progress = 1 - (currentDistance / totalDistance);
        mPlayerProgress.value = progress;

        */
    }

    // Updaters
    // Control Bindings
    private void Move(InputAction.CallbackContext InputValue)
    {
        mMoveInput = InputValue.ReadValue<float>();
        
        if (mMoveInput == 0f) {
            mSpeedRate = mDecelerationRate;
            carRb.drag = mDragValueWhenStopping;
            mComponent.StopFuelConsumption();
        }
        else {
            mSpeedRate = mAccelarationRate;
            carRb.drag = mDragValueWhenMoving;
            mComponent.StartFuelConsumption();
        }
        
        DebugUI.OnMoveInputUpdate?.Invoke(mMoveInput);
        DebugUI.OnSpeedRateUpdate?.Invoke(mSpeedRate);
    }
    
    private void Roll(InputAction.CallbackContext InputValue)
    {
        mRotationInput = InputValue.ReadValue<float>();
    }

    private void Nitro(InputAction.CallbackContext InputValue)
    {
        bool mShouldConsumeNitro = InputValue.ReadValueAsButton();

        if(mShouldConsumeNitro) mComponent.StartNitroConsumption();
        else mComponent.StopNitroConsumption();
    }
    
    // Action Methods
    private void Accelarate()
    {
        /* Original
        float torqueVal = -mMoveInput * mSpeedRate;
        
        frontTireRb.AddTorque(torqueVal);
        backTireRb.AddTorque(torqueVal);
    */
        
        // Calculate the current velocity
        float currentVelocity = carRb.velocity.magnitude;

        // Calculate the torque based on the difference between the current velocity and the desired limit
        float torqueVal = Mathf.Clamp(mMaxSpeed - currentVelocity, 0f, 1f) * mAccelarationRate;
        
        // Apply torque to accelerate
        frontTireRb.AddTorque(-mMoveInput * torqueVal);
        backTireRb.AddTorque(-mMoveInput * torqueVal);

        carRb.velocity = Vector2.ClampMagnitude(carRb.velocity, mMaxSpeed);
        
        float speed = carRb.velocity.magnitude * 3.6f;
        int speedInt = Mathf.RoundToInt(speed);
        DebugUI.OnSpeedUpdate?.Invoke(speedInt);

        //StartCoroutine(mComponent.UpdateFuel());
    }

    private void Decelarate()
    {
        if (mMoveInput != 0 || carRb.velocity.magnitude < 0.1f) return;

        mFrontWheel.breakTorque = mDecelerationRate;
        mRearWheel.breakTorque = mDecelerationRate;
    }

    private void Rotate()
    {
        carRb.AddTorque(-mRotationInput * mRotateSpeed * Time.fixedDeltaTime);
    }


}

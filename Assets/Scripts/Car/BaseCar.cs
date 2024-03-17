using System;
using System.Collections;
using System.Collections.Generic;
using StructClass;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public abstract class BaseCar : MonoBehaviour
{
    public delegate void FOnCarStatusUpdateSignature(FHudValues hudValues);

    public FOnCarStatusUpdateSignature OnCarStatusUpdate;
    
    private Controller PC;
    protected PlayerInputMappingContext mPlayerInput;

    [SerializeField] private Rigidbody2D frontTireRb;
    [SerializeField] private Rigidbody2D backTireRb;
    [SerializeField] private Rigidbody2D carRb;
    
    
    [Space(5)] [Header("Car Engine Manipulation")]
    [SerializeField] private float mAccelarationRate = 150f;
    [SerializeField] private float mDecelarationRate = 300f;
    [Space(5)]
    [SerializeField] private float mDragValueWhenStopping;
    [SerializeField] private float mDragValueWhenMoving;
    [Space(5)]
    [SerializeField] private float mRotateSpeed = 300f;

    [Space(10)] [Header("Car Feature Modification")] 
    [Space(5)] [Header("Fuel")] 
    [SerializeField] private float mTotalFuel = 10f;
    [SerializeField] private float mCurrentFuel = 10f;
    [SerializeField] private float mFuelDecreaseRate = 0.1f;
    [SerializeField] private float mFuelDecreaseInterval = 0.01f;
    [SerializeField] private double mFuelTolerance;
    private bool mShouldConsumeFuel = false;
    
    
    [Space(5)]
    [SerializeField] private FHudValues mHudValues;
    
    private float mSpeedRate;
    private float mRotationInput;
    private float mMoveInput;

    [Space(10)][Header("Development")]
    [SerializeField] private float mHudUpdateTimeInterval = 0.1f;
    [SerializeField] private float mInvokeTolerance = 1f;
    private Coroutine mPlayerHudCoroutine;

    private void Start()
    {
        mCurrentFuel = mTotalFuel;
        
    }

    // On Spawn
    public void Possess(Controller controller)
    {
        PC = controller;
        
        SetupInputComponent();

        mPlayerHudCoroutine = StartCoroutine(HudUpdater());
    }

    private void SetupInputComponent()
    {
        mPlayerInput = new PlayerInputMappingContext();
        
        mPlayerInput.Move.RightLeft.started += Move;
        mPlayerInput.Move.RightLeft.canceled += Move;
        
        mPlayerInput.Move.Roll.started += Roll;
        mPlayerInput.Move.Roll.canceled += Roll;
        
        mPlayerInput.Enable();
    }

    private void FixedUpdate()
    {
        Accelarate();
        Rotate();
    }
    
    // Updaters
    private IEnumerator HudUpdater()
    {
        WaitForSeconds timeInterval = new WaitForSeconds(mHudUpdateTimeInterval);
        while (true)
        {
            yield return timeInterval;

            float distanceDifference = Vector2.Distance(mHudValues.position, transform.position);
            float speedDifference = Mathf.Abs(mHudValues.speed - carRb.velocity.magnitude);
            float fuelDifference = mCurrentFuel - mHudValues.fuel;
            
            if (distanceDifference > mInvokeTolerance || 
                speedDifference > mInvokeTolerance || 
                fuelDifference > mFuelTolerance)
            {
                //TODO: Invoke the event
                Debug.Log("Invoke the event to update the HUD");
                OnCarStatusUpdate?.Invoke(mHudValues);
            }
        }
    }
    
    // Control Bindings
    private void Move(InputAction.CallbackContext obj)
    {
        mMoveInput = obj.ReadValue<float>();

        if (mMoveInput == 0f) {
            mSpeedRate = mDecelarationRate;
            carRb.drag = mDragValueWhenStopping;
            mShouldConsumeFuel = false;
        }
        else {
            mSpeedRate = mAccelarationRate;
            carRb.drag = mDragValueWhenMoving;
            mShouldConsumeFuel = true;
            StartCoroutine(UpdateFuel());
        }
    }
    
    private void Roll(InputAction.CallbackContext obj)
    {
        mRotationInput = obj.ReadValue<float>();
    }

    // Action Methods
    private void Accelarate()
    {
        float torqueVal = -mMoveInput * mSpeedRate * Time.fixedDeltaTime;
        frontTireRb.AddTorque(torqueVal);
        backTireRb.AddTorque(torqueVal);
        
        // Updating the Car Values
        /*
        float currentDistance = Mathf.Abs(flag.transform.position.x - player.transform.position.x);
        float progress = 1 - (currentDistance / totalDistance);
        mPlayerProgress.value = progress;

        */
        
        mHudValues.UpdatePosition(transform.position);
        mHudValues.speed = carRb.velocity.magnitude;
    }

    private void Rotate()
    {
        carRb.AddTorque(-mRotationInput * mRotateSpeed * Time.fixedDeltaTime);
    }

    private IEnumerator UpdateFuel()
    {
        WaitForSeconds timeInterval = new WaitForSeconds(mFuelDecreaseInterval);
        while (mCurrentFuel > 0f && mShouldConsumeFuel)
        {
            mCurrentFuel -= mFuelDecreaseRate;
            mHudValues.fuel = 1 - (mCurrentFuel / mTotalFuel);
            yield return timeInterval;
        }
    }
}

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
    [Header("Debugging Values")] 
    [SerializeField] private float mNitroImpulse = 100f;
    [SerializeField] private float mNitroForce = 100f;
    [SerializeField][Range(0,1)] private float mNitroTimeInterval;
    [SerializeField][Range(0,5)] private float mDragValueWhenBoosting;
    [SerializeField][Range(0.1f, 10)] private int mCarMass;
    [SerializeField][Range(0,5)] private float mDragValueStopBoosting;
    [SerializeField] private bool bApplyToDrag;


    public delegate void FOnCarComponentUpdate(ECarComponent carComponent, FHudValues hudValues);
    public FOnCarComponentUpdate OnComponentUpdated;

    // Privates
    private Coroutine mPlayerHudCoroutine;
    private float mSpeedRate;
    private float mRotationInput;
    private float mMoveInput;
    private Controller PC;
    protected PlayerInputMappingContext mPlayerInput;

    [Space(20)][Header("References")]
    [SerializeField] private Rigidbody2D frontTireRb;
    [SerializeField] private Rigidbody2D backTireRb;
    [SerializeField] private Rigidbody2D carRb;
    public WheelJoint2D mFrontWheel;
    public WheelJoint2D mRearWheel;

    
    [Space(5)] [Header("Car Engine Manipulation")]
    public FHudValues mHudValues;
    public JointMotor2D motor2D;

    [Space(5)]
    [SerializeField] private float mAccelarationRate = 20f;
    [SerializeField] private float mDecelerationRate = 300f;
    private bool bApplyNitro;
    [Space(5)]
    [SerializeField] private float mDragValueWhenStopping;
    [SerializeField] private float mDragValueWhenMoving;
    
    [Space(5)]
    [SerializeField] private float mRotateSpeed = 300f;
    [SerializeField] private float mMaxSpeed;

    [Space(5)] [Header("Tolerance Properties")]
    [SerializeField] private float mFuelTolerance;
    [SerializeField] private float mNitroTolerance;
    
    [Space(10)][Header("Car Components")]
    [SerializeField] private CarComponent mComponent;



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
    
    private void SetupInputComponent()
    {
        mPlayerInput = new PlayerInputMappingContext();
        
        mPlayerInput.Move.RightLeft.started += Move;
        mPlayerInput.Move.RightLeft.canceled += Move;
        
        mPlayerInput.Move.Roll.started += Roll;
        mPlayerInput.Move.Roll.canceled += Roll;

        mPlayerInput.Trigger.Nitro.started += Nitro;
        mPlayerInput.Trigger.Nitro.canceled += Nitro;
        
        mPlayerInput.Enable();
    }

    private void FixedUpdate()
    {
        carRb.mass = mCarMass;
        
        Accelarate();
        Decelarate();
        Rotate();
    }
    
    // Event Binded Methods
    private void OnResourcesOver(ECarComponent resource)
    {
        /*
        string s = "Out of " + resource;
        DebugUI.OnMessageUpdate?.Invoke(s);
        if(resource == ECarComponent.Fuel) mPlayerInput.Move.Disable();
        if(resource == ECarComponent.Nitro) mPlayerInput.Trigger.Disable();
    */
        PC.OpenUpgradeUI();
    }

    private void OnCarComponentUpdate(ECarComponent carComponent, float value)
    {
        float tollerance = 0, hudVal = 0;
        
        switch (carComponent) {
            case ECarComponent.Fuel:
                
                tollerance = mFuelTolerance;
                hudVal = mHudValues.fuel;
                break;
            
            case ECarComponent.Nitro:
                tollerance = mNitroTolerance;
                hudVal = mHudValues.nitro;
                break;
        }
        
        var difference = Mathf.Abs(value - hudVal);
        if (difference > tollerance || value <= 0.0f) {
            OnComponentUpdated.Invoke(carComponent, mHudValues);
            mHudValues.UpdateValue(carComponent, value);
        }
    }

    
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
        bApplyNitro = InputValue.ReadValueAsButton();
        if (bApplyNitro)
        {
            bApplyNitro = true;
            StartCoroutine(Boost());
        }
        else
        {
            bApplyNitro = false;
        }
    }
    
    // Action Methods
    private void Accelarate()
    {
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

    private IEnumerator Boost()
    {
        if (bApplyNitro)
        {
            WaitForSeconds mTimeInterval = new WaitForSeconds(mNitroTimeInterval);
            carRb.drag = mDragValueWhenBoosting;
            
            if (!bApplyToDrag) carRb.drag = mDragValueWhenBoosting;
            else carRb.angularDrag = mDragValueWhenBoosting;

            frontTireRb.AddForce(Vector2.right * mNitroImpulse, ForceMode2D.Impulse);
            backTireRb.AddForce(Vector2.right * mNitroImpulse, ForceMode2D.Impulse);
            while (bApplyNitro)
            {
                frontTireRb.AddForce(Vector2.right * (Time.deltaTime * mNitroForce), ForceMode2D.Force);
                backTireRb.AddForce(Vector2.right * (Time.deltaTime * mNitroForce), ForceMode2D.Force);
                mComponent.UpdateNitro();
                yield return mTimeInterval;
            }

        }
        if (!bApplyToDrag) carRb.drag = mDragValueStopBoosting;
        else carRb.angularDrag = mDragValueStopBoosting;
    }
}

/*
 
     
     Accelaration Original:

        float torqueVal = -mMoveInput * mSpeedRate;
        
        frontTireRb.AddTorque(torqueVal);
        backTireRb.AddTorque(torqueVal);

     -----------------------------------------------
     private IEnumerator HudUpdater()
    {
        WaitForSeconds timeInterval = new WaitForSeconds(mHudUpdateTimeInterval);
        while (true)
        {
            yield return timeInterval;

            /*
            float distanceDifference = Vector2.Distance(mHudValues.position, transform.position);
            float speedDifference = Mathf.Abs(mHudValues.speed - carRb.velocity.magnitude);
            float fuelDifference = mCurrentFuel - mHudValues.fuel;
            
            if (distanceDifference > mInvokeTolerance || 
                speedDifference > mInvokeTolerance || 
                fuelDifference > mFuelTolerance)
            {
                //TODO: Invoke the event
                Debug.Log("Invoke the event to update the HUD");
               // OnCarStatusUpdate?.Invoke(mHudValues);
            }
*/
        
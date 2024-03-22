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


    public delegate void FOnCarComponentUpdate(ECarPart carPart, FHudValues hudValues);
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
    private bool bConsumeFuel;
    private bool bIsFuelOver;

    [Space(5)]
    [SerializeField] private float mAccelarationRate = 20f;
    [SerializeField] private float mDecelerationRate = 300f;
    private bool bApplyNitro;
    private bool bIsNitroOver;
    [Space(5)]
    [SerializeField] private float mDragValueWhenStopping;
    [SerializeField] private float mDragValueWhenMoving;
    
    [Space(5)]
    [SerializeField] private float mRotateSpeed = 300f;
    [SerializeField] private float mMaxSpeed;
    [SerializeField] private float mExtraForce = 0.5f;

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

        float fuel = mComponent.GetCurrentPartValue(ECarPart.Fuel, out var fuelValid);
        if(fuelValid) mHudValues.fuel = fuel;
        float nitro = mComponent.GetCurrentPartValue(ECarPart.Nitro, out var nitroValid);
        if(nitroValid) mHudValues.nitro = nitro;
        
        OnComponentUpdated.Invoke(ECarPart.All_Comp, mHudValues);
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
        
        Rotate();
    }
    
    // Event Binded Methods
    private IEnumerator OnResourcesOver(ECarPart resource)
    {
        /*
        string s = "Out of " + resource;
        DebugUI.OnMessageUpdate?.Invoke(s);
        if(resource == ECarComponent.Fuel) mPlayerInput.Move.Disable();
        if(resource == ECarComponent.Nitro) mPlayerInput.Trigger.Disable();
    */

        switch (resource)
        {
            case ECarPart.Fuel:
                bIsFuelOver = true;
                break;
            case ECarPart.Nitro:
                bIsNitroOver = true;
                break;
        }

        if (bIsFuelOver && bIsNitroOver)
        {
            mPlayerInput.Move.Disable();
            PC.OpenUpgradeUI();
            
        }
    }

    private void OnCarComponentUpdate(ECarPart carPart, float value)
    {
        float tollerance = 0, hudVal = 0;
        
        switch (carPart) {
            case ECarPart.Fuel:
                
                tollerance = mFuelTolerance;
                hudVal = mHudValues.fuel;
                break;
            
            case ECarPart.Nitro:
                tollerance = mNitroTolerance;
                hudVal = mHudValues.nitro;
                break;
        }
        
        var difference = Mathf.Abs(value - hudVal);
        if (difference > tollerance || value <= 0.0f) {
            OnComponentUpdated.Invoke(carPart, mHudValues);
            mHudValues.UpdateValue(carPart, value);
        }
    }

    
    // Control Bindings
    private void Move(InputAction.CallbackContext InputValue)
    {
        mMoveInput = InputValue.ReadValue<float>();
        
        if (mMoveInput == 0f) {
            mSpeedRate = mDecelerationRate;
            carRb.drag = mDragValueWhenStopping;
            bConsumeFuel = false;
            StartCoroutine(Decelarate());
        }
        else {
            mSpeedRate = mAccelarationRate;
            carRb.drag = mDragValueWhenMoving;
            bConsumeFuel = true;
            StartCoroutine(Accelarate());
        }
        
        DebugUI.OnMoveInputUpdate?.Invoke(mMoveInput);
        //DebugUI.OnSpeedRateUpdate?.Invoke(mSpeedRate);
    }
    
    private void Roll(InputAction.CallbackContext InputValue)
    {
        mRotationInput = InputValue.ReadValue<float>();
    }

    private void Nitro(InputAction.CallbackContext InputValue)
    {
        if ((bApplyNitro = InputValue.ReadValueAsButton()) == true)
        {
            StartCoroutine(Boost());
        }
    }
    
    // Action Methods
    private IEnumerator Accelarate()
    {
        WaitForSeconds timeInterval = new WaitForSeconds(0.002f);
        while (bConsumeFuel && !bIsFuelOver)
        {
            // Calculate the current velocity
            float currentVelocity = carRb.velocity.magnitude * 3.6f;

            // Calculate the torque based on the difference between the current velocity and the desired limit
            float torqueVal = Mathf.Clamp(mMaxSpeed - currentVelocity, 0f, mMaxSpeed) * mAccelarationRate;

            // Apply torque to accelerate
            frontTireRb.AddTorque(-mMoveInput * torqueVal);
            backTireRb.AddTorque(-mMoveInput * torqueVal);

            carRb.velocity = Vector2.ClampMagnitude(carRb.velocity, mMaxSpeed);
            carRb.AddForce(Vector2.right * mExtraForce, ForceMode2D.Force);
            //float speed = currentVelocity * 3.6f;
            int speedInt = Mathf.RoundToInt(currentVelocity);
            DebugUI.OnSpeedUpdate?.Invoke(speedInt);

            mComponent.UpdateValue(ECarPart.Fuel);
            yield return timeInterval;
        }
    }

    private IEnumerator Decelarate()
    {
        WaitForSeconds timeInterval = new WaitForSeconds(0.002f);

        while (carRb.velocity.magnitude > 0.1f)
        {
            mFrontWheel.breakTorque = mDecelerationRate;
            mRearWheel.breakTorque = mDecelerationRate;

            yield return timeInterval;
        }

        carRb.velocity = Vector2.zero;
    }

    private void Rotate()
    {
        carRb.AddTorque(-mRotationInput * mRotateSpeed * Time.fixedDeltaTime);
    }

    private IEnumerator Boost()
    {
        if (bApplyNitro && !bIsNitroOver)
        {
            WaitForSeconds mTimeInterval = new WaitForSeconds(mNitroTimeInterval);
            carRb.drag = mDragValueWhenBoosting;
            
            if (!bApplyToDrag) carRb.drag = mDragValueWhenBoosting;
            else carRb.angularDrag = mDragValueWhenBoosting;

            while (bApplyNitro)
            {
                frontTireRb.AddForce(Vector2.right * mNitroImpulse, ForceMode2D.Impulse);
                backTireRb.AddForce(Vector2.right * mNitroImpulse, ForceMode2D.Impulse);
                mComponent.UpdateValue(ECarPart.Nitro);

                yield return mTimeInterval;
            }

        }
        if (!bApplyToDrag) carRb.drag = mDragValueStopBoosting;
        else carRb.angularDrag = mDragValueStopBoosting;
    }
    
    public void Upgrade(ECarPart carcomp, Upgrade upgradestruct)
    {
        if (carcomp == ECarPart.Speed)
        {
            mExtraForce = upgradestruct.Value;
            string s = "Upgraded " + carcomp;
            Debug.Log(s);
            DebugUI.OnMessageUpdate.Invoke(s);

        }
        
        //mComponent.UpgradePart(carcomp, upgradestruct);
    }
}
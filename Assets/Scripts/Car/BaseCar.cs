using System;
using System.Collections;
using System.Collections.Generic;
using StructClass;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public abstract class BaseCar : MonoBehaviour
{
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
    
    [Space(5)]
    [SerializeField] private FHudValues mCarValues;
    
    private float mSpeedRate;
    private float mRotationInput;
    private float mMoveInput;

    [Space(10)][Header("Development")]
    [SerializeField] private float mHudUpdateTimeInterval = 0.1f;
    [FormerlySerializedAs("mPositionTollerance")] [SerializeField] private double mInvokeTollerance;

    // On Spawn
    public void Possess(Controller controller)
    {
        PC = controller;
        
        SetupInputComponent();
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

            float distanceDifference = Vector2.Distance(mCarValues.position, transform.position);
            float speedDifference = Mathf.Abs(mCarValues.speed - carRb.velocity.magnitude);
            if (Math.Abs(mCarValues.position - transform.position.magnitude) > mInvokeTollerance || // Checking the distance
                Math.Abs(mCarValues.speed - carRb.velocity.magnitude) > mInvokeTollerance)          // Checking the car Speed
            {
                //TODO: Invoke the event
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
        }
        else {
            mSpeedRate = mAccelarationRate;
            carRb.drag = mDragValueWhenMoving;
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
        mCarValues.position = transform.position.magnitude;
        mCarValues.speed = carRb.velocity.magnitude;
    }

    private void Rotate()
    {
        carRb.AddTorque(-mRotationInput * mRotateSpeed * Time.fixedDeltaTime);
    }
    
    // Getters
    public FHudValues GetStatus() { return mCarValues; }

}

using System;
using System.Collections;
using System.Numerics;
using EnumHelper;
using StructClass;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.PlayerLoop;
using UnityEngine.Serialization;
using Vector2 = UnityEngine.Vector2;

public abstract class BaseCar : MonoBehaviour
{
    [Header("Debugging Values")] 
    [SerializeField][Range(0.1f, 10)] private int mDefaultCarMass;

    [SerializeField] private float mNitroImpulse = 100f;
    [SerializeField][Range(0,1)] private float mNitroTimeInterval;

    public delegate void FOnCarComponentUpdate(ECarPart carPart, FHudValues hudValues);
    public FOnCarComponentUpdate OnComponentUpdated;

    // Privates
    private Coroutine mPlayerHudCoroutine;
    private float mRotationInput;
    private float mMoveInput;

    private ECarState mCarState;

    private Vector2 mCurrentVelocity;
    private float mCurrentVelocityMag;

    private Controller PC;
    protected PlayerInputMappingContext mPlayerInput;
    

    [Space(20)][Header("References")]
    [SerializeField] private Rigidbody2D frontTireRb;
    [SerializeField] private GameObject mNitro;

    [SerializeField] private Rigidbody2D backTireRb;
    [SerializeField] private Rigidbody2D carRb;
    
    [Space(5)] [Header("Car Engine Manipulation")]
    public FHudValues mHudValues;
    private bool bConsumeFuel;
    private bool bIsFuelOver;

    [Space(5)]
    [SerializeField] private float mAccelarationRate = 20f;
    [SerializeField] private float mDecelerationRate = 300f;
    private bool bApplyNitro;
    private bool bIsNitroOver;
    
    [Space(5)]
    [SerializeField] private float mRotateSpeed = 300f;
    [SerializeField] private float mMaxSpeed;
    [SerializeField] private float mExtraForce = 0.5f;

    [Space(5)] [Header("Tolerance Properties")]
    [SerializeField] private float mFuelTolerance;
    [SerializeField] private float mNitroTolerance;
    
    [Space(10)][Header("Car Components")]
    [SerializeField] private CarComponent mComponent;

    [SerializeField] private float mGroundClearance;
    [SerializeField] private bool bIsOnGround;
    private bool bLastGroundCheck;
    [SerializeField] private LayerMask mGroundLayer;


    // On Spawn
    public void Possess(Controller controller)
    {
        mMoveInput = 0f;

        PC = controller;
        mCarState = ECarState.Idle;
        SetupInputComponent();
        mComponent.OnComponentUpdated += OnCarComponentUpdate;
        mComponent.OnRunningOutOfResources += OnRunningOutOfResourcesOver;

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
        // Ground Check
        Vector2 position = transform.position;
        Vector2 direction = Vector2.down;
        Debug.DrawRay(position, direction * mGroundClearance, Color.cyan, Time.fixedDeltaTime);
        RaycastHit2D hit = Physics2D.Raycast(position, direction, mGroundClearance, mGroundLayer);
        bIsOnGround = (hit.collider != null);
        
        //TODO: if not on ground
        if (bIsOnGround != bLastGroundCheck)
        {
            bLastGroundCheck = bIsOnGround;

            if (bIsOnGround)
            {
                //mRotateSpeed = mRotationSpeedOnGround;
            }
            else
            {
                //mRotateSpeed = mRotationSpeedInAir;
            }
        }
        
        
        Rotate();

        mCurrentVelocity = new Vector2(carRb.velocity.x, 0f);
        mCurrentVelocityMag = mCurrentVelocity.magnitude;

        int speedInt = Mathf.RoundToInt(mCurrentVelocityMag);
        DebugUI.OnSpeedUpdate?.Invoke(speedInt);

        if(mMoveInput > 0) Accelarate();
        else Decelarate();
    }
    
    // Control Bindings
    private void Move(InputAction.CallbackContext InputValue)
    {
        mMoveInput = InputValue.ReadValue<float>();
        
        if (mMoveInput != 0f) {
            bConsumeFuel = true;
        }
        else {
            bConsumeFuel = false;
        }
        
        DebugUI.OnMoveInputUpdate?.Invoke(mMoveInput);
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
        else
        {
            carRb.mass = mDefaultCarMass;
        }
    }
    
    // Action Methods
    private void Accelarate()
    {
        if (bConsumeFuel && !bIsFuelOver)
        {
            float torqueVal = Mathf.Clamp(mMaxSpeed - mCurrentVelocityMag, 0f, mMaxSpeed) * mAccelarationRate;

            mComponent.UpdateValue(ECarPart.Fuel);

            if (bIsOnGround)
            {
                frontTireRb.AddTorque(-mMoveInput * torqueVal);
                backTireRb.AddTorque(-mMoveInput * torqueVal);
            }

            carRb.AddForce(Vector2.right * (mExtraForce * mMoveInput), ForceMode2D.Force);
            DebugUI.OnMessageUpdate("Accelarating");
        }
    }

    private void Decelarate()
    {
        if(mCurrentVelocityMag > 0f){
            carRb.AddForce(-carRb.velocity * mDecelerationRate); // Decelerate immediately
        } 

        DebugUI.OnMessageUpdate("Decelarating");
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

            Vector2 nitroThrustPos = mNitro.transform.up * -1f;;
            while (bApplyNitro)
            {
                carRb.AddForce(nitroThrustPos * mNitroImpulse, ForceMode2D.Force);
                mComponent.UpdateValue(ECarPart.Nitro);

                yield return mTimeInterval;
            }
        }
        
    }
    
        // Event Binded Methods
    private void OnRunningOutOfResourcesOver(ECarPart resource)
    {
        switch (resource)
        {
            case ECarPart.Fuel:
                bIsFuelOver = true;
                break;
            case ECarPart.Nitro:
                bIsNitroOver = true;
                break;
        }
        
        StartCoroutine(OnResourcesOver());
    }

    private IEnumerator OnResourcesOver()
    {
        if (bIsFuelOver && bIsNitroOver)
        {
            mPlayerInput.Move.Disable();
            yield return new WaitForSeconds(5);

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
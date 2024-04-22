using System;
using System.Collections;
using System.Collections.Generic;
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
    private NitroComp mNitroComp;

    [Header("Debugging Values")] 
    [SerializeField][Range(0.1f, 10)] private int mDefaultCarMass;

    public delegate void FOnCarComponentUpdate(ECarPart carPart, FCarMetrics hudValues);
    public FOnCarComponentUpdate OnComponentUpdated;

    // Privates
    private Coroutine mPlayerHudCoroutine;
    private float mRotationInput;
    private float mMoveInput;
    private IDictionary<ECarPart, bool> mExhaustedParts;

    private Vector2 mCurrentVelocity;
    private float mCurrentVelocityMag;

    private Controller PC;
    protected PlayerInputMappingContext mPlayerInput;
    
    [Space(20)][Header("References")]
    [SerializeField] private Rigidbody2D frontTireRb;
    [SerializeField] private Rigidbody2D backTireRb;
    [SerializeField] private Rigidbody2D carRb;
    
    [SerializeField] public FCarMetrics mCarMetrics;

    [Space(5)]
    [SerializeField] private float mAccelarationRate = 20f;
    [SerializeField] private float mDecelerationRate = 300f;
    
    [Space(5)]
    [SerializeField] private float mRotateSpeed = 300f;
    [SerializeField] private float mMaxSpeed;
    [SerializeField] private float mExtraForce = 0.5f;

    [SerializeField] private float mGroundClearance;
    [SerializeField] private bool bIsOnGround;
    private bool bLastGroundCheck;
    [SerializeField] private LayerMask mGroundLayer;

    private void Awake()
    {
        mExhaustedParts = new Dictionary<ECarPart, bool>() { { ECarPart.Fuel, false}, { ECarPart.Nitro, false} };
        
        mNitroComp = GetComponent<NitroComp>();

        CarComponent.OnRunningOutOfResources += OnPartExhausted;
    }

    private void OnPartExhausted(ECarPart exhaustedPart)
    {
        if (mExhaustedParts.ContainsKey(exhaustedPart))
            mExhaustedParts[exhaustedPart] = true;

        int partsCount = 0;
        foreach (var p in mExhaustedParts)
        {
            if (p.Value == true) partsCount++;
        }

        if(partsCount == mExhaustedParts.Count)
        {
            Debug.Log("All parts exhausted");

            mPlayerInput.Move.Disable();
            //PC.DayComplete();
        }
    }

    // On Spawn
    public void Possess(Controller controller)
    {
        mMoveInput = 0f;

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
        
        Rotate();

        mCurrentVelocity = new Vector2(carRb.velocity.x, 0f);
        mCurrentVelocityMag = mCurrentVelocity.magnitude;

        //int speedInt = Mathf.RoundToInt(mCurrentVelocityMag);
        //DebugUI.OnSpeedUpdate?.Invoke(speedInt);

        if(mMoveInput > 0) Accelarate();
        else Decelarate();
    }
    public void UpdateCarMetrics(ECarPart carPart, float value)
    {
        FCarMetrics.UpdateMetricValue(ref mCarMetrics, carPart, value);
        //OnComponentUpdated.Invoke(carPart, mCarMetrics);
    }


    // Control Bindings
    private void Move(InputAction.CallbackContext InputValue)
    {
        mMoveInput = InputValue.ReadValue<float>();  
    }
    
    private void Roll(InputAction.CallbackContext InputValue)
    {
        mRotationInput = InputValue.ReadValue<float>();
    }

    private void Nitro(InputAction.CallbackContext InputValue)
    {
        if (InputValue.ReadValueAsButton() && mExhaustedParts[ECarPart.Nitro] != true) mNitroComp.StartComponent();
        else carRb.mass = mDefaultCarMass;
    }

    // Action Methods
    private void Accelarate()
    {
        if (mExhaustedParts[ECarPart.Fuel] == true) return;

        float torqueVal = Mathf.Clamp(mMaxSpeed - mCurrentVelocityMag, 0f, mMaxSpeed) * mAccelarationRate;

        if (bIsOnGround)
        {
            frontTireRb.AddTorque(-mMoveInput * torqueVal);
            backTireRb.AddTorque(-mMoveInput * torqueVal);
        }

        carRb.AddForce(Vector2.right * (mExtraForce * mMoveInput), ForceMode2D.Force);
    }

    private void Decelarate()
    {
        if(mCurrentVelocityMag > 0f){
            carRb.AddForce(-carRb.velocity * mDecelerationRate); // Decelerate immediately
        } 
    }

    private void Rotate()
    {
        carRb.AddTorque(-mRotationInput * mRotateSpeed * Time.fixedDeltaTime);
    }
    public void Upgrade(ECarPart carcomp, Upgrade upgradestruct)
    {
        if (carcomp == ECarPart.Speed)
        {
            mExtraForce = upgradestruct.Value;
        }
        
        //mComponent.UpgradePart(carcomp, upgradestruct);
    }
}
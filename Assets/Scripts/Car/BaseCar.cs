using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Cinemachine;
using EnumHelper;
using StructClass;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.PlayerLoop;
using UnityEngine.Serialization;
using Vector2 = UnityEngine.Vector2;

public abstract class BaseCar : MonoBehaviour
{
    
    #region Properties
    //[Header("Debugging Values")] 

    // Dictionary that stores all the Components connected to the car
    protected IDictionary<ECarPart, CarComponent> ComponentsDic = new Dictionary<ECarPart, CarComponent>();
    protected IDictionary<ECarPart, bool> mExhaustedParts = new Dictionary<ECarPart, bool>();
    
    public delegate void FOnCarComponentUpdate(ECarPart carPart, float value);
    public FOnCarComponentUpdate OnComponentUpdated;

    // Privates
    public int ID;
    
    protected PlayerInputMappingContext mPlayerInput;

    private Transform startPos;
    [SerializeField]protected Transform endPos;
    
    protected float mTotalDistance;
    private Coroutine mPlayerHudCoroutine;
    private float mRotationInput;
    private float mMoveInput;

    private Vector2 mCurrentVelocity;
    private float mCurrentVelocityMag;
    
    
    [Space(20)][Header("References")]
    [SerializeField] protected Rigidbody2D frontTireRb;
    [SerializeField] protected Rigidbody2D backTireRb;
    [SerializeField] protected Rigidbody2D carRb;
    
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

    [SerializeField] private CinemachineVirtualCamera mVirtualCameraPrefab;
    private CinemachineVirtualCamera mVirtualCamera;


    #endregion

    #region Overridables

    protected virtual void OnStartDrive()
    {
        
    }
    protected virtual void OnResourcesExhausted(){}
    
    #endregion

    #region Initialisers

    protected virtual void Awake()
    {
        CarComponent.OnRunningOutOfResources += OnPartExhausted;
        
        startPos = GameManager.GetPlayerStart().transform;
        endPos = GameObject.FindWithTag("Finish").transform;
        
        mTotalDistance = Mathf.Abs(endPos.position.x - startPos.position.x);
        
        SetupInputComponent();

        if (mVirtualCamera == null)
        {
            mVirtualCamera = Instantiate(mVirtualCameraPrefab);
            mVirtualCamera.Follow = transform;
        }
    }

    public void StartDrive()
    {
        mPlayerInput.Enable();
        OnStartDrive();
    }

    // On Spawn
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
    
    #endregion

    #region Controls
    private void Move(InputAction.CallbackContext InputValue)
    {
        mMoveInput = InputValue.ReadValue<float>();
        
        Debug.Log("Recording Inputs");
    }
    
    private void Roll(InputAction.CallbackContext InputValue)
    {
        mRotationInput = InputValue.ReadValue<float>();
    }

    private void Nitro(InputAction.CallbackContext InputValue)
    {
        if (!ComponentsDic.ContainsKey(ECarPart.Nitro)) return;

        if (InputValue.ReadValueAsButton() && mExhaustedParts[ECarPart.Nitro] != true)
        {
            // This is to stop the fuel consumption when the nitro starts
            ComponentsDic[ECarPart.Fuel].StopComponent();
            ComponentsDic[ECarPart.Nitro].StartComponent();
        }
        else
        {
            // This is to start the fuel consumption when the nitro stops
            ComponentsDic[ECarPart.Nitro].StopComponent();
            ComponentsDic[ECarPart.Fuel].StartComponent();
        }
    }
    
    #endregion

    #region Actions
    // Action Methods
    private void Accelarate()
    {
        if (mExhaustedParts[ECarPart.Fuel]) return;

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
    
    private void OnPartExhausted(ECarPart exhaustedPart)
    {
        if (mExhaustedParts.ContainsKey(exhaustedPart))
            mExhaustedParts[exhaustedPart] = true;

        int partsCount = 0;
        foreach (var p in mExhaustedParts)
        {
            if (p.Value == true) partsCount++;
        }

        if (partsCount == mExhaustedParts.Count)
        {
            Debug.Log("All parts exhausted");
            OnResourcesExhausted();
        }
    }


    #endregion

    #region Component Handlers

    public void RegisterComponent(ECarPart Type, CarComponent Component)
    {
        if(!ComponentsDic.ContainsKey(Type))ComponentsDic.Add(Type, Component);
    }

    public void RegisterExhaustiveComponent(ECarPart Type, bool Value)
    {
        if(!mExhaustedParts.ContainsKey(Type)) mExhaustedParts.Add(Type, Value);
    }

    public void UpdateExhaustiveComponent(ECarPart Type, bool Value)
    {
        if (mExhaustedParts.ContainsKey(Type)) mExhaustedParts[Type] = Value;
    }
    
    #endregion

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

        if (mMoveInput >= 1) Accelarate();
        else Decelarate();
    }

    public void UpdateCarMetrics(ECarPart carPart, float value)
    {
        OnComponentUpdated?.Invoke(carPart, value);
    }
}
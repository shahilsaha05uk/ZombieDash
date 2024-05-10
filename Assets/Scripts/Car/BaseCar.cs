using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Cinemachine;
using EnumHelper;
using Interfaces;
using StructClass;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.PlayerLoop;
using UnityEngine.Serialization;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

[RequireComponent(typeof(CarManager))]
public abstract class BaseCar : MonoBehaviour, IResetInterface
{
    #region Reset Properties
    private Vector3 pos;
    private Vector3 scale;
    private Quaternion rot;
    #endregion


    #region Properties
    private Coroutine EngineCor;

    // Dictionary that stores all the Components connected to the car
    protected IDictionary<ECarPart, CarComponent> ComponentsDic = new Dictionary<ECarPart, CarComponent>();
    
    public delegate void FOnCarComponentUpdate(ECarPart carPart, float value);
    public FOnCarComponentUpdate OnComponentUpdated;

    // Privates
    public int ID;
    protected bool bEngineRunning;
    protected PlayerInputMappingContext mPlayerInput;

    private float mRotationInput;
    private float mMoveInput;

    protected Vector2 mCurrentVelocity;
    protected float mCurrentVelocityMag;
    
    
    [Space(20)][Header("References")]
    protected CarManager mCarManager;

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
    protected bool bStartedDriving = false;
    protected bool bIsVelocityPositive = false;

    [SerializeField] private LayerMask mGroundLayer;

    [SerializeField] private CinemachineVirtualCamera mVirtualCameraPrefab;
    private CinemachineVirtualCamera mVirtualCamera;


    #endregion

    #region Overridables

    protected virtual void OnStartDrive() { }

    protected virtual void OnDriving() { }

    protected virtual void OnStopDrive() { }
    
    #endregion

    #region Initialisers

    protected virtual void Awake()
    {
        mCarManager = GetComponent<CarManager>();
        
        var trans = transform;
        
        pos = trans.position;
        rot = trans.rotation;
        scale = trans.localScale;
        
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
        bEngineRunning = true;
        EngineCor = StartCoroutine(StartEngine());
    }
    public void StopDrive()
    {
        bStartedDriving = false;
        bEngineRunning = false;
        mPlayerInput.Disable();
        OnStopDrive();
    }

    protected virtual IEnumerator StartEngine()
    {
        WaitForSeconds timeInterval = new WaitForSeconds(0.002f);
        while (bEngineRunning)
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

            bIsVelocityPositive = (mCurrentVelocity.x >= 0.1f);

            if (mMoveInput != 0) Accelarate();
            else Decelarate();

            OnDriving();

            yield return timeInterval;
        }
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

        bStartedDriving = true;
    }
    
    private void Roll(InputAction.CallbackContext InputValue)
    {
        mRotationInput = InputValue.ReadValue<float>();
    }

    private void Nitro(InputAction.CallbackContext InputValue)
    {
        if (!ComponentsDic.ContainsKey(ECarPart.Nitro)) return;

        if (InputValue.ReadValueAsButton() && !ComponentsDic[ECarPart.Nitro].mHasExhausted)
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
        if (ComponentsDic[ECarPart.Fuel].mHasExhausted) return;

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
        var rot = -mRotationInput * mRotateSpeed * Time.deltaTime;
        carRb.AddTorque(rot);
    }
    
    #endregion

    #region Component Handlers

    public void RegisterComponent(ECarPart Type, CarComponent Component)
    {
        if(!ComponentsDic.ContainsKey(Type))ComponentsDic.Add(Type, Component);
    }
    #endregion

    public void UpdateCarMetrics(ECarPart carPart, float value)
    {
        OnComponentUpdated?.Invoke(carPart, value);
    }

    public virtual void OnReset()
    {
        frontTireRb.velocity = backTireRb.velocity = carRb.velocity = Vector2.zero;

        transform.SetPositionAndRotation(pos, rot);
        transform.localScale = scale;
        
        mPlayerInput.Disable();

    }
}
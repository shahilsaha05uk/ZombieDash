using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using EnumHelper;
using Interfaces;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

[RequireComponent(typeof(CarManager))]
[RequireComponent(typeof(CheckGroundClearance))]
[RequireComponent(typeof(CarController))]
public abstract class BaseCar : MonoBehaviour, IResetInterface
{
    #region Reset Properties
    private Vector3 pos;
    private Vector3 scale;
    private Quaternion rot;
    #endregion

    #region Delegates and Events
    public delegate void FOnNitroToggleSignature(bool Value);
    public event FOnNitroToggleSignature OnNitroToggled;
    #endregion

    #region Properties
    public CarController mController { private set; get; }

    protected IDictionary<ECarPart, CarComponent> mComponentsDic = new Dictionary<ECarPart, CarComponent>();
    private Coroutine EngineCor;
    
    // Privates
    [Space(20)]
    public int ID;
    protected bool bEngineRunning;
    protected float mRotationInput;
    protected float mMoveInput;

    [Space(5)][Header("Privates")]
    protected CarManager mCarManager;
    protected CheckGroundClearance mGroundClearanceComp;
    [SerializeField] protected Rigidbody2D frontTireRb;
    [SerializeField] protected Rigidbody2D backTireRb;
    [SerializeField] protected Rigidbody2D carRb;

    [Space(5)][Header("Accelaration Properties")]
    [SerializeField] private float mAccelarationRate = 20f;
    [SerializeField] private float mDecelerationRate = 300f;
    
    [Space(5)][Header("Rotation Properties")]
    [SerializeField] private float mRotateSpeed = 300f;
    [SerializeField] private float rotSpeedWhenInAirAndNitro = 6000f;

    [Space(5)][Header("Speed Properties")]
    [SerializeField] private float mMaxSpeed;
    [SerializeField] private float mExtraForce = 0.5f;

    protected bool bStartedDriving = false;
    protected bool bIsVelocityPositive = false;
    private bool bActivateNitro;

    #endregion
    
    #region Initialisers

    protected virtual void Awake()
    {
        mController = GetComponent<CarController>();
        mCarManager = GetComponent<CarManager>();
        mGroundClearanceComp = GetComponent<CheckGroundClearance>();
        transform.rotation = Quaternion.identity;

        var trans = transform;
        pos = trans.position;
        rot = trans.rotation;
        scale = trans.localScale;

        CarComponent.OnNonExhaustiveCarComponentUpgrade += OnNonExhaustivePartUpgrade;
    }

    private void OnNonExhaustivePartUpgrade(float value, ECarPart part)
    {
        switch (part)
        {
            case ECarPart.Speed:
                mMaxSpeed = value;
                break;
        }
    }

    #endregion

    #region Drive

    public void StartDrive()
    {
        OnStartDrive();
        bEngineRunning = true;
    }


    private void Update()
    {
        if (bEngineRunning)
        {
            // Rolls the player in the air
            Rotate();

            bIsVelocityPositive = (mCarManager.Velocity.x >= 0.1f);

            // Accelaration
            if (mMoveInput != 0) Accelarate();
            else Decelarate();

            OnDriving();
        }
    }

    public void StopDrive()
    {
        bStartedDriving = false;
        bEngineRunning = false;
        OnStopDrive();
    }

    #endregion

    #region Actions
    private void Accelarate()
    {
        if (mComponentsDic[ECarPart.Fuel].bHasExhausted) return;

        if (mGroundClearanceComp.bIsOnGround)
        {
            frontTireRb.AddTorque(-mMoveInput * mMaxSpeed * Time.deltaTime);
            backTireRb.AddTorque(-mMoveInput * mMaxSpeed * Time.deltaTime);
        }
        else
        {
            carRb.AddForce(transform.right * (mExtraForce * mMoveInput * Time.deltaTime), ForceMode2D.Force);
        }
    }
    private void Decelarate()
    {
        if(mCarManager.VelocityMag > 0f){
            carRb.AddForce(-carRb.velocity * mDecelerationRate); // Decelerate immediately
        } 
    }
    private void Rotate()
    {
        if (mRotationInput == 0f) return;

        float val = (!mGroundClearanceComp.bIsOnGround && bActivateNitro) ? rotSpeedWhenInAirAndNitro : mRotateSpeed;

        var rot = -mRotationInput * val * Time.deltaTime;
        carRb.AddTorque(rot);
        
        print("Rolling " + rot);

    }
    protected void ApplyNitro(bool Value)
    {
        if (!mComponentsDic.ContainsKey(ECarPart.Nitro)) return;

        bActivateNitro = Value;

        if (bActivateNitro && !mComponentsDic[ECarPart.Nitro].bHasExhausted)
        {
            mComponentsDic[ECarPart.Nitro].StartComponent();
        }
        else
        {
            mComponentsDic[ECarPart.Nitro].StopComponent();
        }
    }

    #endregion

    #region Controls Implementation
    public void Move(float Value)
    {
        mMoveInput = Value;
        bStartedDriving = true;
    }

    public void Roll(float Value)
    {
        mRotationInput = Value;
    }

    public void Nitro(bool Value)
    {
        ApplyNitro(Value);
    }

    #endregion

    #region Component Handlers

    public void RegisterComponent(ECarPart Type, CarComponent Component)
    {
        if(!mComponentsDic.ContainsKey(Type))mComponentsDic.Add(Type, Component);
    }
    #endregion
    
    #region Overridables

    protected virtual void OnStartDrive() { }

    protected virtual void OnDriving() { }

    protected virtual void OnStopDrive() { }

    #endregion
    public virtual void OnReset()
    {
        frontTireRb.velocity = backTireRb.velocity = carRb.velocity = Vector2.zero;

        transform.SetPositionAndRotation(pos, rot);
        transform.localScale = scale;
    }
}
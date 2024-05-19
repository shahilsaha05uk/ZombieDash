using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using EnumHelper;
using Interfaces;
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
    public delegate void FOnCarComponentUpdate(ECarPart carPart, float value);
    public FOnCarComponentUpdate OnComponentUpdated;

    public delegate void FOnNitroToggleSignature(bool Value);
    public event FOnNitroToggleSignature OnNitroToggled;
    #endregion

    #region Properties
    public CarController mController { private set; get; }

    protected IDictionary<ECarPart, CarComponent> ComponentsDic = new Dictionary<ECarPart, CarComponent>();
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
    private bool mActivateNitro;

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
    }

    #endregion

    #region Drive

    public void StartDrive()
    {
        OnStartDrive();
        bEngineRunning = true;
        
        EngineCor = StartCoroutine(RunEngine());
    }

    protected virtual IEnumerator RunEngine()
    {
        WaitForSeconds timeInterval = new WaitForSeconds(0.002f);
        while (bEngineRunning)
        {
            // Ground Check
            Rotate();

            bIsVelocityPositive = (mCarManager.Velocity.x >= 0.1f);

            if (mMoveInput != 0) Accelarate();
            else Decelarate();

            OnDriving();

            yield return timeInterval;
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
        if (ComponentsDic[ECarPart.Fuel].mHasExhausted) return;

        if (mGroundClearanceComp.bIsOnGround)
        {
            frontTireRb.AddTorque(-mMoveInput * mMaxSpeed);
            backTireRb.AddTorque(-mMoveInput * mMaxSpeed);
        }
        else
        {
            carRb.AddForce(transform.right * (mExtraForce * mMoveInput), ForceMode2D.Force);
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

        float val = (!mGroundClearanceComp.bIsOnGround && mActivateNitro) ? rotSpeedWhenInAirAndNitro : mRotateSpeed;

        var rot = -mRotationInput * val * Time.deltaTime;
        carRb.AddTorque(rot);
    }
    protected void ApplyNitro(bool Value)
    {
        if (!ComponentsDic.ContainsKey(ECarPart.Nitro)) return;

        mActivateNitro = Value;

        if (mActivateNitro && !ComponentsDic[ECarPart.Nitro].mHasExhausted)
        {
            ComponentsDic[ECarPart.Nitro].StartComponent();
        }
        else
        {
            ComponentsDic[ECarPart.Nitro].StopComponent();
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
        if(!ComponentsDic.ContainsKey(Type))ComponentsDic.Add(Type, Component);
    }
    public void UpdateCarMetrics(ECarPart carPart, float value)
    {
        OnComponentUpdated?.Invoke(carPart, value);
    }

    public void UpdateSpeed(int Value)
    {
        mMaxSpeed = Value;
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
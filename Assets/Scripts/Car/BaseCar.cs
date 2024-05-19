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
using static UnityEngine.RuleTile.TilingRuleOutput;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

[RequireComponent(typeof(CarManager))]
[RequireComponent(typeof(CheckGroundClearance))]
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
    [Header("Car Properties")]
    [SerializeField] private WheelJoint2D frontJoint;
    [SerializeField] private WheelJoint2D rearJoint;
    [SerializeField][Range(0,1)] private float SuspensionDampRatio;
    [SerializeField] private float SuspensionFrequency;
    [SerializeField] private float SuspensionAngle;

    private Coroutine EngineCor;

    // Dictionary that stores all the Components connected to the car
    protected IDictionary<ECarPart, CarComponent> ComponentsDic = new Dictionary<ECarPart, CarComponent>();
    
    // Privates
    [Space(20)]
    public int ID;
    protected bool bEngineRunning;
    protected PlayerInputMappingContext mPlayerInput;

    private float mRotationInput;
    private float mMoveInput;

    protected Vector2 mCurrentVelocity;
    protected float mCurrentVelocityMag;
    
    
    [Space(20)][Header("References")]
    protected CarManager mCarManager;
    protected CheckGroundClearance mGroundClearanceComp;

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

    protected bool bStartedDriving = false;
    protected bool bIsVelocityPositive = false;

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
        mGroundClearanceComp = GetComponent<CheckGroundClearance>();
        transform.rotation = Quaternion.identity;

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

    public void CarInit()
    {
        JointSuspension2D susp = new JointSuspension2D();
        susp.dampingRatio = SuspensionDampRatio;
        susp.frequency = SuspensionFrequency;
        susp.angle = SuspensionAngle;

        frontJoint.suspension = susp;
        rearJoint.suspension = susp;
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
        
        mPlayerInput.Disable();
    }

    #endregion

    #region Drive

    public void StartDrive()
    {

        mPlayerInput.Enable();
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

            mCurrentVelocity = new Vector2(carRb.velocity.x, 0f);
            mCurrentVelocityMag = mCurrentVelocity.magnitude;

            bIsVelocityPositive = (mCurrentVelocity.x >= 0.1f);

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
        mPlayerInput.Disable();
        OnStopDrive();
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

        bool activateNitro = InputValue.ReadValueAsButton();

        if (activateNitro && !ComponentsDic[ECarPart.Nitro].mHasExhausted)
        {
            ComponentsDic[ECarPart.Nitro].StartComponent();
        }
        else
        {
            // This is to start the fuel consumption when the nitro stops
            ComponentsDic[ECarPart.Nitro].StopComponent();
        }
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
        if(mCurrentVelocityMag > 0f){
            carRb.AddForce(-carRb.velocity * mDecelerationRate); // Decelerate immediately
        } 
    }

    public float targetRot = 45;
    private void Rotate()
    {
        if (mRotationInput == 0f) return;
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
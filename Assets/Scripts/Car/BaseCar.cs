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

public abstract class BaseCar : MonoBehaviour, IDayBeginInterface
{
    #region Properties
    //[Header("Debugging Values")] 

    // Dictionary that stores all the Components connected to the car
    private IDictionary<ECarPart, CarComponent> ComponentsDic = new Dictionary<ECarPart, CarComponent>();
    
    public delegate void FOnCarComponentUpdate(ECarPart carPart, float value);
    public FOnCarComponentUpdate OnComponentUpdated;

    // Privates
    public int ID;
    
    protected PlayerInputMappingContext mPlayerInput;
    private PlayerHUD mPlayerHUD;

    private Transform startPos;
    [SerializeField]private Transform endPos;
    private float mTotalDistance;
    private Coroutine mPlayerHudCoroutine;
    private float mRotationInput;
    private float mMoveInput;
    private IDictionary<ECarPart, bool> mExhaustedParts;

    private Vector2 mCurrentVelocity;
    private float mCurrentVelocityMag;
    
    
    [Space(20)][Header("References")]
    [SerializeField] private Rigidbody2D frontTireRb;
    [SerializeField] private Rigidbody2D backTireRb;
    [SerializeField] private Rigidbody2D carRb;
    
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

    #region Initialisers

    private void Awake()
    {
        startPos = GameManager.GetPlayerStart().transform;
        endPos = GameObject.FindWithTag("Finish").transform;
        
        mMoveInput = 0f;
        SetupInputComponent();
        
        mExhaustedParts = new Dictionary<ECarPart, bool>() { { ECarPart.Fuel, false}, { ECarPart.Nitro, false} };
        CarComponent.OnRunningOutOfResources += OnPartExhausted;

        // From Awake
        GameManager.Instance.OnDayBegin += OnDayBegin;
        GameManager.Instance.OnDayPreComplete += OnDayPreComplete;
        GameManager.Instance.OnDayComplete += OnDayComplete;
        
        mTotalDistance = Mathf.Abs(endPos.position.x - startPos.position.x);

        GameManager.Instance.DayBegin();
    }


    public void StartDrive()
    {
        mPlayerInput.Enable();

        mPlayerHUD.ActivatePanel(EPanelType.Hud);
        
        StartCoroutine(UpdateDistance());
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
    // Control Bindings
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
            mPlayerInput.Move.Disable();
            mPlayerHUD.ActivatePanel(EPanelType.Review);
        }
    }


    #endregion

    #region UI Updates
    // Calling the HUD
    public void UpdateCarMetrics(ECarPart carPart, float value)
    {
        OnComponentUpdated?.Invoke(carPart, value);
    }
    
    // Updating the HUD Distance Meter
    private IEnumerator UpdateDistance()
    {
        while (true)
        {
            float currentDistance = Mathf.Abs(endPos.transform.position.x - transform.position.x);
            float progress = 1 - (currentDistance / mTotalDistance);
            mPlayerHUD.UpdateDistance(progress);
         
            yield return null;
        }
    }
    
    // Registering the components
    public void RegisterComponent(ECarPart Type, CarComponent Component)
    {
        if(!ComponentsDic.ContainsKey(Type))ComponentsDic.Add(Type, Component);
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

    #region Day Management
    
    //TODO: DO NOT CALL THE "DAY" methods from the Game Manager in any methods in the region as it would be stuck in an infinite loop otherwise
    
    // When the Day Completes
    public void OnDayBegin()
    {
        Debug.Log("Day Begins");
        LevelManager.Instance.MoveGameObjectToCurrentScene(gameObject, ELevel.GAME);

        // Set up the Player HUD
        if (mPlayerHUD == null)
        {
            var uiManager = UIManager.Instance;
            if (uiManager != null)
            {
                mPlayerHUD = uiManager.SpawnWidget(EUI.PLAYERHUD).GetWidgetAs<PlayerHUD>();
                var car = this;
                mPlayerHUD.Init(ref car);
            }
        }
        transform.SetPositionAndRotation(startPos.position, startPos.rotation);
        mPlayerHUD.ActivatePanel(EPanelType.Upgrade);
        
        // Set up the camera and attach it to the car to be able to follow it
        if (mVirtualCamera == null)
        {
            mVirtualCamera = Instantiate(mVirtualCameraPrefab);
            mVirtualCamera.Follow = transform;
        }
        
        // Resetting the Components
        foreach (var c in ComponentsDic)
        {
            c.Value.ResetComponent();
        }
    }
    private void OnDayPreComplete()
    {
        LevelManager.Instance.MoveGameObjectToCurrentScene(gameObject, ELevel.GAME_PERSISTANCE);
    }
    private void OnDayComplete()
    {
        
    }
    #endregion

}
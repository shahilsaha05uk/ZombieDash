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
    //[Header("Debugging Values")] 

    // Dictionary that stores all the Components connected to the car
    private IDictionary<ECarPart, CarComponent> ComponentsDic = new Dictionary<ECarPart, CarComponent>();
    
    public delegate void FOnCarComponentUpdate(ECarPart carPart, float value);
    public FOnCarComponentUpdate OnComponentUpdated;

    // Privates
    private Controller PC;
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

    private void Awake()
    {
        mExhaustedParts = new Dictionary<ECarPart, bool>() { { ECarPart.Fuel, false}, { ECarPart.Nitro, false} };
        CarComponent.OnRunningOutOfResources += OnPartExhausted;

        startPos = transform;
        endPos = GameObject.FindWithTag("Finish").transform;
        
        mTotalDistance = Mathf.Abs(endPos.position.x - startPos.position.x);
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

            mPlayerHUD.ActivateReviewPanel();
        }
    }

    // On Spawn
    public void Possess(Controller controller)
    {
        mMoveInput = 0f;

        PC = controller;
        SetupInputComponent();
        
        // Set up the Player HUD
        var uiManager = UIManager.Instance;
        if (uiManager != null)
        {
            mPlayerHUD = uiManager.SpawnWidget(EUI.PLAYERHUD).GetWidgetAs<PlayerHUD>();
            var car = this;
            mPlayerHUD.Init(ref car);
        }
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
        
        if(mMoveInput >= 1) Accelarate();
        else Decelarate();
        
        UpdateDistance();
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
    
    // Calling the HUD
    public void UpdateCarMetrics(ECarPart carPart, float value)
    {
        OnComponentUpdated?.Invoke(carPart, value);
    }
    
    // Updating the HUD Distance Meter
    private void UpdateDistance()
    {
        float currentDistance = Mathf.Abs(endPos.transform.position.x - transform.position.x);
        float progress = 1 - (currentDistance / mTotalDistance);
       // mPlayerHUD.UpdateDistance(progress);
    }
    
    // Registering the components
    public void RegisterComponent(ECarPart Type, CarComponent Component)
    {
        if(!ComponentsDic.ContainsKey(Type))ComponentsDic.Add(Type, Component);
    }
}
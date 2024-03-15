using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour
{
    [SerializeField] private Rigidbody2D frontTireRb;
    [SerializeField] private Rigidbody2D backTireRb;
    [SerializeField] private Rigidbody2D carRb;
    [SerializeField] private float speed = 150f;
    [SerializeField] private float rotationSpeed = 300f;

    private float moveInput;
    
    //TODO: Add controls for rotation as well
    
    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");
    }

    private void FixedUpdate()
    {
        frontTireRb.AddTorque(-moveInput * speed * Time.fixedDeltaTime);
        backTireRb.AddTorque(-moveInput * speed * Time.fixedDeltaTime);
        carRb.AddTorque(moveInput * rotationSpeed * Time.fixedDeltaTime);
    }
}

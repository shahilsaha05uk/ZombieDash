using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CarController : MonoBehaviour
{
    [SerializeField] private Rigidbody2D frontTireRb;
    [SerializeField] private Rigidbody2D backTireRb;
    [SerializeField] private WheelJoint2D frontTireJoint;
    [SerializeField] private WheelJoint2D backTireJoint;
    [SerializeField] private Rigidbody2D carRb;
    [SerializeField] private float speed = 150f;
    [SerializeField] private float accelaration = 1000f;
    [SerializeField] private float rotationSpeed = 300f;
    [SerializeField] private TextMeshProUGUI txtKPH;
    private float moveInput;
    
    //TODO: Add controls for rotation as well

    private void Start()
    {
        frontTireJoint.useMotor = true;
        backTireJoint.useMotor = true;
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");
        
        int speed = (int)(carRb.velocity.magnitude * 3.6f);
        txtKPH.SetText(speed.ToString());
    }

    private void FixedUpdate()
    {

        //float speed = moveInput * speed * Time.fixedDeltaTime;

        /*
        frontTireRb.AddTorque(-moveInput * speed * Time.fixedDeltaTime);
        backTireRb.AddTorque(-moveInput * speed * Time.fixedDeltaTime); 
        carRb.AddTorque(moveInput * rotationSpeed * Time.fixedDeltaTime);
        */
        //Vector2 dir = new Vector2(moveInput * rotationSpeed * Time.fixedDeltaTime, transform.position.y);
        //carRb.AddForce(dir, ForceMode2D.Force);
        
        Accelarate();
    }


    private void Accelarate()
    {
        frontTireJoint.motor.
    }

    private void Deccelarate()
    {
        
    }
}

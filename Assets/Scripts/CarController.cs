using UnityEngine;

public class CarController : MonoBehaviour
{
    /*
    [SerializeField] private Rigidbody2D frontTireRb;
    [SerializeField] private Rigidbody2D backTireRb;
    [SerializeField] private Rigidbody2D carRb;
    
    [SerializeField] private float speed = 150f;
    [SerializeField] private float airRotationSpeed = 300f;
    
    [SerializeField] private PlayerHUD mPlayerHUD;
    
    private float moveInput;
    private float rotationInput;

    private bool bShouldRotate = false;
    */
    //TODO: Add controls for rotation as well

    void Update()
    {
        /*
        moveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetKeyDown(KeyCode.M))
        {
            rotationInput = -1;
            bShouldRotate = true;
        }
        else if (Input.GetKeyDown(KeyCode.N))
        {
            rotationInput = 1;
            bShouldRotate = true;
        }
        else if (Input.GetKeyUp(KeyCode.M) || Input.GetKeyUp(KeyCode.N))
        {
            bShouldRotate = false;
            rotationInput = 0; // Reset rotation input when the keys are released
        }

        Vector2 carVel = carRb.velocity;
        float carSpeed = carRb.velocity.magnitude;
        
        int speed = (int)(carVel.magnitude * 3.6f);
        mPlayerHUD.txtKPH.SetText(speed.ToString());

        if (carVel.magnitude > 20f)
        {
            mPlayerHUD.DecreaseFuel();
        }
    */
    }


    /*private void FixedUpdate()
    {
        Accelarate();

        if (bShouldRotate)
        {
            RotateCar();
        }
    }
    
    private void Accelarate()
    {
        frontTireRb.AddTorque(-moveInput * speed * Time.fixedDeltaTime);
        backTireRb.AddTorque(-moveInput * speed * Time.fixedDeltaTime); 
    }

    private void RotateCar()
    {
        carRb.AddTorque(rotationInput * airRotationSpeed * Time.fixedDeltaTime);
    }*/
}

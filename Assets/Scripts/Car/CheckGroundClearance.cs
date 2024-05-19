using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CheckGroundClearance : MonoBehaviour
{
    private Rigidbody2D rb;
    public bool bIsOnGround {  get; private set; }
    
    [SerializeField] private float mGroundClearance;
    [SerializeField] private LayerMask mGroundLayer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if (rb == null) return;
        
        Vector2 position = transform.position;
        Vector2 direction = Vector2.down;
        Debug.DrawRay(position, direction * mGroundClearance, Color.cyan, Time.fixedDeltaTime);

        RaycastHit2D hit = Physics2D.Raycast(position, direction, mGroundClearance, mGroundLayer);
        bIsOnGround = (hit.collider != null);
    }
}

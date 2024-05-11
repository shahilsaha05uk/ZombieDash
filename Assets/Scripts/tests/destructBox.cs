using Interfaces;
using System.Collections;
using UnityEngine;

public class destructBox : MonoBehaviour, IResetInterface
{
    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer meshRenderer;
    public Vector3 pos;
    public Vector3 scale;
    public Quaternion rot;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        meshRenderer = GetComponent<SpriteRenderer>();
        var trans = transform;

        pos = trans.position;
        rot = trans.rotation;
        scale = trans.localScale;

        GameManager.OnResetLevel += OnReset;
    }
    public void OnReset()
    {
        meshRenderer.enabled = true;
        if (col != null) col.enabled = true;
        
        rb.isKinematic = true;
        transform.SetPositionAndRotation(pos, rot);
        transform.localScale = scale;
        rb.isKinematic = false;
    }
    private void Update()
    {
        if(Input.GetKeyUp(KeyCode.R)) OnReset();
    }
}

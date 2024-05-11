using UnityEngine;
using WSMGameStudio.Behaviours;
using WSMGameStudio.Settings;

public class destructBox : MonoBehaviour
{
    private Rigidbody2D rb;
    private Collider2D col;
    private MeshRenderer meshRenderer;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        meshRenderer = GetComponent<MeshRenderer>();
    }


    void Update()
    {
        if (Input.GetKeyUp(KeyCode.R))
        {
            GetComponent<ResettableTransform>().ResetObject();

            ResetBox();
        }
        if(Input.GetMouseButtonUp(0))
        {
            print("Taken Damage");

        }
    }

    public void ResetBox()
    {
        rb.isKinematic = false;
        if (col != null)
            col.enabled = true;

        meshRenderer.enabled = true;

    }

    public void OnBreak()
    {
        print("Taken Damage");
    }
}

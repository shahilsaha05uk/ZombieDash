using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;


public class LineTest : MonoBehaviour
{
    public LineRenderer linePrefab;
    
    private LineRenderer lineRenderer;

    [SerializeField] private float lineTimeInterval;
    [SerializeField] private float lineWidth;
    
    private Vector2 lastMousePos;
    private Coroutine lineDrawCoroutine;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            StartLineDraw();
        }
        if (Input.GetMouseButtonUp(0))
        {
            StopLineDraw();
        }
    }

    private void StartLineDraw()
    {
        lineRenderer = Instantiate(linePrefab);
        lineRenderer.widthMultiplier = lineWidth;

        lineDrawCoroutine = StartCoroutine(LineDraw());
    }

    private IEnumerator LineDraw()
    {
        EdgeCollider2D edgeCol = lineRenderer.GetComponent<EdgeCollider2D>();
        edgeCol.edgeRadius = lineWidth;
        
        List<Vector2> colliderPoints = new List<Vector2>();

        while (true)
        {
            Vector2 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            
            if (pos != lastMousePos)
            {
                lastMousePos = pos;
                lineRenderer.positionCount++;
                var count = lineRenderer.positionCount - 1;
                lineRenderer.SetPosition(count, pos);

                Vector3 localPos = lineRenderer.transform.InverseTransformPoint(pos);
                colliderPoints.Add(localPos);
                edgeCol.points = colliderPoints.ToArray();
            }
            
            yield return new WaitForSeconds(lineTimeInterval);
        }

    }
    private void StopLineDraw()
    {
        if(lineDrawCoroutine != null) StopCoroutine(lineDrawCoroutine);
    }
    
}

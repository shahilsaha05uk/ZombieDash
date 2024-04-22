using System;
using System.Collections;
using System.Collections.Generic;
using EnumHelper;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

[RequireComponent(typeof(CanvasGroup))]
public abstract class BaseWidget : MonoBehaviour
{
    public delegate void FOnDestroyWidgetSignature(EUI ui);

    public FOnDestroyWidgetSignature OnWidgetDestroy;

    [SerializeField] protected EUI mUiType;

    protected virtual void OnEnable()
    {
        
    }

    public void AddToViewport()
    {
        GetComponent<CanvasGroup>().alpha = 1f;
    }
    public void RemoveFromViewport()
    {
        GetComponent<CanvasGroup>().alpha = 0f;
    }
    public void DestroyWidget()
    {
        OnWidgetDestroy.Invoke(mUiType);
    }
    public T GetWidgetAs<T>() where T : BaseWidget
    {
        return this as T;
    }

}

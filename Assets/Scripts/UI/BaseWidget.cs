using System;
using System.Collections;
using System.Collections.Generic;
using EnumHelper;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public enum EAnimDirection
{
    Forward, Backward
}

[RequireComponent(typeof(CanvasGroup))]
public abstract class BaseWidget : MonoBehaviour
{
    public delegate void FOnDestroyWidgetSignature(EUI ui);

    public FOnDestroyWidgetSignature OnWidgetDestroy;

    [SerializeField] protected EUI mUiType;
    
    public void DestroyWidget()
    {
        OnWidgetDestroy?.Invoke(mUiType);
    }
    public T GetWidgetAs<T>() where T : BaseWidget
    {
        return this as T;
    }
}

using System.Collections;
using System.Collections.Generic;
using EnumHelper;
using UnityEngine;

public abstract class BaseWidget : MonoBehaviour
{
    public delegate void FOnDestroyWidgetSignature(EUI ui);

    public FOnDestroyWidgetSignature OnWidgetDestroy;

    [SerializeField] protected EUI mUiType;
    
    public void AddToViewport()
    {
        gameObject.SetActive(true);
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

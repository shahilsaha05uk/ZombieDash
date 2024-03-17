using System;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using EnumHelper;
using UnityEngine;

public class UIManager : ParentManager
{
    [SerializedDictionary("UI Enum", "UI Object Ref")]
    [SerializeField]private SerializedDictionary<EUI, BaseWidget> mWidgetClass;

    private IDictionary<EUI, BaseWidget> mWidgetInstanceRef;

    private void Awake()
    {
        mWidgetInstanceRef = new Dictionary<EUI, BaseWidget>();
    }

    public BaseWidget InitialiseWidget(EUI WidgetToInitialise, bool bAddToViewport = false)
    {
        if (!mWidgetInstanceRef.TryGetValue(WidgetToInitialise, out var value))
        {
            BaseWidget widget = Instantiate(mWidgetClass[WidgetToInitialise], transform);
            widget.gameObject.SetActive(bAddToViewport);
            widget.OnWidgetDestroy += OnWidgetDestroy;
            mWidgetInstanceRef.Add(WidgetToInitialise, widget);
            return widget;
        }

        return null;
    }

    private void OnWidgetDestroy(EUI ui)
    {
        bool widget = mWidgetInstanceRef.TryGetValue(ui, out var value);
        if (value != null)
        {
            value.OnWidgetDestroy += OnWidgetDestroy;
            Destroy(value.gameObject);
            mWidgetInstanceRef.Remove(ui);
        }
    }
}

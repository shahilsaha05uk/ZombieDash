using System;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using EnumHelper;
using UnityEngine;

public class UIManager : ParentManager
{
    [SerializeField]private SO_LevelUIList mWidgetList;

    public static UIManager Instance;
    private IDictionary<EUI, BaseWidget> mWidgetInstanceRef;

    [SerializeField] private Transform mCanvas;

    private void Awake()
    {
        if(Instance == null) Instance = this;

        mWidgetInstanceRef = new Dictionary<EUI, BaseWidget>();
    }

    public BaseWidget InitialiseWidget(EUI WidgetToInitialise, bool bAddToViewport = true)
    {
        if (!mWidgetInstanceRef.TryGetValue(WidgetToInitialise, out var value))
        {
            BaseWidget widget = Instantiate(mWidgetList.WidgetClass[WidgetToInitialise], mCanvas);
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

using System;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using EnumHelper;
using UnityEngine;


public class UIManager : ParentManager
{
    public static UIManager Instance { get; private set; }

    [SerializeField]private SO_LevelUIList mWidgetList;

    private IDictionary<EUI, BaseWidget> mWidgetInstanceRef;

    protected override void InitManager()
    {
        base.InitManager();

        if (Instance == null) Instance = this;
    }


    private void Awake()
    {
        mWidgetInstanceRef = new Dictionary<EUI, BaseWidget>();
    }

    public BaseWidget GetWidget(EUI WidgetType)
    {
        return mWidgetInstanceRef.TryGetValue(WidgetType, out var value) ? value : null;
    }

    public BaseWidget SpawnWidget(EUI WidgetToInitialise, bool bAddToViewport = true)
    {
        mWidgetInstanceRef.TryGetValue(WidgetToInitialise, out var value);
        if (value == null)
        {
            BaseWidget widget = Instantiate(mWidgetList.WidgetClass[WidgetToInitialise], Vector3.zero, Quaternion.identity);
            InitWidget(ref widget, WidgetToInitialise, bAddToViewport);
            return widget;
        }
        else return value;
    }

    public BaseWidget SpawnWidget(EUI WidgetToInitialise, Canvas ParentCanvas, bool bAddToViewport = true)
    {
        if (mWidgetInstanceRef.TryGetValue(WidgetToInitialise, out var value))
        {
            return value;
        }
        else
        {
            BaseWidget widget = Instantiate(mWidgetList.WidgetClass[WidgetToInitialise], ParentCanvas.transform);
            InitWidget(ref widget, WidgetToInitialise, bAddToViewport);
            return widget;
        }
    }
    private void InitWidget(ref BaseWidget widget, EUI WidgetToInitialise, bool bAddToViewport)
    {
        widget.gameObject.SetActive(bAddToViewport);
        widget.OnWidgetDestroy += OnWidgetDestroy;
        mWidgetInstanceRef.Add(WidgetToInitialise, widget);
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

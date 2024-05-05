using EnumHelper;
using Helpers;
using speedometer;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerHUD : BaseWidget
{
    private BaseCar mCarRef;
    [SerializeField] private ReviewPanel mReviewPanel;
    
    [SerializeField] private Speedometer mNitro;
    [SerializeField] private Speedometer mFuel;
    [SerializeField] private DistanceMeter mDistanceMeter;
    private float totalDistance;

    private void Awake()
    {
        mUiType = EUI.PLAYERHUD;
        
        mReviewPanel.gameObject.SetActive(false);
    }
    public void Init(ref BaseCar Car)
    {
        if (Car != null)
        {
            mCarRef = Car;
            mCarRef.OnComponentUpdated += OnWidgetUpdateRequest;
        }
    }

    public void UpdateDistance(float NormalizedDistance)
    {
        mDistanceMeter.UpdateValue(NormalizedDistance);
    }
    private void OnWidgetUpdateRequest(ECarPart carpart, float value)
    {
        Debug.Log("Widget Update requested!!");
        switch (carpart)
        {
            case ECarPart.All_Comp:
                mNitro.UpdateValue(value);
                mFuel.UpdateValue(value);
                break;
            case ECarPart.Fuel:
                mFuel.UpdateValue(value);
                break;
            case ECarPart.Nitro:
                mNitro.UpdateValue(value);
                break;
            case ECarPart.Speed:
                break;
        }
    }

    public void ActivateReviewPanel()
    {
        Debug.Log("Activate!!");
        mReviewPanel.gameObject.SetActive(true);
    }
}

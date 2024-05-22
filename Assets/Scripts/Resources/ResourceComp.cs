using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ResourceComp
{
    public delegate void FOnResourceUpdatedSignature(int CurrentBalance);
    public static event FOnResourceUpdatedSignature OnResourceUpdated;

    private static int Current;

    public static void AddResources(int increaseBy)
    {
        Current += increaseBy;
        OnResourceUpdated?.Invoke(Current);
    }

    public static void SubtractResources(int decreaseBy)
    {
        Current -= decreaseBy;
        OnResourceUpdated?.Invoke(Current);
    }

    public static int GetCurrentResources()
    {
        return Current;
    }
}

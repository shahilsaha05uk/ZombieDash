using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ResourceComp
{
    private static int Current;

    public static void AddResources(int increaseBy)
    {
        Current += increaseBy;
    }

    public static void SubtractResources(int decreaseBy)
    {
        Current -= decreaseBy;
    }

    public static int GetCurrentResources()
    {
        return Current;
    }
}

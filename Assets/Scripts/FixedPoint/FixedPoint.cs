using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct FixedFloat
{
    public static readonly int numberDecimal = 3;

    int data;

    public FixedFloat(int val)
    {
        data = val;
        for (int i = 0; i < numberDecimal; i++)
        {
            val *= 10;
        }
    }

    public FixedFloat(double val)
    {
        for (int i = 0; i < numberDecimal; i++)
        {
            val *= 10;
        }
        data = (int) System.Math.Floor(val);
    }
}

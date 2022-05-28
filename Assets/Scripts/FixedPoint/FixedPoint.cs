using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
public struct FixedFloat
{
    public static readonly int scaleFactor = 1024;

    public static readonly int numDecPts = (int) Math.Log10(scaleFactor) + 1;

    int data;

    public float floatVal => ((float)data)/scaleFactor;

    public FixedFloat(int val)
    {
        data = val;
        val *= scaleFactor;
    }

    public FixedFloat(double val)
    {
        val *= scaleFactor;
        data = (int) Math.Floor(val);
    }

    public static FixedFloat add(FixedFloat a, FixedFloat b)
    {
        return new FixedFloat
        {
            data = a.data + b.data
        };
    }

    public static FixedFloat subtract(FixedFloat a, FixedFloat b)
    {
        return new FixedFloat
        {
            data = a.data - b.data
        };
    }

    public static FixedFloat multiply(FixedFloat a, FixedFloat b)
    {
        int val = a.data * b.data;
        return new FixedFloat
        {
            data = val/(scaleFactor)
        };
    }

    public static FixedFloat divide(FixedFloat a, FixedFloat b)
    {
        int val = (a.data*scaleFactor) / (b.data);
        return new FixedFloat
        {
            data = val
        };
    }

    public static FixedFloat sqrt(FixedFloat a)
    {
        return new FixedFloat
        {
            data = isqrt(a.data)*isqrt(scaleFactor)
        };
    }

    //private static readonly HashSet<int> powersOfTwo = new HashSet<int>
    //{
    //    1<<1, 1<<2, 1<<3, 1<<4, 1<<5, 1<<6, 1<<7, 1<<8, 1<<9, 1<<10, 1<<11, 1<<12, 1<<13, 1<<14, 1<<15, 1<<16, 1<<17, 1<<18, 1<<19, 1<<20, 1<<21, 1<<22, 1<<23, 1<<24, 1<<25, 1<<26, 1<<27, 1<<28, 1<<29, 1<<30, 1<<31, 1<<32,
    //};

    private static int isqrt(int a)
    {
        if(a < 0)
        {
            throw new ArithmeticException("Square root of negative number: " + a);
        }

        //if(powersOfTwo.Contains(a))
        //{
        //    return a >> 1;
        //}
        int x0 = a / 2;

        if (x0 != 0)
        {
            int x1 = (x0 + a / x0) / 2;

            while (x1 < x0)
            {
                x0 = x1;
                x1 = (x0 + a / x0) / 2;
            }

            return x0;
        }

        return a;
    }

    public FixedFloat sqrt() => sqrt(this);

    public static FixedFloat operator +(FixedFloat a) => a;
    public static FixedFloat operator -(FixedFloat a) => new FixedFloat { data = -a.data };

    public static FixedFloat operator +(FixedFloat a, FixedFloat b) => add(a,b);
    public static FixedFloat operator -(FixedFloat a, FixedFloat b) => subtract(a, b);
    public static FixedFloat operator *(FixedFloat a, FixedFloat b) => multiply(a, b);
    public static FixedFloat operator /(FixedFloat a, FixedFloat b) => divide(a, b);

    public static bool operator ==(FixedFloat a, FixedFloat b) => a.data == b.data;
    public static bool operator !=(FixedFloat a, FixedFloat b) => a.data != b.data;

    public static bool operator <(FixedFloat a, FixedFloat b) => a.data < b.data;
    public static bool operator >(FixedFloat a, FixedFloat b) => a.data > b.data;

    public static bool operator <=(FixedFloat a, FixedFloat b) => a.data <= b.data;
    public static bool operator >=(FixedFloat a, FixedFloat b) => a.data >= b.data;

    public override bool Equals(object a)
    {
        //Check for null and compare run-time types.
        if ((a == null) || !this.GetType().Equals(a.GetType()))
        {
            return false;
        }
        else
        {
            FixedFloat f = (FixedFloat)a;
            return f.data == this.data;
        }
    }

    public override int GetHashCode()
    {
        return data.GetHashCode();
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MathUtils
{
    public static float TicksToSeconds(long ticks)
    {
        return ticks / 10000000f;
    }

    public static long SecondsToTicks(float seconds)
    {
        return (long)(seconds * 10000000);
    }

    public static float MinMaxAvgConc(float x, float min, float max, float avg, float conc)
    {
        max = Mathf.Max(max, min);
        avg = Mathf.Clamp(avg, min, max);

        var linear = x * (max - min) + min;
        var warpedValue = linear + ((avg - linear) * Mathf.Pow(conc, 2));

        //if (intValues) warpedValue = Mathf.Round(warpedValue);

        return warpedValue;
    }

    public static int MinMaxAvgConcToInt(float x, float min, float max, float avg, float conc)
    {
        return Mathf.RoundToInt(MinMaxAvgConc(x, min, max, avg, conc));
    }
    public static int RandomNegOrPos()
    {
        return (UnityEngine.Random.Range(0, 2) == 1) ? 1 : -1;
    }

    public static bool RandomBool()
    {
        return (UnityEngine.Random.Range(0, 2) == 1) ? true : false;
    }

    public static string FormatFloat(float number, int maxDecimalPlaces)
    {
        // Format with the maximum number of decimal places
        string format = "F" + maxDecimalPlaces;
        string result = number.ToString(format);

        // Trim trailing zeros up to the maximum decimal places
        for (int i = maxDecimalPlaces; i > 0; i--)
        {
            if (result.Contains(".") && result.EndsWith("0"))
            {
                result = result.Substring(0, result.Length - 1);
            }
            else
            {
                break;
            }
        }

        // If the number ends with a decimal point, remove it
        if (result.EndsWith("."))
        {
            result = result.Substring(0, result.Length - 1);
        }

        return result;
    }
}

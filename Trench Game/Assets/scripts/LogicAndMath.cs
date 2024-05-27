using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class LogicAndMath
{
    public static float MinMaxAvgConc(float x, float min, float max, float avg, float conc)
    {
        max = Mathf.Max(max, min);
        avg = Mathf.Clamp(avg, min, max);

        var linear = x * (max - min) + min;
        var warpedValue = linear + ((avg - linear) * Mathf.Pow(conc, 2));

        //if (intValues) warpedValue = Mathf.Round(warpedValue);

        return warpedValue;
    }

    public static float GetListValueTotal<T> (List<T> list, Func<T,float> predicate)
    {
        float total = 0f;

        foreach (var item in list)
        {
            total += predicate(item);
        }

        return total;
    }

    public static float ItemRatioFromListItemValues<T>(T item, List<T> list, Func<T, float> predicate)
    {
        var itemValue = predicate(item);

        var total = GetListValueTotal(list, predicate);

        return itemValue / total;
    }

    //public static float IndexRatioFromListItemValue<T> (int index, List<T> list, Func<T, float> predicate)
    //{
    //    if (list.Count <= index) return 0;

    //    var item = list[index];
    //    return ItemRatioFromListItemValues(item, list, predicate);
    //}

    public static int GetRandomIndexFromListValues<T> (float ratio, List<T> list, Func<T, float> predicate)
    {
        ratio = MathF.Min(ratio, 1);

        var idk = 0f;

        var total = GetListValueTotal(list, predicate);

        for (int i = 0; i < list.Count; i++)
        {
            var item = list[i];
            var itemRatio = (predicate(item) +idk)/ total;
            if (itemRatio >= ratio)
            {
                return i;
            }

            idk += itemRatio;
        }

        return list.Count - 1;
    }

    public static T GetRandomItemFromListValues<T> (float ratio, List<T> list, Func<T, float> predicate)
    {
        var index = GetRandomIndexFromListValues(ratio, list, predicate);
        if (index < list.Count) return list[index];
        return default;
    }
}

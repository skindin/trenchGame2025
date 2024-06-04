using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

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

    public static int MinMaxAvgConcToInt(float x, float min, float max, float avg, float conc)
    {
        return Mathf.RoundToInt(MinMaxAvgConc(x, min, max, avg, conc));
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

    public static (T,int)[] GetOccurancePairs<T> (List<T> list, int count, Func<T, float> chancePredicate, bool onlyReturnApplicable = true)
    {
        T[] results = new T[count];

        for (int i = 0; i < count; i++)
        {
            results[i] = GetRandomItemFromListValues(UnityEngine.Random.value, list, chancePredicate);
        }

        (T,int)[] allPairs = new (T,int)[list.Count];

        for (int i = 0; i < list.Count; i++)
        {
            var item = list[i];

            allPairs[i].Item1 = item;

            int occurances = GetTotal(results, x => x.Equals(item));

            allPairs[i].Item2 = occurances;
        }

        if (!onlyReturnApplicable) return allPairs;

        return GetItems(allPairs, x => x.Item2 > 0);
    }

    public static (T, int)[] GetOccurancePairs<T>(List<T> list, int count, Func<T, float> chancePredicate, Func<T, int> maxOccurancePredicate, bool onlyReturnApplicable = true, bool enforceCount = false)
    {
        T[] results = new T[count];

        for (int i = 0; i < count; i++)
        {
            T result;
            if (enforceCount)
            {
                bool condition(T x) => GetTotal(results, y => x.Equals(y)) < maxOccurancePredicate(x);
                result = GetRandomItemFromListValuesWithCondition(UnityEngine.Random.value, list, chancePredicate, condition);
            }
            else
            {
                result = GetRandomItemFromListValues(UnityEngine.Random.value, list, chancePredicate);
                var total = GetTotal(results, x => result.Equals(x));
                var max = maxOccurancePredicate(result);
                if (total >= max)
                {
                    result = default;
                }
            }

            results[i] = result;
        }

        (T, int)[] allPairs = new (T, int)[list.Count];

        for (int i = 0; i < list.Count; i++)
        {
            var item = list[i];

            allPairs[i].Item1 = item;

            int occurances = GetTotal(results, x => item.Equals(x));

            allPairs[i].Item2 = occurances;
        }

        if (!onlyReturnApplicable) return allPairs;

        return GetItems(allPairs, x => x.Item2 > 0);
    }

    public static T[] GetItems<T> (IEnumerable<T> list, Func<T, bool> predicate)
    {
        var applicableCount = 0;

        foreach (var item in list)
        {
            if (predicate(item)) applicableCount++;
        }

        var listCount = list.Count();
        var lastApplicableIndex = 0;

        var output = new T[applicableCount];

        for (int i = 0; i < listCount; i++)
        {
            var item = list.ElementAtOrDefault(i);

            if (predicate(item))
            {
                output[lastApplicableIndex] = item;
                lastApplicableIndex++;
            }
        }

        return output;
    }

    public static int GetTotal<T> (IEnumerable<T> list, Func<T, bool> predicate)
    {
        int total = 0;

        foreach (var item in list)
        {
            if (predicate(item))
                total++;
        }

        return total;
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

    public static T GetRandomItemFromListValuesWithCondition<T> (float ratio, List<T> list, Func<T, float> chance, Func<T, bool> condition)
    {
        ratio = MathF.Min(ratio, 1);

        var idk = 0f;

        var total = GetListValueTotal(list, chance);

        for (int i = 0; i < list.Count; i++)
        {
            var item = list[i];
            if (!condition(item)) continue;
            var itemRatio = (chance(item) + idk) / total;
            if (itemRatio >= ratio)
            {
                return list[i];
            }

            idk += itemRatio;
        }

        return default;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.ObjectModel;
using Unity.VisualScripting;
//using static UnityEditor.Progress;
//using static UnityEditor.PlayerSettings;
//using UnityEditor;
//using JetBrains.Annotations;

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

    public static float GetListValueTotal<T> (T[] list, Func<T,float> getValue)
    {
        float total = 0f;

        foreach (var item in list)
        {
            total += getValue(item);
        }

        return total;
    }

    public static float ItemRatioFromListItemValues<T>(T item, List<T> list, Func<T, float> predicate)
    {
        var itemValue = predicate(item);

        var total = GetListValueTotal(list.ToArray(), predicate);

        return itemValue / total;
    }

    public static (T,int)[] GetOccurancePairs<T> (List<T> list, int count, Func<T, float> chancePredicate, bool onlyReturnApplicable = true)
    {
        T[] results = new T[count];

        for (int i = 0; i < results.Length; i++)
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

    public static (T, int)[] GetOccurancePairs<T>(List<T> list, int count, Func<T, float> getChance, Func<T, int> getMaxOccurance, bool onlyReturnApplicable = true, bool enforceCount = false)
    {
        T[] results = new T[count];

        for (int i = 0; i < results.Length; i++)
        {
            T result = GetRandomItemFromListValues(UnityEngine.Random.value, list, getChance);
            var total = GetTotal(results, x => result.Equals(x));
            var max = getMaxOccurance(result);
            if (total >= max)
            {
                result = default;
            }

            results[i] = result;
        }

        (T, int)[] allPairs = new (T, int)[list.Count];

        for (int i = 0; i < list.Count; i++)
        {
            var item = list[i];

            allPairs[i].Item1 = item;

            int occurances = GetTotal(results,x =>{
                     return item.Equals(x); });

            allPairs[i].Item2 = occurances;
        }

        if (!onlyReturnApplicable) return allPairs;

        return GetItems(allPairs, x => x.Item2 > 0);
    }

    //public static T GetRandomItemFromListValuesWithCondition<T>(float ratio, List<T> list, Func<T, float> chance, Func<T, bool> condition)
    //{
    //    ratio = MathF.Min(ratio, 1);

    //    var idk = 0f;

    //    var total = GetListValueTotal(list, chance);

    //    for (int i = 0; i < list.Count; i++)
    //    {
    //        var item = list[i];
    //        if (!condition(item))
    //            continue;
    //        var itemRatio = (chance(item) + idk) / total;
    //        if (itemRatio >= ratio)
    //        {
    //            return list[i];
    //        }

    //        idk += itemRatio;
    //    }

    //    return default;
    //} //total didn't comply to condition, which broke this, and I don't feel like working on that rn

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

    public static int GetTotal<T> (T[] list, Func<T, bool> predicate)
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

        var total = GetListValueTotal(list.ToArray(), predicate);

        for (int i = 0; i < list.Count; i++)
        {
            var item = list[i];
            var itemRatio = predicate(item)/ total + idk;
            if (itemRatio >= ratio)
            {
                return i;
            }

            idk = itemRatio;
        }

        return list.Count - 1;
    }

    public static T GetRandomItemFromListValues<T> (float ratio, List<T> list, Func<T, float> predicate)
    {
        var index = GetRandomIndexFromListValues(ratio, list, predicate);
        if (index < list.Count) return list[index];
        return default;
    }

    static float GetDistance<T> (T item, Vector2 pos, Func<T, Vector2> getPos)
    {
        return Vector2.Distance(pos, getPos(item));
    }

    static void OnPosSelected<T> (T item, Func<T, Vector2> getPos)
    {
        GeoUtils.MarkPoint(getPos(item), .5f, Color.green);
    }

    public static T GetClosest<T>(Vector2 pos, T[] array, Func<T, Vector2> getPos, out int lowestIndex, Func<T, bool> condition = null, T defaultItem = default, float maxDist = Mathf.Infinity, bool debugLines = false)
    {
        static void MarkPos<Item> (Item item, Func<Item,Vector2> getPos, Color color)
        {
            GeoUtils.MarkPoint(getPos(item), .5f, color);
        }

        //Func<T, float> getDistance = item => Vector2.Distance(pos, getPos(item));
        Action<T> onClosest = debugLines ? item => MarkPos(item,getPos,Color.green) : null;
        Action<T> onNotClosest = debugLines ? item => MarkPos(item, getPos, Color.red) : null;

        return GetLowest(array, item => GetDistance(item,pos,getPos), out lowestIndex, condition, defaultItem, maxDist, onClosest, onNotClosest);
    }

    public static T GetLowest<T> (T[] array, Func<T, float> getValue, out int lowestIndex, Func<T, bool> condition = null, T defaultItem = default, float maxValue = Mathf.Infinity, Action<T> onSelected = null, Action<T> onNotSelected = null)
    {
        lowestIndex = -1;
        float lowestValue = maxValue;
        T lowestItem = defaultItem;

        for (int i = 0; i < array.Length; i++)
        {
            var item = array[i];

            if (condition != null && !condition(item))
                continue;

            var value = getValue(item);

            if (value < lowestValue)
            {
                lowestValue = value;
                lowestIndex = i;
                lowestItem = item;
            }
        }

        if (onNotSelected != null)
        {
            for (int i = 0; i < array.Length; i++)
            {
                var item = array[i];

                if (item is not null)
                    onNotSelected(item);
            }
        }

        if (onSelected != null && lowestItem is not null)
        {
            onSelected(lowestItem);
        }

        return lowestItem;
    }

    public static int RandomNegOrPos ()
    {
        return (UnityEngine.Random.Range(0, 2) == 1) ? 1 : -1;
    }

    public static bool RandomBool ()
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

    public static T[] FlattenArray<T>(T[,] multidimArray)
    {
        return multidimArray.Cast<T>().ToArray();
    }

    public static T[] FlattenArray<T>(T[][] jaggedArray)
    {
        //int size = Mathf.FloorToInt(GetListValueTotal(jaggedArray, obj => obj.Length));

        //var flattenedArray = new T[size];

        return jaggedArray.SelectMany(innerArray => innerArray).ToArray();
    }

    public static List<ValueType> GetValuesList<ItemType, ValueType>(List<ItemType> list, List<ValueType> valueList, Func<ItemType, ValueType> getValue, Func<ItemType, bool> condition = null, bool clearValues = false)
    {
        if (clearValues)
            valueList.Clear();
        //var result = new List<ValueType>();

        for (int i = 0; i < valueList.Count; i++)
        {
            var item = list[i];

            if (condition != null && !condition(item))
                continue;

            valueList.Add(getValue(item));
        }

        return valueList;
    }


    public static List<T> SortHighestToLowest<T>(List<T> list, Func<T,float> getValue)
    {
        for (int i = 1; i < list.Count; i++)
        {
            var current = list[i];
            int j = i - 1;

            while (j >= 0 && getValue(current) > getValue( list[j]))
            {
                list[j + 1] = list[j];
                j--;
            }

            list[j + 1] = current;
        }

        return list;
    }

    public static List<T> SortLowestToHighest<T>(List<T> list, Func<T, float> getValue)
    {
        for (int i = 1; i < list.Count; i++)
        {
            var current = list[i];
            int j = i - 1;

            while (j <= 0 && getValue(current) > getValue(list[j]))
            {
                list[j + 1] = list[j];
                j--;
            }

            list[j + 1] = current;
        }

        return list;
    }

    public static List<T> AssignIndexes<T> (List<T> list, Action<T, int> setIndex) where T : class
    {
        for (int i = 0; i < list.Count; i++)
        {
            setIndex(list[i], i);
        }

        return list;
    }

    public static float TicksToSeconds (long ticks)
    {
        return ticks / 10000000f;
    }

    public static long SecondsToTicks (float seconds)
    {
        return (long)(seconds * 10000000);
    }
}

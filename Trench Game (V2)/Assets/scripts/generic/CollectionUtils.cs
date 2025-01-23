using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
//using static UnityEditor.Progress;
//using static UnityEditor.PlayerSettings;
//using UnityEditor;
//using JetBrains.Annotations;

public static class CollectionUtils
{

    public static float GetListValueTotal<T>(T[] list, Func<T, float> getValue)
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

    public static (T, int)[] GetOccurancePairs<T>(List<T> list, int count, Func<T, float> chancePredicate, bool onlyReturnApplicable = true)
    {
        T[] results = new T[count];

        for (int i = 0; i < results.Length; i++)
        {
            results[i] = GetRandomItemFromListValues(UnityEngine.Random.value, list, chancePredicate);
        }

        (T, int)[] allPairs = new (T, int)[list.Count];

        for (int i = 0; i < list.Count; i++)
        {
            var item = list[i];

            allPairs[i].Item1 = item;

            int occurances = GetTotal(results, x => x.Equals(item));

            allPairs[i].Item2 = occurances;
        }

        if (!onlyReturnApplicable)
            return allPairs;

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

            int occurances = GetTotal(results, x => {
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

    public static T[] GetItems<T>(IEnumerable<T> list, Func<T, bool> predicate)
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

    public static int GetTotal<T>(T[] list, Func<T, bool> predicate)
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

    public static int GetRandomIndexFromListValues<T>(float ratio, List<T> list, Func<T, float> predicate)
    {
        ratio = MathF.Min(ratio, 1);

        var idk = 0f;

        var total = GetListValueTotal(list.ToArray(), predicate);

        for (int i = 0; i < list.Count; i++)
        {
            var item = list[i];
            var itemRatio = predicate(item) / total + idk;
            if (itemRatio >= ratio)
            {
                return i;
            }

            idk = itemRatio;
        }

        return list.Count - 1;
    }

    public static T GetRandomItemFromListValues<T>(float ratio, List<T> list, Func<T, float> predicate)
    {
        var index = GetRandomIndexFromListValues(ratio, list, predicate);
        if (index < list.Count)
            return list[index];

        return default;
    }

    //static float GetDistance<T> (T item, Vector2 pos, Func<T, Vector2> getPos)
    //{
    //    return Vector2.Distance(pos, getPos(item));
    //}

    public static T GetClosest<T>(Vector2 pos, List<T> list, Func<T, Vector2> getPos, out int lowestIndex, Func<T, bool> condition = null, T defaultItem = default, float maxDist = Mathf.Infinity, bool debugLines = false)
    {
        static void MarkPos<Item>(Item item, Func<Item, Vector2> getPos, Color color)
        {
            GeoUtils.MarkPoint(getPos(item), .5f, color);
        }

        //Func<T, float> getDistance = item => Vector2.Distance(pos, getPos(item));
        Action<T> onClosest = debugLines ? item => MarkPos(item, getPos, Color.green) : null;
        Action<T> onNotClosest = debugLines ? item => MarkPos(item, getPos, Color.red) : null;

        return GetLowest(list, item => Vector2.Distance(pos, getPos(item)), out lowestIndex, condition, defaultItem, maxDist, onClosest, onNotClosest);
    }

    public static T GetLowest<T>(IEnumerable<T> list, Func<T, float> getValue, out int lowestIndex, Func<T, bool> condition = null, T defaultItem = default, float maxValue = Mathf.Infinity, Action<T> onSelected = null, Action<T> onNotSelected = null)
    {
        lowestIndex = -1;
        float lowestValue = maxValue;
        T lowestItem = defaultItem;

        var i = 0;

        foreach (var item in list)
        {

            if (condition != null && !condition(item))
                continue;

            var value = getValue(item);

            if (value < lowestValue)
            {
                lowestValue = value;
                lowestIndex = i;
                lowestItem = item;
            }

            i++;
        }

        if (onNotSelected != null)
        {
            foreach (var item in list)
            {

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

    public static T GetHighest<T>(IEnumerable<T> list, Func<T, float> getValue, out int highestIndex, Func<T, bool> condition = null,
        T defaultItem = default, float minValue = 0, Action<T> onSelected = null, Action<T> onNotSelected = null)
    {
        highestIndex = -1;
        float highestValue = minValue;
        T highestItem = defaultItem;

        var i = 0;
        foreach (var item in list)
        {

            if (condition != null && !condition(item))
                continue;

            var value = getValue(item);

            if (value > highestValue)
            {
                highestValue = value;
                highestIndex = i;
                highestItem = item;
            }

            i++;
        }

        if (onNotSelected != null)
        {
            foreach (var item in list)
            {

                if (item is not null)
                    onNotSelected(item);
            }
        }

        if (onSelected != null && highestItem is not null)
        {
            onSelected(highestItem);
        }

        return highestItem;
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


    public static List<T> SortHighestToLowest<T>(List<T> list, Func<T, float> getValue)
    {
        for (int i = 1; i < list.Count; i++)
        {
            var current = list[i];
            int j = i - 1;

            while (j >= 0 && getValue(current) > getValue(list[j]))
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

    public static List<T> AssignIntPropToIndex<T>(List<T> list, Action<T, int> setIndex) where T : class
    {
        for (int i = 0; i < list.Count; i++)
        {
            setIndex(list[i], i);
        }

        return list;
    }

    public static List<T> AssignIndexesByIntCollection<T>(List<T> itemList, IEnumerable<int> indexColl, Func<T, int> getProperty)
    {
        var itemListClone = new List<T>(itemList);
        itemList.Clear();

        foreach (var index in indexColl)
        {
            for (var i = 0; i < itemListClone.Count; i++)
            {
                var item = itemListClone[i];

                if (getProperty(item) == index)
                {
                    itemList.Add(item);
                    itemListClone.RemoveAt(i);
                    break;
                }
            }
        }

        foreach (var clone in itemListClone)
        {
            itemList.Add(clone); //im so f* lazy
        }

        return itemList;
    }

    public static Dictionary<TKey, List<TObject>> SortToListDict<TObject, TKey>(IEnumerable<TObject> collection, Func<TObject, TKey> getKey)
    {
        Dictionary<TKey, List<TObject>> dictionary = new();

        foreach (var item in collection)
        {
            List<TObject> list;

            var itemKey = getKey(item);

            if (!dictionary.TryGetValue(itemKey, out list))
            {
                list = new();
                dictionary[itemKey] = list;
            }

            list.Add(item);
        }

        return dictionary;
    }


    /// <summary>
    /// returns first of every value. example: 1,5,3,8,4,4,2,2,5,6,8 would be 1,5,3,8,4,2,6
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <typeparam name="TObject"></typeparam>
    /// <param name="collection"></param>
    /// <param name="getValue"></param>
    /// <returns></returns>
    public static List<TObject> ListFirstOfEveryValue<TValue, TObject>(IEnumerable<TObject> collection, Func<TObject, TValue> getValue, int? limit = null)
    {
        List<TObject> output = new();

        HashSet<TValue> hashSet = new();

        int count = 0;

        foreach (var item in collection)
        {
            if (limit.HasValue && count >= limit.Value)
            {
                return output;
            }

            var value = getValue(item);

            if (!hashSet.Contains(value))
            {
                hashSet.Add(value);
                output.Add(item);
            }

            count++;
        }

        return output;
    }

    public static List<TObject> DictionaryToList<TObject,TKey> (Dictionary<TKey,TObject> dict)
    {
        var list = new List<TObject>();

        foreach (var pair in dict)
        {
            list.Add(pair.Value);
        }

        return list;
    }

    public static void RandomizeOrder<T> (List<T> list, Func<int,int,int> getIndex)
    {
        var indexList = GetRandomizedIntList(list.Count, getIndex);

        var referenceClone = new List<T>(list);

        for (int i = 0; i < list.Count; i++)
        {
            list[i] = referenceClone[indexList[i]];
        }
    }

    public static List<int> GetRandomizedIntList (int count, Func<int,int,int> getIndex)
    {
        var output = new List<int>();

        for (int i = 0; i < count; i++)
        {
            var index = getIndex(0, output.Count + 1);

            if (index > output.Count)
            {
                output.Add(i);
            }
            else 
            {
                output.Insert(index,i);
            }
        }

        return output;
    }
}

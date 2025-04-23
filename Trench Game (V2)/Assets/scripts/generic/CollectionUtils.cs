using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
//using static UnityEditor.Progress;
//using static UnityEditor.PlayerSettings;
//using UnityEditor;
//using JetBrains.Annotations;

public static class CollectionUtils //this is just a script for all sorts of convenient collection functions
{
    /// <summary>
    /// returns the total value of the values of all objects in the list
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="getValue"></param>
    /// <returns></returns>
    public static float GetListValueTotal<T>(T[] list, Func<T, float> getValue)
    {
        //tbh, i wrote this before i learned about ienumerables, which is why this returns an array
        float total = 0f;

        foreach (var item in list)
        {
            total += getValue(item);
        }

        return total;
    }

    /// <summary>
    /// returns the one items value divided by the total values
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="item"></param>
    /// <param name="list"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public static float ItemRatioFromListItemValues<T>(T item, List<T> list, Func<T, float> predicate)
    {
        var itemValue = predicate(item);

        var total = GetListValueTotal(list.ToArray(), predicate);

        return itemValue / total;
    }

    /// <summary>
    /// returns a set of random items within the list, each occurance determined by a designated chance property of every item.
    /// only return applicable removes any pairs that were selected 0 times
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="count"></param>
    /// <param name="chancePredicate"></param>
    /// <param name="onlyReturnApplicable"></param>
    /// <returns></returns>

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
        //tbh i guess i never ended up implementing the enforce count variable, which is probably why the item drop generation was bugged
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


    /// <summary>
    /// returns items that pass predicate. this is super overcomplicated becase i designed it for arrays and have yet to improve it lol
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
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

    /// <summary>
    /// returns total amount of items that passed predicate func
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
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

    /// <summary>
    /// returns random index of list, each index's chance being determined by the predicate value of each item
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="ratio"></param>
    /// <param name="list"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
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

    /// <summary>
    /// returns random item, each items chance being found by the predicate
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="ratio"></param>
    /// <param name="list"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
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

    /// <summary>
    /// returns the element closest to the pos. closestIndex returns the index of the closest element. optional condition func. default item in case no items are closer than max dist
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="pos"></param>
    /// <param name="list"></param>
    /// <param name="getPos"></param>
    /// <param name="closestIndex"></param>
    /// <param name="condition"></param>
    /// <param name="defaultItem"></param>
    /// <param name="maxDist"></param>
    /// <param name="debugLines"></param>
    /// <returns></returns>
    public static T GetClosest<T>(Vector2 pos, List<T> list, Func<T, Vector2> getPos, out int closestIndex, Func<T, bool> condition = null, T defaultItem = default, float maxDist = Mathf.Infinity, bool debugLines = false)
    {
        static void MarkPos<Item>(Item item, Func<Item, Vector2> getPos, Color color)
        {
            GeoUtils.MarkPoint(getPos(item), .5f, color);
        }

        //Func<T, float> getDistance = item => Vector2.Distance(pos, getPos(item));
        Action<T> onClosest = debugLines ? item => MarkPos(item, getPos, Color.green) : null;
        Action<T> onNotClosest = debugLines ? item => MarkPos(item, getPos, Color.red) : null;

        return GetLowest(list, item => Vector2.Distance(pos, getPos(item)), out closestIndex, condition, defaultItem, maxDist, onClosest, onNotClosest);
    }


    /// <summary>
    /// returns item with lowest value. lowestIndex returns index of lowest item. optional condition func. default item in case no values are less than maxValue. actions for when a previous item was lower or not
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="getValue"></param>
    /// <param name="lowestIndex"></param>
    /// <param name="condition"></param>
    /// <param name="defaultItem"></param>
    /// <param name="maxValue"></param>
    /// <param name="onSelected"></param>
    /// <param name="onNotSelected"></param>
    /// <returns></returns>
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


    /// <summary>
    /// just a reverse version of getLowest. i haven't actually used this one but i assume it works
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="getValue"></param>
    /// <param name="highestIndex"></param>
    /// <param name="condition"></param>
    /// <param name="defaultItem"></param>
    /// <param name="minValue"></param>
    /// <param name="onSelected"></param>
    /// <param name="onNotSelected"></param>
    /// <returns></returns>
    public static T GetHighest<T>(IEnumerable<T> list, Func<T, float> getValue, out int highestIndex, Func<T, bool> condition = null,
        T defaultItem = default, float minValue = 0, Action<T> onSelected = null, Action<T> onNotSelected = null)
    {
        var output = GetLowest(list, item => -getValue(item), out var lowestIndex, condition, defaultItem, -minValue, onSelected, onNotSelected);

        highestIndex = -lowestIndex;

        return output;
    }

    /// <summary>
    /// funny attempt to use a bell graph. doesn't really work the way i want
    /// </summary>
    /// <param name="x"></param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <param name="avg"></param>
    /// <param name="conc"></param>
    /// <returns></returns>
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

    /// <summary>
    /// creates ienumerable of list item values. optional condition value
    /// </summary>
    /// <typeparam name="ItemType"></typeparam>
    /// <typeparam name="ValueType"></typeparam>
    /// <param name="list"></param>
    /// <param name="getProperty"></param>
    /// <param name="condition"></param>
    /// <returns></returns>
    public static IEnumerable<ValueType> GetPropertyCollection<ItemType, ValueType>(IEnumerable<ItemType> list, Func<ItemType, ValueType> getProperty, Func<ItemType, bool> condition = null)
    {
        //var result = new List<ValueType>();

        foreach (var item in list)
        {
            if (condition != null && !condition(item))
                continue;

            yield return getProperty(item);
        }
    }

    /// <summary>
    /// sorts a list by element values from highest to lowest
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="getValue"></param>
    /// <returns></returns>
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

    /// <summary>
    /// sorts list by element values from lowest to highest
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="getValue"></param>
    /// <returns></returns>
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

    /// <summary>
    /// assigns int property of items to their index within the list
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="setIndex"></param>
    /// <returns></returns>
    public static List<T> AssignIntPropToIndex<T>(List<T> list, Action<T, int> setIndex) where T : class
    {
        for (int i = 0; i < list.Count; i++)
        {
            setIndex(list[i], i);
        }

        return list;
    }

    /// <summary>
    /// sorts a list in the order that their property values appear in the index collection. example: {apple(1), peach(3), mango(0)} and {0,1,3} would return {mango, apple, peach}
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="itemList"></param>
    /// <param name="indexColl"></param>
    /// <param name="getProperty"></param>
    /// <returns></returns>
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

    /// <summary>
    /// creates a dictionary of all list elements and associated values
    /// </summary>
    /// <typeparam name="TObject"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <param name="collection"></param>
    /// <param name="getKey"></param>
    /// <returns></returns>
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
    /// returns first of every new value. example: 1,5,3,8,4,4,2,2,5,6,8 would be 1,5,3,8,4,2,6
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

    /// <summary>
    /// creates a list of all a dictionary's values
    /// </summary>
    /// <typeparam name="TObject"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <param name="dict"></param>
    /// <returns></returns>
    public static List<TObject> DictionaryValuesToList<TObject,TKey> (Dictionary<TKey,TObject> dict)
    {
        var list = new List<TObject>();

        foreach (var pair in dict)
        {
            list.Add(pair.Value);
        }

        return list;
    }


    /// <summary>
    /// randomly orders the elements of a list
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="getIndex"></param>
    public static void RandomizeOrder<T> (List<T> list, Func<int,int,int> getIndex)
    {
        var indexList = GetRandomizedIntList(list.Count, getIndex);

        var referenceClone = new List<T>(list);

        for (int i = 0; i < list.Count; i++)
        {
            list[i] = referenceClone[indexList[i]];
        }
    }

    /// <summary>
    /// gets list ints from 0 to count-1 in random order
    /// </summary>
    /// <param name="count"></param>
    /// <param name="getIndex"></param>
    /// <returns></returns>
    public static List<int> GetRandomizedIntList(int count, Func<int, int, int> getIndex)
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
                output.Insert(index, i);
            }
        }

        return output;
    }

    /// <summary>
    /// basically a dictionary but works both ways
    /// </summary>
    /// <typeparam name="Type1"></typeparam>
    /// <typeparam name="Type2"></typeparam>
    public class TwoColumnTable<Type1, Type2> : IEnumerable<ColumnRow<Type1,Type2>>
    {
        readonly Dictionary<Type1, Type2> column1 = new();
        readonly Dictionary<Type2, Type1> column2 = new();

        public void Add(Type1 value1, Type2 value2)
        {
            if (column1.ContainsKey(value1))
            {
                throw new Exception($"Table column 1 already contained value {value1}");
            }
            else if (column2.ContainsKey(value2))
            {
                throw new Exception($"Table column 2 already contained value {value2}");
            }

            column1.Add(value1, value2);
            column2.Add(value2, value1);
        }

        public bool TryAdd(Type1 value1, Type2 value2)
        {
            if (!column1.ContainsKey(value1) && !column2.ContainsKey(value2))
            {
                column1.Add(value1, value2);
                column2.Add(value2, value1);

                return true;
            }

            return false;
        }

        public bool TryGet2From1 (Type1 value1, out Type2 value2)
        {
            if (column1.TryGetValue(value1, out value2))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TryGet1From2(Type2 value2, out Type1 value1)
        {
            if (column2.TryGetValue(value2, out value1))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool RemoveFromColumn1 (Type1 value1)
        {
            if (column1.TryGetValue(value1, out var value2))
            {
                column1.Remove(value1);
                column2.Remove(value2);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void OverrideColumn2 (Type1 value1, Type2 value2)
        {
            var prevValue2 = column1[value1];
            column1[value1] = value2;
            column2.Remove(prevValue2);
            column2.Add(value2, value1);
        }

        public void OverrideColumn1 (Type1 value1, Type2 value2)
        {
            var prevValue1 = column2[value2];
            column2[value2] = value1;
            column1.Remove(prevValue1);
            column1.Add(value1, value2);
        }

        public bool RemoveFromColumn2(Type2 value2)
        {
            if (column2.TryGetValue(value2, out var value1))
            {
                column2.Remove(value2);
                column1.Remove(value1);
                return true;
            }
            else
            {
                return false;
            }
        }

        public IEnumerator<ColumnRow<Type1,Type2>> GetEnumerator()
        {
            foreach (var pair in column1)
            {
                yield return new ColumnRow<Type1,Type2>(pair.Key, pair.Value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    } 
    
    /// <summary>
    /// like keyValuePairs but for twoColumnTable
    /// </summary>
    /// <typeparam name="Type1"></typeparam>
    /// <typeparam name="Type2"></typeparam>
    public struct ColumnRow<Type1, Type2>
    {
        public Type1 value1;
        public Type2 value2;

        public ColumnRow(Type1 value1, Type2 value2)
        {
            this.value1 = value1;
            this.value2 = value2;
        }
    }

    /// <summary>
    /// object that returns random ints between min and max value. will not repeat a value before all values have been recycled.
    /// example: if 1, 2, 4, and 5 have been returned, it will not repeat these numbers until a 3 is returned
    /// </summary>
    public class RandomIntSeries
    {
        readonly int min, max;
        readonly Func<int, int, int> randomAccessor;
        readonly List<int> ints = new();

        public RandomIntSeries(int min, int max, Func<int, int, int> randomAccessor)
        {
            this.min = min;
            this.max = max;
            this.randomAccessor = randomAccessor;
        }

        public void Reset()
        {
            ints.Clear(); //
        }

        void AddIntsBetweenMinMax()
        {
            for (int i = min; i < max; i++)
            {
                ints.Add(i);
            }
        }

        public int Get()
        {
            if (ints.Count == 0)
            {
                AddIntsBetweenMinMax();
            }

            var intIndex = randomAccessor(0,ints.Count);

            var value = ints[intIndex];

            ints.RemoveAt(intIndex);

            return value;
        }
    }
}
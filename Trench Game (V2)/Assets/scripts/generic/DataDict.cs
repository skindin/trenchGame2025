using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Windows.Speech;

[System.Serializable]
public struct DataDict<T> : ICustomJson<DataDict<T>>
{
    string[] keys;
    T[] values;
    int count;

    public DataDict(int startSize = 0)
    {
        keys = new string[startSize];
        values = new T[startSize];
        count = 0;
    }

    public DataDict(params (string, T)[] entries) //eventually gotta make sure none of these keys are the same
    {
        keys = new string[entries.Length];
        values = new T[entries.Length];

        for (int i = 0; i < entries.Length; i++)
        {
            keys[i] = entries[i].Item1;
            values[i] = entries[i].Item2;
        }

        count = entries.Length;
    }

    public DataDict(string key, T value) : this((key, value))
    {

    }

    //public T this[string key]
    //{
    //    get
    //    {
    //        return GetValue(key);
    //    }
    //}

    public bool TryKey(string queryKey, out T value)
    {
        for (int i = 0; i < keys.Length; i++)
        {
            var key = keys[i];

            if (queryKey.Equals(key))
            {
                value = values[i];
                return true;
            }
        }

        value = default;
        return false;
    }

    //T GetValue (string queryKey)
    //{
    //    for (int i = 0; i < keys.Length; i++)
    //    {
    //        var key = keys[i];

    //        if (queryKey.Equals(key))
    //        {
    //            return values[i];
    //        }
    //    }

    //    return default;
    //    //throw new Exception($"DataDict did not contain key {queryKey.ToString()}");
    //}

    public static void Combine(ref DataDict<T> dict, string newKey, T newValue, bool replace = false, int incoming = 1)
    {
        for (int i = 0; i < dict.keys.Length; i++)
        {
            var key = dict.keys[i];

            if (key == newKey)
            {
                if (replace)
                {
                    dict.values[i] = newValue;
                    return;
                }
                else
                {
                    throw new Exception($"DictData already had Key " + newKey.ToString());
                }
            }
        }

        if (dict.count + incoming > dict.keys.Length)
        {
            var newSize = dict.count + incoming;

            Array.Resize(ref dict.keys, newSize);
            Array.Resize(ref dict.values, newSize);
        }

        dict.keys[dict.count] = newKey;
        dict.values[dict.count] = newValue;

        dict.count++;
    }

    public static void Combine (ref DataDict<T> dict, params(string, T)[] entries)
    {
        for (int i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];

            var incoming = entries.Length - i;

            Combine(ref dict, entry.Item1, entry.Item2, false, incoming);
        }
    }

    public string ToJson()
    {
        string json = "{";

        for (int i = 0; i < keys.Length; i++)
        {
            if (i > 0)
                json += ", ";

            string keyJson = keys[i];

            var value = values[i];

            string valueJson;

            if (value is ICustomJson<object> custom)
            {
                valueJson = custom.ToJson();
            }
            else
            {
                valueJson = value.ToString();
            }

            json += $"\"{keyJson}\": {valueJson}";
        }

        json += "}";

        return json;
    }

    public static DataDict<T> JsonToObj(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            throw new ArgumentException("JSON string is null or empty.");
        }

        // Remove surrounding braces
        json = json.Trim('{', '}');

        // Split by comma to get key-value pairs
        string[] pairs = json.Split(',');

        // Initialize arrays for keys and values
        string[] keys = new string[pairs.Length];
        T[] values = new T[pairs.Length];

        for (int i = 0; i < pairs.Length; i++)
        {
            // Split each pair into key and value
            string[] keyValue = pairs[i].Trim().Split(':');

            // Remove surrounding quotes from key
            string key = keyValue[0].Trim().Trim('\"');

            // Remove surrounding quotes and spaces from value
            string valueJson = keyValue[1].Trim().Trim('\"', ' ');

            // Deserialize value based on its type
            if (typeof(T) == typeof(float))
            {
                if (float.TryParse(valueJson, out float floatValue))
                {
                    values[i] = (T)(object)floatValue;
                }
                else
                {
                    throw new FormatException($"Unable to parse '{valueJson}' as float.");
                }
            }
            else if (typeof(T) == typeof(int))
            {
                if (int.TryParse(valueJson, out int intValue))
                {
                    values[i] = (T)(object)intValue;
                }
                else
                {
                    throw new FormatException($"Unable to parse '{valueJson}' as int.");
                }
            }
            else if (typeof(T) == typeof(bool))
            {
                if (bool.TryParse(valueJson, out bool boolValue))
                {
                    values[i] = (T)(object)boolValue;
                }
                else
                {
                    throw new FormatException($"Unable to parse '{valueJson}' as bool.");
                }
            }
            else
            {
                // For other types, assuming they are strings
                values[i] = (T)(object)valueJson;
            }

            // Assign key to keys array
            keys[i] = key;
        }

        // Create and return DataDict<T> instance
        return new DataDict<T>(keys.Zip(values, (k, v) => (k, v)).ToArray());
    }

}

public interface ICustomJson<T>
{
    public string ToJson();

    public static T JsonToObj (string json)
    {
        return default;
    }
}

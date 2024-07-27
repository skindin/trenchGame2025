using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Logger
{
    public static List<string> logs = new List<string>();

    public static void Log (string message)
    {
#if UNITY_EDITOR
        Debug.Log (message);
#else
        Console.WriteLine(message);
#endif

        logs.Add (message);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ManagerBase<T> : MonoBehaviour where T :MonoBehaviour
{
    static T cached;

    public static T Manager
    { 
        get
        {
            if (!cached)
            {
                cached = FindObjectOfType<T>();
            }


            return cached;
        }
    }
}

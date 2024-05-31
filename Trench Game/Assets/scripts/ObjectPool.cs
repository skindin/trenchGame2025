using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[System.Serializable]
public class ObjectPool<T> where T : class
{
    public List<T> objects = new();
    public Func<T> newFunc; 
    public Action<T> resetAction, disableAction, removeAction;
    public int currentPooled, maxPooled = 100;

    public ObjectPool (Func<T> newFunc, Action<T> resetAction, Action<T> disableAction, Action<T> removeAction)
    {
        this.newFunc = newFunc;
        this.resetAction = resetAction;
        this.disableAction = disableAction;
        this.removeAction = removeAction;
    }

    public T Get ()
    {
        T obj;

        if (objects.Count > 0)
        {
            obj = objects[0];
            objects.RemoveAt(0);
            resetAction?.Invoke(obj);
        }
        else
        {
            obj = newFunc();
        }

        currentPooled = objects.Count;

        return obj;
    }

    public void Remove (T obj)
    {
        if (objects.Count < maxPooled)
        {
            objects.Add(obj);
            disableAction?.Invoke(obj);
        }
        else
            removeAction?.Invoke(obj);

        currentPooled = objects.Count;
    }
}
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
    public int currentPooled, minPooled = 5, maxPooled = 100;

    public ObjectPool (int minPooled = 0, int maxPooled = 100, Func<T> newFunc = null, Action<T> resetAction = null, Action<T> disableAction = null, Action<T> removeAction = null)
    {
        this.newFunc = newFunc;
        this.resetAction = resetAction;
        this.disableAction = disableAction;
        this.removeAction = removeAction;

        this.minPooled = minPooled;
        this.maxPooled = maxPooled;


        EnsureMin();
    }

    void EnsureMin ()
    {
        for (int i = 0; minPooled > i; i = objects.Count)
        {
            var obj = newFunc();
            objects.Add(obj);
            disableAction?.Invoke(obj);
        }
    }

    public T GetFromPool ()
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

        EnsureMin();

        return obj;
    }

    public void AddToPool (T obj)
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
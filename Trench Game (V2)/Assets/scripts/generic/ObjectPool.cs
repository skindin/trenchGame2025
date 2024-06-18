using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ObjectPool<T> where T : class
{
    public Queue<T> objects = new();
    Func<T> newFunc;
    Action<T> resetAction, disableAction, destroyAction;
    //public int currentPooled;
    public int minPooled = 5;
    public int maxPooled = 100;

    public ObjectPool(int minPooled = 0, int maxPooled = 100, Func<T> newFunc = null, Action<T> resetAction = null, Action<T> disableAction = null, Action<T> destroyAction = null)
    {
        objects = new();

        this.newFunc = newFunc ?? throw new ArgumentNullException(nameof(newFunc));
        this.resetAction = resetAction;
        this.disableAction = disableAction;
        this.destroyAction = destroyAction;

        this.minPooled = minPooled;
        this.maxPooled = maxPooled;

        EnsureMin();
    }

    private void EnsureMin()
    {
        while (objects.Count < minPooled)
        {
            var obj = newFunc();
            objects.Enqueue(obj);
            disableAction?.Invoke(obj);
        }
    }

    public T GetFromPool()
    {
        T obj;

        if (objects.Count > 0)
        {
            obj = objects.Dequeue();
            resetAction?.Invoke(obj);
        }
        else
        {
            obj = newFunc();
            //resetAction?.Invoke(obj); //prob unnecessary
        }

        EnsureMin();

        //if (obj is Item item)
        //{
        //    Debug.Log($"Item {item} {item.gameObject.GetInstanceID()} chunk was {(item.Chunk != null ? $"{item.Chunk} {item.Chunk.adress} which {(item.Chunk.items.Contains(item) ? $"contained " : "didn't contain ")}item" : "null")} when taken out of pool");
        //}

        return obj;
    }

    public void AddToPool(T obj)
    {
        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        if (objects.Count < maxPooled)
        {
            if (!objects.Contains(obj))  // Avoid adding the same object multiple times
            {
                objects.Enqueue(obj);
                disableAction?.Invoke(obj);
            }
        }
        else
        {
            destroyAction?.Invoke(obj);
        }

        //if (obj is Item item)
        //{
        //    Debug.Log($"Item {item} {item.gameObject.GetInstanceID()} chunk was {(item.Chunk == null ? "null ": $"chunk {item.Chunk.adress}")} when it was added to pool");
        //}
    }
}

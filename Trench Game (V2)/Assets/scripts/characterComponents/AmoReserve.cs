using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AmoReserve : MonoBehaviour
{
    public Character character;

    public List<AmoPool> amoPools = new();

    public int AddAmo(AmoType type, int amount)
    {
        var typeReserve = amoPools.Find(x => x.type == type);
        if (typeReserve != null)
        {
            return typeReserve.AddAmo(amount);
        }
        
        Debug.Log("Reserve doesn't store amo type " + type.name);
        return amount;
    }

    public int RemoveAmo(AmoType type, int amount)
    {
        var typeReserve = amoPools.Find(x => x.type == type);
        if (typeReserve != null)
        {
            var leftOver = typeReserve.RemoveAmo(amount);

            if (character.inventory)
                character.inventory.DetectItems(); //when they reload, refill reserve

            return leftOver;
        }

        Debug.Log("Reserve doesn't contain amo of type " + type.name);
        return 0;
    }

    public int GetAmoAmount (AmoType type)
    {
        var typeReserve = amoPools.Find(x => x.type == type);
        if (typeReserve != null)
        {
            return typeReserve.rounds;
        }

        return 0;
    }

    public AmoPool GetPool (AmoType type)
    {
        var typeReserve = amoPools.Find(x => x.type == type);
        if (typeReserve != null)
        {
            return typeReserve;
        }

        return null;
    }

    public string GetInfo (string separator = " ")
    {
        var array = new string[amoPools.Count];

        for (int i = 0; i < amoPools.Count; i++)
        {
            var pool = amoPools[i];

            array[i] = $"{pool.rounds}/{pool.maxRounds} {pool.type.name}";
        }

        return string.Join(separator, array) ;
    }

    public void DropEverything (float dropRadius)
    {
        foreach (var pool in amoPools)
        {
            if (pool.rounds <= 0) continue;
            var pos = Random.insideUnitCircle * dropRadius + (Vector2)transform.position;
            SpawnManager.Manager.DropAmo(pool.type, pool.rounds, pos );
            pool.rounds = 0;
        }
    }

    public void Clear ()
    {
        foreach (var pool in amoPools)
        {
            pool.rounds = 0;
        }
    }

    //public DataDict<object> Data
    //{
    //    get
    //    {
    //        var data = new DataDict<object> (amoPools.Count);

    //        for (int i = 0;i < amoPools.Count;i++)
    //        {
    //            var pool = amoPools[i];

    //            var poolData = pool.Data;//temporary

    //            DataDict<object>.Combine(ref data, (i.ToString(),pool.Data));
    //        }

    //        return data;
    //    }
    //}
}

[System.Serializable]
public class AmoPool
{
    public AmoType type;
    public int rounds = 0, maxRounds = 100;

    public int AddAmo (int count)
    {
        var spaceLeft = maxRounds - rounds;
        var leftover = Mathf.Max(count - spaceLeft,0);
        rounds += Mathf.Min(spaceLeft, count);
        return leftover;
    }

    public int RemoveAmo (int count)
    {
        var avlblRounds = Mathf.Min(count, rounds);
        rounds -= avlblRounds;
        return avlblRounds;
    }

    //public DataDict<object> Data
    //{
    //    get
    //    {
    //        return new(
    //            (Naming.amoType, type.name),
    //            (Naming.rounds, rounds),
    //            (Naming.maxRounds, maxRounds)
    //            );
    //    }
    //}
}

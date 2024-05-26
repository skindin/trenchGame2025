using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AmoReserve : MonoBehaviour
{
    public List<AmoTypeReserve> typeReserves = new();

    public int AddAmo(AmoType type, int amount)
    {
        var typeReserve = typeReserves.Find(x => x.type == type);
        if (typeReserve != null)
        {
            return typeReserve.AddAmo(amount);
        }
        
        Debug.Log("Reserve doesn't store amo type " + type.name);
        return amount;
    }

    public int RemoveAmo(AmoType type, int amount)
    {
        var typeReserve = typeReserves.Find(x => x.type == type);
        if (typeReserve != null)
        {
            return typeReserve.RemoveAmo(amount);
        }

        Debug.Log("Reserve doesn't contain amo of type " + type.name);
        return 0;
    }

    public int GetAmoAmount (AmoType type)
    {
        var typeReserve = typeReserves.Find(x => x.type == type);
        if (typeReserve != null)
        {
            return typeReserve.rounds;
        }

        return 0;
    }

    public AmoTypeReserve GetReserve (AmoType type)
    {
        var typeReserve = typeReserves.Find(x => x.type == type);
        if (typeReserve != null)
        {
            return typeReserve;
        }

        return null;
    }
}

[System.Serializable]
public class AmoTypeReserve
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
}

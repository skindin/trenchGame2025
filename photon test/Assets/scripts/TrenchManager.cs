using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrenchManager : MonoBehaviour
{
    List<Trench> pool = new();
    public List<Trench> trenches = new();
    public int endRes = 4, cornerRes = 4, maxPooled = 100;
    public float maxTrenchArea = 100;
    public bool debugLines = false;

    private void Awake()
    {
        if (!Trench.manager) Trench.manager = this;
    }

    private void Update()
    {
        if (!Trench.manager) Trench.manager = this;
    }

    Trench NewTrench ()
    {
        Trench newTrench;
        if (pool.Count > 0)
        {
            newTrench = pool[0];
            pool.Remove(newTrench);
        }
        else
        {
            newTrench = new();
        }

        trenches.Add(newTrench);

        return newTrench;
    }

    public void RemoveTrench (Trench trench)
    {
        trenches.Remove(trench);

        if (pool.Count < maxPooled)
        {
            pool.Add(trench);
        }

        trench.lineMesh.points.Clear();
    }

    /// <summary>
    /// insert 'null' into digger parameter if you don't want to assign a new digger
    /// </summary>
    /// <param name="point"></param>
    /// <param name="width"></param>
    /// <param name="trench"></param>
    /// <param name="digger"></param>
    /// <returns></returns>

    public Trench DigTrench (Vector2 point, float width, Trench trench, TrenchDigger digger)
    {
        if (trench == null || trench.lineMesh.area >= maxTrenchArea)
        {
            trench = NewTrench();
        }

        //trench.lineMesh.SetWidth(width);
        trench.lineMesh.width = width;
        trench.AddPoint(point);

        if (digger != null)
            trench.digger = digger;

        return trench;
    }

    public bool TestWithinTrench (Vector2 pos)
    {
        var chunk = Chunk.manager.ChunkFromPos(pos,false);

        if (chunk == null) return false;

        foreach (var trench in chunk.trenches)
        {
            if (trench.lineMesh.TestBox(pos,debugLines) && trench.TestWithin(pos,debugLines))
            {
                return true;
            }
        }

        return false;
    }

    public void RegenerateMesh (Trench trench)
    {
        trench.lineMesh.NewMesh(endRes, cornerRes);
    }
}
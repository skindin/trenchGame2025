using System.Collections.Generic;
using UnityEngine;

public class TrenchManager : MonoBehaviour
{//what the fuck is happening
    readonly List<Trench> pool = new();
    public List<Trench> trenches = new();
    public int endRes = 4, cornerRes = 4, maxPooled = 100;
    public float maxTrenchArea = 100;
    public bool debugLines = false;
    List<Chunk> chunkFillList = new();

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
        
        trench.OnRemove();
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

    public void FillTrenches (Vector2 pos, float width)
    {
        var boxDelta = Vector2.one * width / 2;
        var boxMin = pos - boxDelta;
        var boxMax = pos + boxDelta;

        chunkFillList.Clear();

        Chunk.manager.ChunksFromBox(boxMin,boxMax,chunkFillList,false);

        for (int chunkI = 0; chunkI < chunkFillList.Count; chunkI++)
        {
            var chunk = chunkFillList[chunkI];

            if (debugLines) Chunk.manager.DrawChunk(chunk, new(.75f, .25f, 0)); //orange heheh
            //this isn't optimized. it checks every trench in the chunk,
            //without even considering whether the trench overlaps

            for (int trenchI = 0; trenchI < chunk.trenches.Count; trenchI++)
            {
                var trench = chunk.trenches[trenchI];

                if (!trench.lineMesh.TestBoxOverlap(boxMin, boxMax)) continue;

                Vector2 lastPoint = Vector2.zero;

                for (int pointI = 0; pointI < trench.lineMesh.points.Count; pointI++)
                {
                    var point = trench.lineMesh.points[pointI];

                    if (pointI > 0 && debugLines) Debug.DrawLine(point, lastPoint);

                    var dist = Vector2.Distance(pos, point);
                    if (dist <= width/2)
                    {
                        var newTrench = trench.Split(pointI, NewTrench(), false);

                        if (newTrench.lineMesh.points.Count == 0)
                        {
                            RemoveTrench(newTrench);
                        }
                        else
                        {
                            RecalculateTrench(newTrench);
                        }

                        if (trench.lineMesh.points.Count == 0)
                        {

                            RemoveTrench(trench);
                            //Debug.Log("new trench was removed");
                            trenchI--;
                            var newChunkI = Chunk.manager.chunks.IndexOf(chunk);
                            chunkI = Mathf.Clamp(chunkI, 0, newChunkI);
                        }
                        else
                        {
                            RecalculateTrench(trench);
                        }

                        pointI = 0;
                    }

                    lastPoint = point;
                }
            }
        }
    }

    public Trench TestAllTrenches (Vector2 pos)
    {
        var chunk = Chunk.manager.ChunkFromPos(pos,false);

        if (chunk == null) return null;

        foreach (var trench in chunk.trenches)
        {
            if (TestTrench(pos, trench))
            {
                return trench;
            }
        }

        return null;
    }

    public bool TestTrench (Vector2 pos, Trench trench)
    {
        return trench.lineMesh.TestBoxWithPoint(pos, debugLines) && trench.TestWithin(pos, debugLines);
    }

    public void RegenerateMesh (Trench trench)
    {
        trench.lineMesh.NewMesh(endRes, cornerRes, debugLines);
    }

    public void RecalculateTrench (Trench trench)
    {
        RegenerateMesh(trench);
        trench.lineMesh.CalculateBox();
        Chunk.manager.AutoAssignChunks(trench);
    }
}
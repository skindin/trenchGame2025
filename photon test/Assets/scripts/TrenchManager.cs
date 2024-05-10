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
    List<(int, int)> intTuples = new();
    public int pooledCount, activeCount, totalCount;

    private void Awake()
    {
        if (!Trench.manager) Trench.manager = this;
    }

    private void Update()
    {
        if (!Trench.manager) Trench.manager = this;


        //if (debugLines)
        //    foreach (var trench in trenches)
        //    {
        //        Vector2 lastPoint = Vector2.zero;

        //        for (int i = 0; i < trench.lineMesh.points.Count; i++)
        //        {
        //            var point = trench.lineMesh.points[i];

        //            if (i > 0) Debug.DrawLine(lastPoint, point, Color.black);
        //            else DrawX(point, .5f, Color.red);

        //            lastPoint = point;
        //        }
        //    }
    }

    Trench NewTrench ()
    {
        Trench newTrench;
        if (pool.Count > 0)
        {
            newTrench = pool[0];
            pool.Remove(newTrench);
            pooledCount--;
        }
        else
        {
            newTrench = new();
        }

        activeCount++;

        totalCount = activeCount + pooledCount;

        trenches.Add(newTrench);

        return newTrench;
    }

    public void RemoveTrench (Trench trench)
    {
        trenches.Remove(trench);

        if (pool.Count < maxPooled)
        {
            pool.Add(trench);
            pooledCount++;
        }

        activeCount--;

        totalCount = pooledCount + activeCount;
        
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
        if (trench == null || TrenchExceedsMaxArea(trench))
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

        Chunk.manager.ChunksFromBox(boxMin,boxMax,chunkFillList,false,true);

        if (debugLines) LineMesh.DrawBox(boxMin, boxMax, Color.magenta);

        for (int chunkI = 0; chunkI < chunkFillList.Count; chunkI++)
        {
            var chunk = chunkFillList[chunkI];

            //if (debugLines) Chunk.manager.DrawChunk(chunk, Color.green); //orange heheh

            for (int trenchI = 0; trenchI < chunk.trenches.Count; trenchI++)
            {
                var trench = chunk.trenches[trenchI];

                if (debugLines) trench.lineMesh.DrawMeshBox();

                if (!trench.lineMesh.TestBoxOverlap(boxMin, boxMax))
                {
                    continue;
                }

                if(debugLines) DrawCircle(pos, width/2, Color.cyan);

                Vector2 lastPoint = Vector2.zero;
                bool lastWithin = false;

                intTuples.Clear();

                for (int pointI = 0; pointI < trench.lineMesh.points.Count; pointI++)
                {
                    var point = trench.lineMesh.points[pointI];

                    if (pointI > 0 && debugLines) Debug.DrawLine(point, lastPoint, Color.black);

                    var dist = Vector2.Distance(pos, point);
                    var withinDist = dist <= width / 2;

                    if (withinDist && (!lastWithin || intTuples.Count == 0))
                    {
                        intTuples.Add((pointI, 1));
                    }
                    else if (withinDist && lastWithin)
                    {
                        var tuple = intTuples[^1];
                        intTuples[^1] = new(tuple.Item1, tuple.Item2 + 1);
                    }

                    if (pointI == trench.lineMesh.points.Count-1)
                    {
                        DrawX(point, .5f, Color.red);
                        break;
                    }

                    lastWithin = withinDist;
                    lastPoint = point;//must be after anything accessing lastPoint
                }

                var indexDelta = 0;

                foreach (var tuple in intTuples)
                {
                    Trench newTrench = null;

                    var index = tuple.Item1 - (indexDelta);

                    var count = tuple.Item2;

                    if (index > 0)
                    {
                        newTrench = NewTrench();
                    }

                    trench.SplitAtPoints(index, count, newTrench);

                    if (newTrench != null) RecalculateTrench(newTrench);

                    if (trench.lineMesh.points.Count == 0)
                    {
                        RemoveTrench(trench);
                        var currentChunkI = Chunk.manager.chunks.IndexOf(chunk);

                        chunkI = Mathf.Min(chunkI, currentChunkI);

                        //if (trench.lineMesh.points.Count == 0 && trench.lineMesh.mesh.triangles.Length > 0)
                        //{
                        //    var bruh = false;
                        //}

                        //this'll have to be changed once multiple diggers are digging and filling
                    }
                    else
                    {
                        RecalculateTrench(trench);
                    }

                    indexDelta += index + count;
                }
            }
        }
    }

    public Trench TestAllTrenches (Vector2 pos)
    {
        var chunk = Chunk.manager.ChunkFromPos(pos,false,true);

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

    /// <summary>
    /// Returns true if trench area more than max area
    /// </summary>
    /// <returns></returns>
    public bool TrenchExceedsMaxArea (Trench trench)
    {
        return trench.lineMesh.area > maxTrenchArea;
    }

    public static void DrawCircle (Vector3 center, float radius, Color color, int res = 4)
    {
        Vector3 lastPoint = Vector2.up * radius;

        int verts = res * 4;
        var angle = 360f / verts;

        for (int i = 1; i < verts+1; i++)
        {
            var point = Quaternion.AngleAxis(angle, Vector3.forward) * lastPoint;

            Debug.DrawLine(point + center, lastPoint + center, color);

            lastPoint = point;
        }
    }

    public static void DrawX (Vector2 point, float size, Color color)
    {
        var min = -Vector2.one * size;
        var max = -min;

        Debug.DrawLine(min+ point, max + point, color);

        min = Vector2.Perpendicular(min);
        max = Vector2.Perpendicular(max);

        Debug.DrawLine(min + point, max + point, color);
    }
}
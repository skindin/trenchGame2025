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
    public int pooledCount, activeCount, totalCount;
    List<(int, int)> intTuples = new();

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
            newTrench.lineMesh.NewMesh(0, 0);
        }

        //Chunk.manager.UnassignChunks(newTrench);

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

    //public void FillTrenches (Vector2 pos, float width)
    //{
    //    var boxDelta = Vector2.one * width / 2;
    //    var boxMin = pos - boxDelta;
    //    var boxMax = pos + boxDelta;

    //    chunkFillList.Clear();

    //    Chunk.manager.ChunksFromBox(boxMin,boxMax,chunkFillList,false,true);

    //    if (debugLines) LineMesh.DrawBox(boxMin, boxMax, Color.magenta);

    //    for (int chunkI = 0; chunkI < chunkFillList.Count; chunkI++)
    //    {
    //        var chunk = chunkFillList[chunkI];

    //        //if (debugLines) Chunk.manager.DrawChunk(chunk, Color.green); //orange heheh

    //        for (int trenchI = 0; trenchI < chunk.trenches.Count; trenchI++)
    //        {
    //            var trench = chunk.trenches[trenchI];

    //            if (debugLines) trench.lineMesh.DrawMeshBox();

    //            if (!trench.lineMesh.TestBoxOverlap(boxMin, boxMax))
    //            {
    //                continue;
    //            }

    //            if(debugLines) DrawCircle(pos, width/2, Color.cyan);

    //            Vector2 lastPoint = Vector2.zero;
    //            bool lastWasWthin = false;
    //            bool wasInnerPoint = false;
    //            //inner meaning right after or right before the first or last within the circle

    //            intTuples.Clear();

    //            for (int pointI = 0; pointI < trench.lineMesh.points.Count; pointI++)
    //            {
    //                var point = trench.lineMesh.points[pointI];

    //                if (pointI > 0 && debugLines) Debug.DrawLine(point, lastPoint, Color.black);

    //                var dist = Vector2.Distance(pos, point);
    //                var withinDist = dist < width / 2;

    //                if (withinDist && !lastWasWthin)
    //                {
    //                    if (pointI > 0)
    //                    {
    //                        var intersectPoint = GetCircleLineIntersection(pos, width/2, point, lastPoint);
    //                        trench.lineMesh.points[pointI] = intersectPoint;
    //                        RecalculateTrench(trench);
    //                    }
    //                    else if (trench.lineMesh.points.Count == 1)
    //                    {
    //                        intTuples.Add((0, 1));
    //                    }
    //                }
    //                else if (withinDist && (lastWasWthin || pointI == 0))
    //                {
    //                    if (!wasInnerPoint)
    //                    {
    //                        intTuples.Add((pointI, 1));
    //                        wasInnerPoint = true;
    //                    }
    //                    else
    //                    {
    //                        var tuple = intTuples[^1];
    //                        intTuples[^1] = new(tuple.Item1, tuple.Item2 + 1);
    //                    }
    //                }
    //                else if (!withinDist && lastWasWthin)
    //                {
    //                    var intersectPoint = GetCircleLineIntersection(pos, width/2, point, lastPoint);

    //                    if (pointI == 1)
    //                    {
    //                        trench.lineMesh.points[0] = intersectPoint;
    //                    }
    //                    else if (wasInnerPoint)
    //                    {
    //                        var tuple = intTuples[^1];
    //                        intTuples[^1] = new(tuple.Item1, tuple.Item2 - 1);
    //                        trench.lineMesh.points[pointI-1] = intersectPoint;
    //                    }
    //                    else //one point dipped inside
    //                    {
    //                        var newTrench = trench.SplitAtPoint(pointI, NewTrench());
    //                        newTrench.lineMesh.points[^1] = intersectPoint;
    //                        //zero because it's first of new trench
    //                        //RecalculateTrench(newTrench);
    //                        RecalculateTrench(newTrench);
    //                        pointI = 1;

    //                    }

    //                    wasInnerPoint = false;

    //                    RecalculateTrench(trench);
    //                }

    //                if (pointI == trench.lineMesh.points.Count-1)
    //                {
    //                    DrawX(point, .5f, Color.red);
    //                    break;
    //                }

    //                lastWasWthin = withinDist;
    //                lastPoint = point;//must be after anything accessing lastPoint
    //            }

    //            var indexDelta = 0;

    //            foreach (var tuple in intTuples)
    //            {
    //                if (tuple.Item2 == 0) continue;

    //                Trench newTrench = null;

    //                var index = tuple.Item1 - (indexDelta);

    //                var count = tuple.Item2;

    //                if (index > 0)
    //                {
    //                    newTrench = NewTrench();
    //                }

    //                trench.SplitAtPoints(index, count, newTrench);

    //                if (newTrench != null)
    //                {
    //                    RecalculateTrench(newTrench);
    //                    //if (trench.lineMesh.mesh.triangles.Length == 0)
    //                    //{
    //                    //    var bruh = false;
    //                    //}

    //                    //Debug.Log(newTrench.lineMesh.points.Count);

    //                    if (newTrench.lineMesh.mesh.vertices.Length == 0) Debug.Log("bruh no vertices");
    //                    if (newTrench.lineMesh.mesh.triangles.Length == 0) Debug.Log("bruh no triangles");
    //                    if (newTrench.chunks.Count == 0) Debug.Log("bruh trench has no chunks");
    //                    if (newTrench.chunks.Find(x => x.trenches.Contains(newTrench)) == null)
    //                        Debug.Log("bruh trench's chunk doesn't contain trench");
    //                }

    //                if (trench.lineMesh.points.Count == 0)
    //                {
    //                    RemoveTrench(trench);
    //                    var currentChunkI = Chunk.manager.chunks.IndexOf(chunk);

    //                    chunkI = Mathf.Min(chunkI, currentChunkI);
    //                }
    //                else
    //                {
    //                    RecalculateTrench(trench);
    //                    //if (trench.lineMesh.mesh.triangles.Length == 0)
    //                    //{
    //                    //    var bruh = false;
    //                    //}
    //                }

    //                indexDelta += index + count;
    //            }
    //        }
    //    }
    //}

    public void FillTrenches(Vector2 pos, float width)
    {
        var boxDelta = Vector2.one * width / 2;
        var boxMin = pos - boxDelta;
        var boxMax = pos + boxDelta;

        chunkFillList.Clear();

        Chunk.manager.ChunksFromBox(boxMin, boxMax, chunkFillList, false, debugLines);

        if (debugLines) GeoFuncs.DrawBox(boxMin, boxMax, Color.magenta);

        for (int chunkI = 0; chunkI < chunkFillList.Count; chunkI++)
        {
            var chunk = chunkFillList[chunkI];

            //if (debugLines) Chunk.manager.DrawChunk(chunk, Color.green); //orange heheh

            for (int trenchI = 0; trenchI < chunk.trenches.Count; trenchI++)
            {
                var trench = chunk.trenches[trenchI];
                var points = trench.lineMesh.points;

                if (debugLines) trench.lineMesh.DrawMeshBox();

                if (!trench.lineMesh.TestBoxOverlap(boxMin, boxMax))
                {
                    continue;
                }

                if (debugLines) GeoFuncs.DrawCircle(pos, width / 2, Color.cyan);

                Vector2 prevPoint = Vector2.zero;
                Vector2 currentPoint = Vector2.zero;
                Vector2 nextPoint = Vector2.zero;
                bool prevIsWithin = false;
                bool nextIsWithin = false;
                bool lastWasInner = false;

                intTuples.Clear();

                for (int pointI = 0; pointI < points.Count; pointI++)
                {
                    bool isFirst = pointI == 0;
                    bool isLast = pointI + 1 == points.Count;
                    bool isWithin;

                    if (isFirst)
                    {
                        currentPoint = points[0];
                        isWithin = Vector2.Distance(currentPoint, pos) < width / 2;
                    }
                    else
                    {
                        currentPoint = nextPoint;
                        isWithin = nextIsWithin;
                        if (debugLines) Debug.DrawLine(prevPoint, currentPoint, Color.black);
                    }

                    if (!isLast) //if this isn't the last point
                    {
                        nextPoint = points[pointI + 1];
                        nextIsWithin = Vector2.Distance(nextPoint, pos) < width / 2;
                    }
                    else
                    {
                        if (debugLines) GeoFuncs.DrawX(currentPoint, .5f, Color.red);
                    }

                    //gotta find where to turn lastWasInner off

                    if (!(prevIsWithin && isFirst) && isWithin && !nextIsWithin)
                    {
                        var newTrench = trench.SplitAtPoint(pointI, NewTrench());
                        pointI = 0;
                    }
                    else if ((prevIsWithin || isFirst) && isWithin && (nextIsWithin || isLast))
                    {
                        if (lastWasInner)
                        {
                            var tuple = intTuples[^1];
                            var countDelta = points.Count - pointI;
                            intTuples[^1] = (countDelta, tuple.Item2 + 1);
                        }
                        else
                        {
                            intTuples.Add((pointI, 1));
                            lastWasInner = true;
                        }
                    }

                    prevPoint = currentPoint;
                    prevIsWithin = isWithin;
                }

                foreach (var tuple in intTuples)
                {
                    if (tuple.Item2 == 0) continue;

                    Trench newTrench = null;

                    var index = points.Count - tuple.Item1;

                    var count = tuple.Item2;

                    if (index > 0)
                    {
                        newTrench = NewTrench();
                    }

                    trench.SplitAtPoints(index, count, newTrench);

                    if (newTrench != null)
                    {
                        RecalculateTrench(newTrench);
                        //if (trench.lineMesh.mesh.triangles.Length == 0)
                        //{
                        //    var bruh = false;
                        //}

                        //Debug.Log(newTrench.lineMesh.points.Count);

                        if (newTrench.lineMesh.mesh.vertices.Length == 0) Debug.Log("bruh no vertices");
                        if (newTrench.lineMesh.mesh.triangles.Length == 0) Debug.Log("bruh no triangles");
                        if (newTrench.chunks.Count == 0) Debug.Log("bruh trench has no chunks");
                        if (newTrench.chunks.Find(x => x.trenches.Contains(newTrench)) == null)
                            Debug.Log("bruh trench's chunk doesn't contain trench");
                    }

                    if (trench.lineMesh.points.Count == 0)
                    {
                        RemoveTrench(trench);
                        var currentChunkI = Chunk.manager.chunks.IndexOf(chunk);

                        chunkI = Mathf.Min(chunkI, currentChunkI); //BRUH THIS CAN'T BE GOOD
                    }
                    else
                    {
                        RecalculateTrench(trench);
                        //if (trench.lineMesh.mesh.triangles.Length == 0)
                        //{
                        //    var bruh = false;
                        //}
                    }
                }
            }
        }
    }

    public Trench TestChunkTrenches (Vector2 pos, float radius, Chunk chunk)
    {
        foreach (var trench in chunk.trenches)
        {
            if (TestTrench(pos, radius, trench))
            {
                return trench;
            }
        }

        return null;
    }

    public Trench TestAllTrenches (Vector2 pos, float radius)
    {
        var chunk = Chunk.manager.ChunkFromPos(pos,false,debugLines);

        if (chunk == null) return null;

        TestChunkTrenches(pos, radius, chunk);

        return null;
    }

    public bool TestTrench (Vector2 pos, float radius, Trench trench)
    {
        return trench.lineMesh.TestBoxWithPoint(pos, debugLines) && trench.TestWithin(pos, radius, debugLines);
    }

    public void RegenerateMesh (Trench trench)
    {
        trench.lineMesh.NewMesh(endRes, cornerRes, debugLines);
    }

    public void RecalculateTrench (Trench trench)
    {
        RegenerateMesh(trench);
        //trench.lineMesh.CalculateBox();
        Chunk.manager.AutoAssignChunks(trench);
    }

    List<Chunk> chunkList = new();

    public Vector2 FindTrenchEdgeFromOutside(Vector2 a, Vector2 b)
    {
        chunkList.Clear();
        Chunk.manager.ChunksFromLine(a, b, chunkList, false, debugLines);

        var delta = b - a;

        var closestPoint = b;
        float closestDist = delta.magnitude;

        foreach (var chunk in chunkList)
        {
            foreach (var trench in chunk.trenches)
            {
                var lineMesh = trench.lineMesh;
                var bounds = lineMesh.mesh.bounds;

                if (!GeoFuncs.DoesLineIntersectBox(a, b, bounds.min, bounds.max, debugLines)) continue;

                var points = lineMesh.points;

                Vector2 lastPoint = Vector2.zero;

                for (int i = 0; i < points.Count; i++)
                {
                    var point = points[i];

                    if (i > 0)
                    {
                        if (debugLines) Debug.DrawLine(lastPoint, point, Color.black);

                        var segDelta = lastPoint - point;

                        var edgeDelta = lineMesh.width / 2 * Vector2.Perpendicular(segDelta).normalized;

                        for (int l = -1; l <= 1; l+=2)
                        {
                            var sideDelta = l * edgeDelta;

                            var lastPointEdge = lastPoint + sideDelta;
                            var thisPointEdge = point + sideDelta;

                            if (debugLines) Debug.DrawLine(lastPointEdge, thisPointEdge, Color.grey);

                            var intersection = GeoFuncs.FindIntersection(a, b, lastPointEdge, thisPointEdge);
                            if (intersection.x != Mathf.Infinity)
                            {
                                //intersection += lineMesh.width / 2 * backWardOne;
                                if (debugLines) GeoFuncs.DrawX(intersection, .5f, Color.magenta);

                                var distance = (intersection - a).magnitude;

                                if (distance < closestDist)
                                {
                                    closestDist = distance;
                                    closestPoint = intersection;
                                }
                                //if ()
                                //return intersection;
                            }
                        }
                    }

                    if (debugLines) GeoFuncs.DrawCircle(point, lineMesh.width / 2, Color.grey);
                    var circleIntersection = GeoFuncs.GetCircleLineIntersection(point, lineMesh.width / 2, b, a);
                    if (circleIntersection.x != Mathf.Infinity)
                    {
                        if (debugLines) GeoFuncs.DrawX(circleIntersection, .5f, Color.magenta);

                        var distance = (circleIntersection - a).magnitude;

                        if (distance < closestDist)
                        {
                            closestDist = distance;
                            closestPoint = circleIntersection;
                        }
                    }

                    lastPoint = point;
                }
            }

            //Chunk.manager.DrawChunk(chunk, Color.black);

        }

        GeoFuncs.DrawX(closestPoint, 1, Color.green);

        return closestPoint;
    }

    public Vector2 FindTrenchEdgeFromInside(Vector2 a, Vector2 b)
    {
        var furthestPoint = GetFirstPointWithinRange(a);
        if (furthestPoint.x == Mathf.Infinity) return b;
        float furthesDist = 0;

        chunkList.Clear();
        Chunk.manager.ChunksFromLine(a, b, chunkList, false, debugLines);

        var delta = b - a;

        foreach (var chunk in chunkList)
        {
            var chunkPos = Chunk.manager.GetRealChunkPos(chunk.coords);

            var dist = (chunkPos - a).magnitude;

            if (dist < furthesDist) continue;

            foreach (var trench in chunk.trenches)
            {
                var lineMesh = trench.lineMesh;
                var bounds = lineMesh.mesh.bounds;

                if (!GeoFuncs.DoesLineIntersectBox(a, b, bounds.min, bounds.max, debugLines)) continue;

                var points = lineMesh.points;

                Vector2 lastPoint = Vector2.zero;

                for (int i = 0; i < points.Count; i++)
                {
                    var point = points[i];

                    //if (i > 0)
                    //{
                    //    if (debugLines) Debug.DrawLine(lastPoint, point, Color.black);

                    //    var segDelta = lastPoint - point;

                    //    var edgeDelta = lineMesh.width / 2 * Vector2.Perpendicular(segDelta).normalized;

                    //    for (int l = -1; l <= 1; l += 2)
                    //    {
                    //        var sideDelta = l * edgeDelta;

                    //        var lastPointEdge = lastPoint + sideDelta;
                    //        var thisPointEdge = point + sideDelta;

                    //        if (debugLines) Debug.DrawLine(lastPointEdge, thisPointEdge, Color.grey);

                    //        var intersection = GeoFuncs.FindIntersection(a, b, lastPointEdge, thisPointEdge);
                    //        if (intersection.x != Mathf.Infinity)
                    //        {
                    //            //intersection += lineMesh.width / 2 * backWardOne;
                    //            if (debugLines) GeoFuncs.DrawX(intersection, .5f, Color.magenta);

                    //            var distance = (intersection - a).magnitude;

                    //            if (distance < closestDist)
                    //            {
                    //                closestDist = distance;
                    //                closestPoint = intersection;
                    //            }
                    //            //if ()
                    //            //return intersection;
                    //        }
                    //    }
                    //}

                    if (debugLines) GeoFuncs.DrawCircle(point, lineMesh.width / 2, Color.grey);

                    var distFromClosest = (point - furthestPoint).magnitude;

                    if (distFromClosest < lineMesh.width / 2)
                    {

                        var circleIntersection = GeoFuncs.GetCircleLineIntersection(point, lineMesh.width / 2, a, b);
                        if (circleIntersection.x != Mathf.Infinity)
                        {
                            if (debugLines) GeoFuncs.DrawX(circleIntersection, .5f, Color.magenta);

                            var distance = (circleIntersection - a).magnitude;

                            if (distance > furthesDist)
                            {
                                furthesDist = distance;
                                furthestPoint = circleIntersection;
                            } //gonna have to make a list of points, AND THEN test if they are in range... crying emoji
                        }
                    }

                    lastPoint = point;
                }
            }

            //Chunk.manager.DrawChunk(chunk, Color.black);

        }

        GeoFuncs.DrawX(furthestPoint, 1, Color.green);

        return furthestPoint;
    }


    /// <summary>
    /// Returns the first trench point within range. Returns Vector2.positiveInfinity if there are no trench points
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public Vector2 GetFirstPointWithinRange (Vector2 pos)
    {
        var chunk = Chunk.manager.ChunkFromPos(pos,false,debugLines);

        if (chunk == null) return Vector2.positiveInfinity;

        foreach (var trench in chunk.trenches)
        {
            var point = trench.GetFirstInRangePoint(pos, 0, debugLines);
            if (point.x != Mathf.Infinity) return point;
        }

        return Vector2.positiveInfinity;
    }

    /// <summary>
    /// Returns true if trench area more than max area
    /// </summary>
    /// <returns></returns>
    public bool TrenchExceedsMaxArea (Trench trench)
    {
        return trench.lineMesh.area > maxTrenchArea;
    }
}
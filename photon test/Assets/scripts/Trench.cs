using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[System.Serializable]
public class Trench
{
    public static TrenchManager manager;

    public TrenchDigger digger;
    public LineMesh lineMesh = new();
    public List<Chunk> chunks = new();

    public void AddPoint (Vector2 newPoint) //MOVE ALL THIS BOUNDARY TESTING TO TRENCH DIGGER
    {
        var prevEmpty = lineMesh.points.Count == 0;

        //if (lineMesh.points.Count > 2)
        //{
        //    var a = true;
        //}

        lineMesh.AddPoint(newPoint, lineMesh.points.Count);

        // detects if new box inherits any more chunks
        if (prevEmpty)
        {
            Chunk.manager.AutoAssignChunks(this);
        }
    }

    /// <summary>
    /// returns a new trench with points BEOFRE index, removes THIS trench to only have lines AFTER index
    /// </summary>
    /// <param name="index"></param>
    /// <param name="newTrench"></param>
    /// <param name="includeIndex"></param>
    /// <returns></returns>
    public Trench SplitAtPoint (int index, Trench newTrench = null, bool includeIndex = true)
    {
        if (newTrench == null)
        {
            newTrench = new();
        }

        newTrench.lineMesh.width = lineMesh.width;

        var newPointCount = index;
        if (!includeIndex) newPointCount--;

        for (var i = 0; i < newPointCount; i++)
        {
            var point = lineMesh.points[i];
            newTrench.lineMesh.AddPoint(point,i);
        }

        var oldPointCount = lineMesh.points.Count - index;
        if (!includeIndex) oldPointCount--;
        oldPointCount = Mathf.Max(oldPointCount, 0);

        while (lineMesh.points.Count>oldPointCount)
        {
            lineMesh.points.RemoveAt(0);
        }

        //if (newTrench.lineMesh.points.Count == 0)
        //{
        //    Debug.Log("New mesh had zero points");
        //}

        return newTrench;
    }

    /// <summary>
    /// Requires newTrench to not be null to keep first half
    /// </summary>
    /// <param name="index"></param>
    /// <param name="count"></param>
    /// <param name="newTrench"></param>
    /// <returns></returns>
    public Trench SplitAtPoints (int index, int count, Trench newTrench)
    {
        var maxIndex = lineMesh.points.Count - 1;

        if (index > lineMesh.points.Count - 1)
        {
            Debug.Log($"Index {index} is higher than max index {maxIndex}");
            return newTrench;
        }

        var maxCount = lineMesh.points.Count - index;

        if (count > maxCount)
        {
            Debug.Log($"Count {count} is higher than max count {maxCount}");
            return newTrench;
        }

        //if (newTrench == null) newTrench = new();
        if (newTrench != null)
        {
            var newPointCount = index;

            for (var i = 0; i < newPointCount; i++)
            {
                var point = lineMesh.points[i];
                newTrench.lineMesh.points.Add(point);
            }

            newTrench.lineMesh.width = lineMesh.width;
        }

        var oldPointCount = lineMesh.points.Count - (count + index);

        oldPointCount = Mathf.Max(oldPointCount, 0);

        while (lineMesh.points.Count > oldPointCount)
        {
            lineMesh.points.RemoveAt(0);
        }

        return newTrench;
    }

    public bool TestWithin(Vector2 pos, float radius, bool debugLines = false)
    {
        var lastPoint = Vector2.zero;
        var closestDist = Mathf.Infinity;
        var closestPoint = Vector2.zero;

        for (int i = 0; i < lineMesh.points.Count; i++)
        {
            var point = lineMesh.points[i];

            if (i > 0)
            {
                if (debugLines) Debug.DrawLine(point, lastPoint, Color.black);

                var closestSegPoint = GeoFuncs.ClosestPointToLineSegment(pos, lastPoint, point);

                var dist = Vector2.Distance(pos, closestSegPoint) + radius;
                GeoFuncs.DrawCircle(pos, radius, Color.green);
                if (dist <= lineMesh.width / 2)
                {
                    if (debugLines) Debug.DrawLine(pos, closestSegPoint, Color.green);
                    return true;
                }
                if (dist < closestDist && debugLines)
                {
                    closestDist = dist;
                    closestPoint = closestSegPoint;
                }


            }
            else if (lineMesh.points.Count == 1)
            {
                closestPoint = lineMesh.points[0];
                closestDist = Vector2.Distance(pos, closestPoint);
                if (closestDist <= lineMesh.width / 2)
                {
                    if (debugLines) Debug.DrawLine(pos, closestPoint, Color.green);
                    return true;
                }
            }

            lastPoint = point;
        }

        if (debugLines) Debug.DrawLine(pos, closestPoint, Color.red);
        return false;
    }

    public void OnRemove()
    {
        lineMesh.Reset();
        Chunk.manager.UnassignChunks(this,true);
    }
}

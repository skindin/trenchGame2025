using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
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
    public Trench Split (int index, Trench newTrench = null, bool includeIndex = true)
    {
        if (newTrench == null)
        {
            newTrench = new();
        }

        newTrench.lineMesh.width = lineMesh.width;

        var newPointCount = index;
        if (includeIndex) newPointCount++;

        for (var i = 0; i < newPointCount; i++)
        {
            var point = lineMesh.points[i];
            newTrench.lineMesh.AddPoint(point,i);
        }

        var oldPointCount = lineMesh.points.Count - index;
        if (includeIndex) oldPointCount++;

        while (lineMesh.points.Count>oldPointCount)
        {
            lineMesh.points.RemoveAt(0);
        }

        return newTrench;
    }

    public bool TestWithin(Vector2 pos, bool debugLines = false)
    {
        var lastPoint = Vector2.zero;
        var closestDist = Mathf.Infinity;
        var closestPoint = Vector2.zero;

        for (int i = 0; i < lineMesh.points.Count; i++)
        {
            var point = lineMesh.points[i];

            if (i > 0)
            {
                if (debugLines) Debug.DrawLine(point, lastPoint, Color.yellow);

                var closestSegPoint = ClosestPointToLineSegment(pos, lastPoint, point);

                var dist = Vector2.Distance(pos, closestSegPoint);
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

            lastPoint = point;
        }

        if (debugLines) Debug.DrawLine(pos, closestPoint, Color.red);
        return false;
    }

    Vector2 ClosestPointToLineSegment(Vector2 objectPos, Vector2 lineStart, Vector2 lineEnd)
    {

        if (lineStart == lineEnd) return lineStart;
        // Calculate the squared length of the line segment
        float lineLengthSquared = Mathf.Pow(lineEnd.x - lineStart.x, 2) + Mathf.Pow(lineEnd.y - lineStart.y, 2);

        // Calculate the parameter (t) of the closest point to the line segment
        float t = Mathf.Max(0, Mathf.Min(1, Vector2.Dot(objectPos - lineStart, lineEnd - lineStart) / lineLengthSquared));

        // Calculate the closest point on the line segment
        Vector2 closestPoint = lineStart + t * (lineEnd - lineStart);

        return closestPoint;
    }
}

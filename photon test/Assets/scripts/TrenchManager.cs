using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrenchManager : MonoBehaviour
{
    public static TrenchManager instance;

    public List<LineRenderer> trenchLines;

    private void Awake()
    {
        if (!instance) instance = this;
    }

    private void Update()
    {
        if (!instance) instance = this;
    }

    public bool CheckWithinTrench (Vector2 pos)
    {
        foreach (var line in trenchLines)
        {
            if (line.positionCount == 0)
            {
                break;
            }

            if (line.positionCount == 1)
            {
                var pointA = line.GetPosition(0);
                var dist = Vector2.Distance(pointA, pos);
                if (dist <= line.widthMultiplier/2)
                {
                    return true;
                }
            }

            if (line.positionCount > 1)
            {
                FindClosestEdge(pos, line,  out var pointA, out var pointB);

                var closestPoint = ClosestPointToLineSegment(pos, pointA, pointB);

                var distFromLine = Vector2.Distance(closestPoint, pos);

                var withinTrench = true;
                Color rayColor = Color.green;

                if (distFromLine > line.widthMultiplier/2)
                {
                    withinTrench = false;
                    rayColor = Color.red;
                }

                Debug.DrawLine(pos, closestPoint, rayColor);

                return withinTrench;
            }
        }

        return false;
    }

    void FindClosestEdge(Vector3 point, LineRenderer line, out Vector3 pointA, out Vector3 pointB)
    {
        pointA = Vector3.zero;
        pointB = Vector3.zero;

        // Get the positions of all points defining the line

        float minDist = Mathf.Infinity;
        for (int i = 0; i < line.positionCount; i++)
        {
            var linePoint = line.GetPosition(i)+line.transform.position;

            float distance = Vector2.Distance(linePoint, point);

            if (distance < minDist)
            {
                minDist = distance;
                pointA = linePoint;
                if (i != 0)
                {
                    pointB = line.GetPosition(i - 1) + line.transform.position;
                }
                else if (i < line.positionCount-1)
                {
                    pointB = line.GetPosition(i + 1) + line.transform.position;
                }
            }

            if (i + 1 < line.positionCount)
                Debug.DrawLine(linePoint, line.GetPosition(i + 1)+line.transform.position);
        }

        Debug.DrawLine(pointA, pointB, Color.blue);
    }

    Vector2 ClosestPointToLineSegment(Vector2 objectPos, Vector2 lineStart, Vector2 lineEnd)
    {
        // Calculate the squared length of the line segment
        float lineLengthSquared = Mathf.Pow(lineEnd.x - lineStart.x, 2) + Mathf.Pow(lineEnd.y - lineStart.y, 2);

        // Calculate the parameter (t) of the closest point to the line segment
        float t = Mathf.Max(0, Mathf.Min(1, Vector2.Dot(objectPos - lineStart, lineEnd - lineStart) / lineLengthSquared));

        // Calculate the closest point on the line segment
        Vector2 closestPoint = lineStart + t * (lineEnd - lineStart);

        return closestPoint;
    }
}

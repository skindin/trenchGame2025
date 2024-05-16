using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GeoFuncs
{
    public static Vector2 ClosestPointToLineSegment(Vector2 objectPos, Vector2 lineStart, Vector2 lineEnd)
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

    /// <summary>
    /// Used for line between two points
    /// </summary>
    /// <param name="circleCenter"></param>
    /// <param name="circleRadius"></param>
    /// <param name="lineStart"></param>
    /// <param name="lineEnd"></param>
    /// <returns></returns>

    public static bool DoesLineIntersectCircle (Vector2 circleCenter, float circleRadius, Vector2 a, Vector2 b, bool debugLines = false)
    {
        var closestPoint = ClosestPointToLineSegment(circleCenter, a, b);
        var dist = (closestPoint - circleCenter).magnitude;
        var doesIntersect = dist <= circleRadius;
        Color lineColor;
        if (debugLines)
        {
            if (doesIntersect) lineColor = Color.green;
            else lineColor = Color.red;

            Debug.DrawLine(closestPoint, circleCenter, lineColor);
        }
        return doesIntersect;
    }

    public static Vector2 GetCircleLineIntersection(Vector2 circleCenter, float circleRadius, Vector2 lineStart, Vector2 lineEnd)
    {
        // Vector from start to end of the line segment
        Vector2 lineVec = lineEnd - lineStart;

        // Vector from circle center to line start
        Vector2 circleToStart = lineStart - circleCenter;

        // Calculate the coefficients of the quadratic equation
        float a = lineVec.sqrMagnitude;
        float b = 2f * Vector2.Dot(lineVec, circleToStart);
        float c = circleToStart.sqrMagnitude - circleRadius * circleRadius;

        // Calculate the discriminant of the quadratic equation
        float discriminant = b * b - 4 * a * c;

        // If the discriminant is negative, there are no intersections
        if (discriminant < 0)
        {
            // Return Vector2.positiveInfinity to indicate no intersection
            return Vector2.positiveInfinity;
        }

        // Calculate the two possible intersection points
        float t1 = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
        float t2 = (-b - Mathf.Sqrt(discriminant)) / (2 * a);

        // Check if the intersection points are within the line segment bounds
        if (t1 >= 0 && t1 <= 1)
        {
            return lineStart + t1 * lineVec;
        }
        else if (t2 >= 0 && t2 <= 1)
        {
            return lineStart + t2 * lineVec;
        }

        // If neither intersection point is within the line segment bounds, return Vector2.positiveInfinity
        return Vector2.positiveInfinity;
    }

    public static bool DoLinesIntersect(Vector2 pointA, Vector2 pointB, Vector2 pointC, Vector2 pointD, bool debugLines = false)
    {
        // Direction vectors
        Vector2 line1Dir = pointB - pointA;
        Vector2 line2Dir = pointD - pointC;

        float denominator = (line1Dir.x * line2Dir.y) - (line1Dir.y * line2Dir.x);

        if (debugLines)
        {
            //Debug.DrawLine(pointA, pointB, Color.cyan);
            Debug.DrawLine(pointC, pointD, new Color(.25f, 0, 1));
        }

        if (denominator == 0)
        {
            // Lines are parallel
            return false;
        }

        Vector2 pointDiff = pointC - pointA;
        float t = ((pointDiff.x * line2Dir.y) - (pointDiff.y * line2Dir.x)) / denominator;
        float u = ((pointDiff.x * line1Dir.y) - (pointDiff.y * line1Dir.x)) / denominator;

        if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
        {
            // Intersection point lies within the line segments
            return true;
        }

        return false;
    }

    public static bool DoesLineIntersectBox (Vector2 pointA, Vector2 pointB, Vector2 boxMin, Vector2 boxMax, bool debugLines = false)
    {
        if (TestBox(boxMin, boxMax, pointA) || TestBox(boxMin, boxMax, pointA))
        {
            GeoFuncs.DrawBox(boxMin, boxMax, Color.blue);
            return true;
        }

        GetTopLeftAndBottomRight(boxMin, boxMax, out var topLeft, out var bottomRight);

        if (DoLinesIntersect(pointA, pointB, boxMin, boxMax, debugLines))
        {
            return true;
        }

        if (DoLinesIntersect(pointA, pointB, topLeft, bottomRight, debugLines))
        {
            return true;
        }

        //if (DoLinesIntersect(pointA, pointB, boxMax, bottomRight, debugLines))
        //{
        //    return true;
        //}

        //if (DoLinesIntersect(pointA, pointB, bottomRight, boxMin, debugLines))
        //{
        //    return true;
        //}

        //realized there is no scenario where you would have to test all four lines if you're already testing if it's within

        return false;
    }

    public static Vector2 FindIntersection(Vector2 line1Start, Vector2 line1End, Vector2 line2Start, Vector2 line2End)
    {
        float s1_x, s1_y, s2_x, s2_y;
        s1_x = line1End.x - line1Start.x;
        s1_y = line1End.y - line1Start.y;
        s2_x = line2End.x - line2Start.x;
        s2_y = line2End.y - line2Start.y;

        float s, t;
        s = (-s1_y * (line1Start.x - line2Start.x) + s1_x * (line1Start.y - line2Start.y)) / (-s2_x * s1_y + s1_x * s2_y);
        t = (s2_x * (line1Start.y - line2Start.y) - s2_y * (line1Start.x - line2Start.x)) / (-s2_x * s1_y + s1_x * s2_y);

        if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
        {
            // Intersection detected
            return new Vector2(line1Start.x + (t * s1_x), line1Start.y + (t * s1_y));
        }

        // No intersection
        return Vector2.positiveInfinity;
    }

    public static bool TestBox(Vector2 min, Vector2 max, Vector2 point)
    {
        if (point.x < min.x ||
        point.y < min.y ||
        point.x > max.x ||
        point.y > max.y)
        {
            return false;
        }

        return true;
    }

    public static void DrawCircle(Vector3 center, float radius, Color color, int res = 4)
    {
        Vector3 lastPoint = Vector2.up * radius;

        int verts = res * 4;
        var angle = 360f / verts;

        for (int i = 1; i < verts + 1; i++)
        {
            var point = Quaternion.AngleAxis(angle, Vector3.forward) * lastPoint;

            Debug.DrawLine(point + center, lastPoint + center, color);

            lastPoint = point;
        }
    }

    public static void DrawX(Vector2 point, float size, Color color)
    {
        var min = -Vector2.one * size;
        var max = -min;

        Debug.DrawLine(min + point, max + point, color);

        min = Vector2.Perpendicular(min);
        max = Vector2.Perpendicular(max);

        Debug.DrawLine(min + point, max + point, color);
    }

    public static void DrawBox(Vector2 min, Vector2 max, Color color)
    {
        GetTopLeftAndBottomRight(min, max, out var topLeft, out var bottomRight);
        Debug.DrawLine(min, topLeft, color);
        Debug.DrawLine(topLeft, max, color);
        Debug.DrawLine(max, bottomRight, color);
        Debug.DrawLine(bottomRight, min, color);
    }

    public static void GetTopLeftAndBottomRight(Vector2 min, Vector2 max, out Vector2 topLeft, out Vector2 bottomRight)
    {
        topLeft = new(min.x, max.y);
        bottomRight = new(max.x, min.y);
    }
}

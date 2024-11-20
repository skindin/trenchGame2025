using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public static class GeoUtils
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

    public static Vector2 ClosestPointToLineSegment(Vector2 objectPos, Vector2 lineStart, Vector2 lineEnd, out float ratio)
    {

        if (lineStart == lineEnd)
        {
            ratio = 0;
            return lineStart;
        }
        // Calculate the squared length of the line segment
        float lineLengthSquared = Mathf.Pow(lineEnd.x - lineStart.x, 2) + Mathf.Pow(lineEnd.y - lineStart.y, 2);

        // Calculate the parameter (t) of the closest point to the line segment
        ratio = Mathf.Max(0, Mathf.Min(1, Vector2.Dot(objectPos - lineStart, lineEnd - lineStart) / lineLengthSquared));

        // Calculate the closest point on the line segment
        Vector2 closestPoint = lineStart + ratio * (lineEnd - lineStart);

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
            DrawCircle(circleCenter, circleRadius, lineColor);
        }
        
        return doesIntersect;
    }

    /// <summary>
    /// Returns the last point of a circle the line would touch. apparently this function doesn't even work
    /// </summary>
    /// <param name="circleCenter"></param>
    /// <param name="circleRadius"></param>
    /// <param name="lineStart"></param>
    /// <param name="lineEnd"></param>
    /// <returns></returns>

    //public static Vector2 GetCircleLineIntersection(Vector2 circleCenter, float circleRadius, Vector2 lineStart, Vector2 lineEnd)
    //{
    //    if (Vector2.Distance(circleCenter, lineStart) <= circleRadius && Vector2.Distance(circleCenter, lineEnd) <= circleRadius) return lineStart;

    //    // Vector from start to end of the line segment
    //    Vector2 lineVec = lineEnd - lineStart;

    //    // Vector from circle center to line start
    //    Vector2 circleToStart = lineStart - circleCenter;

    //    // Calculate the coefficients of the quadratic equation
    //    float a = lineVec.sqrMagnitude;
    //    float b = 2f * Vector2.Dot(lineVec, circleToStart);
    //    float c = circleToStart.sqrMagnitude - circleRadius * circleRadius;

    //    // Calculate the discriminant of the quadratic equation
    //    float discriminant = b * b - 4 * a * c;

    //    // If the discriminant is negative, there are no intersections
    //    if (discriminant < 0)
    //    {
    //        // Return Vector2.positiveInfinity to indicate no intersection
    //        return Vector2.positiveInfinity;
    //    }

    //    // Calculate the two possible intersection points
    //    float t1 = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
    //    float t2 = (-b - Mathf.Sqrt(discriminant)) / (2 * a);

    //    // Check if the intersection points are within the line segment bounds
    //    if (t1 >= 0 && t1 <= 1)
    //    {
    //        return lineStart + t1 * lineVec;
    //    }
    //    else if (t2 >= 0 && t2 <= 1)
    //    {
    //        return lineStart + t2 * lineVec;
    //    }

    //    // If neither intersection point is within the line segment bounds, return Vector2.positiveInfinity
    //    return Vector2.positiveInfinity;
    //}

    public static Vector2 GetCircleLineIntersection(Vector2 circleCenter, float circleRadius, Vector2 lineStart, Vector2 lineEnd)
    {
        var defaultOutput = Vector2.positiveInfinity;

        if ((lineStart - circleCenter).magnitude <= circleRadius) return defaultOutput;

        Vector2 d = lineEnd - lineStart; // Direction vector of the line
        Vector2 f = lineStart - circleCenter; // Vector from circle center to line start

        float a = Vector2.Dot(d, d);
        float b = 2 * Vector2.Dot(f, d);
        float c = Vector2.Dot(f, f) - circleRadius * circleRadius;

        float discriminant = b * b - 4 * a * c;

        if (discriminant < 0)
        {
            // No intersection
            return defaultOutput;
        }
        else
        {
            // Intersection exists
            discriminant = Mathf.Sqrt(discriminant);

            float t1 = (-b - discriminant) / (2 * a);
            float t2 = (-b + discriminant) / (2 * a);

            // If you need only one intersection point, you can choose the smaller or larger t value
            // Here we are returning the first intersection point (t1)
            // You may need to add checks to ensure t1 and t2 are within the segment bounds (0 <= t <= 1)
            if (t1 >= 0 && t1 <= 1)
            {
                return lineStart + t1 * d;
            }

            if (t2 >= 0 && t2 <= 1)
            {
                return lineStart + t2 * d;
            }

            // If neither t1 nor t2 is within the segment, there's no intersection on the segment
            return defaultOutput;
        }
    }

    public static bool DoLinesIntersect(Vector2 pointA, Vector2 pointB, Vector2 pointC, Vector2 pointD, bool debugLines = false)
    {
        // Direction vectors
        Vector2 line1Dir = pointB - pointA;
        Vector2 line2Dir = pointD - pointC;

        float denominator = (line1Dir.x * line2Dir.y) - (line1Dir.y * line2Dir.x);

        //if (debugLines)
        //{
        //    //Debug.DrawLine(pointA, pointB, Color.cyan);
        //    Debug.DrawLine(pointC, pointD, new Color(.25f, 0, 1));
        //}

        if (denominator == 0)
        {
            // Lines are parallel
            if (debugLines) Debug.DrawLine(pointA, pointB, Color.red);
            return false;
        }

        Vector2 pointDiff = pointC - pointA;
        float t = ((pointDiff.x * line2Dir.y) - (pointDiff.y * line2Dir.x)) / denominator;
        float u = ((pointDiff.x * line1Dir.y) - (pointDiff.y * line1Dir.x)) / denominator;

        if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
        {
            // Intersection point lies within the line segments
            if (debugLines) Debug.DrawLine(pointC, pointD, Color.green);
            return true;
        }

        if (debugLines) Debug.DrawLine(pointC, pointD, Color.red);
        return false;
    }

    public static bool DoesLineIntersectBoxMinMax (Vector2 pointA, Vector2 pointB, Vector2 boxMin, Vector2 boxMax, bool debugLines = false)
    {
        if (debugLines) Debug.DrawLine(pointA, pointB, Color.green);

        if (TestBoxMinMax(boxMin, boxMax, pointA, debugLines) || TestBoxMinMax(boxMin, boxMax, pointB, debugLines))
        {
            if (debugLines) DrawBoxMinMax(boxMin, boxMax, Color.blue);
            return true;
        }

        GetTopLeftAndBottomRight(boxMin, boxMax, out var topLeft, out var bottomRight);

        return DoesLineIntersectX(pointA, pointB, boxMin, topLeft, boxMax, bottomRight, debugLines);

        //if (DoLinesIntersect(pointA, pointB, boxMax, bottomRight, debugLines))
        //{
        //    return true;
        //}

        //if (DoLinesIntersect(pointA, pointB, bottomRight, boxMin, debugLines))
        //{
        //    return true;
        //}

        //realized there is no scenario where you would have to test all four lines if you're already testing if it's within

    }

    public static bool DoesLineIntersectBoxPosSize (Vector2 pointA, Vector2 pointB, Vector2 boxPos, Vector2 boxSize, bool debugLines = false)
    {
        var edge = boxSize / 2;
        var direction = Vector2.one * edge;
        var min = boxPos - direction;
        var max = boxPos + direction;

        return DoesLineIntersectBoxMinMax(pointA, pointB, min, max, debugLines);
    }

    public static bool DoesLineIntersectX (Vector2 pointA, Vector2 pointB, Vector2 bottomLeft, Vector2 topLeft, Vector2 topRight, Vector2 bottomRight,
        bool debugLines = false)
    {

        if (DoLinesIntersect(pointA, pointB, topLeft, bottomRight, debugLines))
        {
            return true;
        }

        return DoLinesIntersect(pointA, pointB, bottomLeft, topRight, debugLines);
    }

    public static bool IsPointInTriangle(Vector2 point, Vector2 vertex1, Vector2 vertex2, Vector2 vertex3, bool debugLines = false)
    {
        // Compute vectors
        Vector2 v0 = vertex3 - vertex1;
        Vector2 v1 = vertex2 - vertex1;
        Vector2 v2 = point - vertex1;

        // Compute dot products
        float dot00 = Vector2.Dot(v0, v0);
        float dot01 = Vector2.Dot(v0, v1);
        float dot02 = Vector2.Dot(v0, v2);
        float dot11 = Vector2.Dot(v1, v1);
        float dot12 = Vector2.Dot(v1, v2);

        // Compute barycentric coordinates
        float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
        float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

        if (debugLines)
        {
            Color color = new(.5f, .8f, 1);
            Debug.DrawLine(vertex1, vertex2, color);
            Debug.DrawLine(vertex2, vertex3, color);
            Debug.DrawLine(vertex3, vertex1, color);
        }

        // Check if point is in triangle
        return (u >= 0) && (v >= 0) && (u + v < 1);
    }

    public static bool IsPointInQuad(Vector2 point, Vector2 vertex1, Vector2 vertex2, Vector2 vertex3, Vector2 vertex4, bool debugLines = false)
    {
        if (IsPointInTriangle(point, vertex1, vertex2, vertex3, debugLines)) return true;
        return IsPointInTriangle(point, vertex1, vertex4, vertex3, debugLines);
    }


    /// <summary>
    /// Returns true if the line overlaps the quad at all Input vertexes in order!
    /// </summary>
    /// <param name="pointA"></param>
    /// <param name="pointB"></param>
    /// <param name="vertex1"></param>
    /// <param name="vertex2"></param>
    /// <param name="vertex3"></param>
    /// <param name="vertex4"></param>
    /// <returns></returns>
    public static bool DoesLineIntersectQuad(Vector2 pointA, Vector2 pointB, Vector2 vertex1, Vector2 vertex2, Vector2 vertex3, Vector2 vertex4,
        bool debugLines = false)
    {
        if (
            IsPointInQuad(pointA, vertex1, vertex2, vertex3, vertex4, debugLines) ||
            IsPointInQuad(pointB, vertex1, vertex2, vertex3, vertex4, debugLines)
            ) return true;

        return DoesLineIntersectX(pointA, pointB, vertex1, vertex2, vertex3, vertex4, debugLines);
    }

    public static void GetQuadIntercepts(Vector2 pointA, Vector2 pointB, Vector2 vertex1, Vector2 vertex2, Vector2 vertex3, Vector2 vertex4,
        out Vector2 interceptA, out Vector2 interceptB, bool debugLines = false)
    {
        //interceptA = pointA;
        //interceptB = pointB;

        interceptA = Vector2.positiveInfinity;
        interceptB = Vector2.positiveInfinity;

        Vector2 firstFound = pointA;

        var interceptCount = 0;

        var intercept = FindIntersection(pointA, pointB, vertex1, vertex2, debugLines);
        if (intercept.x != Mathf.Infinity)
        {
            firstFound = intercept;
            interceptCount++;
        }
        
        intercept = FindIntersection(pointA, pointB, vertex3, vertex2, debugLines);
        if (intercept.x != Mathf.Infinity)
        {
            if (interceptCount == 1)
            {
                GetClosestAndFurthest(pointA, firstFound, intercept, out var closest, out var furthest);
                interceptA = closest;
                interceptB = furthest;
                return;
            }
            else
            {
                firstFound = intercept;
                interceptCount++;
            }
        }

        intercept = FindIntersection(pointA, pointB, vertex3, vertex4, debugLines);
        if (intercept.x != Mathf.Infinity)
        {
            if (interceptCount == 1)
            {
                GetClosestAndFurthest(pointA, firstFound, intercept, out var closest, out var furthest);
                interceptA = closest;
                interceptB = furthest;
                return;
            }
            else
            {
                firstFound = intercept;
                interceptCount++;
            }
        }

        intercept = FindIntersection(pointA, pointB, vertex1, vertex4, debugLines);
        if (intercept.x != Mathf.Infinity)
        {
            if (interceptCount == 1)
            {
                GetClosestAndFurthest(pointA, firstFound, intercept, out var closest, out var furthest);
                interceptA = closest;
                interceptB = furthest;
                return;
            }
            else
            {
                firstFound = intercept;
                interceptCount++;
            }
        }

        if (interceptCount == 1)
        {
            if (IsPointInQuad(pointA, vertex1, vertex2, vertex3, vertex4))
            {
                //GetClosestAndFurthest(pointA, firstFound, pointB, out var closest, out var furthest);
                interceptA = pointA;
                interceptB = firstFound;
                return;
            }
            else
            //if (IsPointInQuad(pointB, vertex1, vertex2, vertex3, vertex4))
            {
                interceptA = firstFound;
                interceptB = pointB;
            }
        }
        else if (IsPointInQuad(pointA,vertex1,vertex2,vertex3,vertex4) &&
            IsPointInQuad(pointB, vertex1, vertex2, vertex3, vertex4))
        {
                interceptA = pointA;
                interceptB = pointB;
        }
    }

    public static void GetThickLineInterceps(Vector2 pointA, Vector2 pointB, Vector2 thickPointA, Vector2 thickPointB, float width,
        out Vector2 closest, out Vector2 furthest, bool debugLines = false)
    {
        var delta = thickPointB - thickPointA;
        var edgeDelta = Vector2.Perpendicular(delta).normalized * width/2;

        var vertex1 = thickPointA + edgeDelta;
        var vertex2 = thickPointB + edgeDelta;
        var vertex3 = thickPointB - edgeDelta;
        var vertex4 = thickPointA - edgeDelta;

        GetQuadIntercepts(pointA, pointB, vertex1, vertex2, vertex3, vertex4, out closest, out furthest, debugLines);
    }

    public static bool DoesLineInterceptThickLine(Vector2 pointA, Vector2 pointB, Vector2 thickPointA, Vector2 thickPointB, float width, bool debugLines = false)
    {
        var delta = thickPointB - thickPointA;
        var edgeDelta = Vector2.Perpendicular(delta).normalized * width / 2;

        var vertex1 = thickPointA + edgeDelta;
        var vertex2 = thickPointB + edgeDelta;
        var vertex3 = thickPointB - edgeDelta;
        var vertex4 = thickPointA - edgeDelta;

        return DoesLineIntersectQuad(pointA, pointB, vertex1, vertex2, vertex3, vertex4, debugLines);
    }

    public static void GetClosestAndFurthest (Vector2 subjPoint, Vector2 pointA, Vector2 pointB, out Vector2 closest, out Vector2 furthest)
    {
        var distA = (pointA - subjPoint).magnitude;
        var distB = (pointB - subjPoint).magnitude;

        if (distA < distB)
        {
            closest = pointA;
            furthest = pointB;
        }
        else
        {
            closest = pointB;
            furthest = pointA;
        }
    }

    public static Vector2 FindIntersection(Vector2 line1Start, Vector2 line1End, Vector2 line2Start, Vector2 line2End, bool debugLines = false)
    {
        if (debugLines) Debug.DrawLine(line2Start, line2End, Color.cyan);

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
            if (debugLines) Debug.DrawLine(line2Start, line2End, Color.green);
            var intersection = new Vector2(line1Start.x + (t * s1_x), line1Start.y + (t * s1_y));
            return intersection;
        }

        // No intersection
        if (debugLines) Debug.DrawLine(line2Start, line2End, Color.red);
        return Vector2.positiveInfinity;
    }

    public static bool TestBoxMinMax(Vector2 min, Vector2 max, Vector2 point, bool debugLines = false)
    {
        if (debugLines)
        {
            DrawBoxMinMax(min, max, Color.blue);
        }

        if (point.x < min.x ||
        point.y < min.y ||
        point.x > max.x ||
        point.y > max.y)
        {
            if (debugLines)
                MarkPoint(point,.5f, Color.red);
            return false;
        }

        if (debugLines)
            MarkPoint(point,.5f,Color.green);

        return true;
    }

    public static bool TestBoxPosSize (Vector2 pos, Vector2 size, Vector2 point, bool debugLines = false)
    {
        Vector2 min = new Vector2(-size.x, -size.y) / 2 + pos;
        Vector2 max = new Vector2(size.x, size.y) / 2 + pos;
        return TestBoxMinMax(min, max, point, debugLines );
    }


    /// <summary>
    /// efficent af
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="cellSize"></param>
    /// <param name="action"></param>
    /// <param name="logTotal"></param>
    public static void GetLineGridIntersections(Vector2 start, Vector2 end, float cellSize, Action<Vector2Int> action, bool logTotal = false)
    {
        // Convert world coordinates to grid coordinates
        Vector2 current = start / cellSize;
        Vector2 goal = end / cellSize;

        // Get the starting and ending cell indices (rounded down)
        int x0 = Mathf.FloorToInt(current.x);
        int y0 = Mathf.FloorToInt(current.y);
        int x1 = Mathf.FloorToInt(goal.x);
        int y1 = Mathf.FloorToInt(goal.y);

        // Initial action for the starting cell
        action(new Vector2Int(x0, y0));

        // Calculate differences between target and start positions
        float dx = Mathf.Abs(goal.x - current.x);
        float dy = Mathf.Abs(goal.y - current.y);

        // Avoid division by zero for vertical or horizontal lines
        if (dx == 0 && dy == 0)
        {
            if (logTotal) Debug.Log("Line is a single cell.");
            return;
        }

        // Directional steps
        int stepX = current.x < goal.x ? 1 : (current.x > goal.x ? -1 : 0);
        int stepY = current.y < goal.y ? 1 : (current.y > goal.y ? -1 : 0);

        // Time to the next grid line in each direction (helps in deciding which direction to step in)
        float tDeltaX = dx > 0 ? 1f / dx : float.MaxValue; // Max value for vertical lines
        float tDeltaY = dy > 0 ? 1f / dy : float.MaxValue; // Max value for horizontal lines

        // The time to cross the current grid cell
        float tMaxX = stepX > 0 ? Mathf.Ceil(current.x) - current.x : current.x - Mathf.Floor(current.x);
        float tMaxY = stepY > 0 ? Mathf.Ceil(current.y) - current.y : current.y - Mathf.Floor(current.y);

        tMaxX /= Mathf.Max(dx, 1e-6f); // Avoid zero for vertical lines
        tMaxY /= Mathf.Max(dy, 1e-6f); // Avoid zero for horizontal lines

        // A small threshold to handle floating-point imprecision
        float threshold = 0.001f;

        // The number of cells we calculate
        int cellsCalculated = 1;

        // Loop through the grid until we reach the destination cell
        while (true)
        {
            // Check if we’ve crossed the target cell boundaries
            if ((stepX > 0 && x0 > x1) || (stepX < 0 && x0 < x1) ||
                (stepY > 0 && y0 > y1) || (stepY < 0 && y0 < y1))
            {
                break;
            }

            // Move to the next cell based on which direction the line is traveling
            if (tMaxX < tMaxY)
            {
                tMaxX += tDeltaX;
                x0 += stepX;
            }
            else
            {
                tMaxY += tDeltaY;
                y0 += stepY;
            }

            // Execute the action (e.g., logging or processing the cell)
            action(new Vector2Int(x0, y0));
            cellsCalculated++;

            // If we've reached the destination cell (handling near-perfect alignments)
            if (x0 == x1 && y0 == y1)
            {
                break;
            }
        }

        // Ensure the final cell is included if the line ends on the grid cell boundary
        if ((x0 == x1 && y0 == y1) ||
            (stepX > 0 && x0 >= x1) || (stepX < 0 && x0 <= x1) ||
            (stepY > 0 && y0 >= y1) || (stepY < 0 && y0 <= y1))
        {
            action(new Vector2Int(x1, y1));
            cellsCalculated++;
        }

        // Optional logging of total cells processed
        if (logTotal)
        {
            Debug.Log($"Total cells calculated: {cellsCalculated}");
        }
    }

    public static bool TestPointWithinTaperedCapsule (Vector2 testPoint, Vector2 pointA, float radiusA, Vector2 pointB, float radiusB
        //,out Vector2 closestPoint, out float thickness
        , bool debugLines = false
        )
    {
        var closestPoint = ClosestPointToLineSegment(testPoint, pointA, pointB, out var ratio);
        var thickness = radiusA - ((radiusA - radiusB) * ratio);

        if (debugLines)
        {
            DrawCircle(closestPoint, thickness, Color.magenta);
            Debug.DrawLine(closestPoint,testPoint,Color.magenta);
        }

        return (closestPoint-testPoint).magnitude <= thickness;
    }

    //i just realilzed this will only work if the box is small enough. it could have the entire capsule inside of it lol
    public static bool TestBoxTouchesTaperedCapsule(Vector2 boxPos, Vector2 boxSize, Vector2 pointA, float radiusA, Vector2 pointB, float radiusB,
        bool debugLines = false)
    {
        //if (TestPointWithinTaperedCapsule(boxPos, pointA, radiusA, pointB, radiusB
        //    //, out var point, out var thickness
        //    ))
        //    return true;

        //if (TestBoxPosSizeTouchesCircle(boxPos, boxSize, point, thickness))
        //    return true;

        //if (TestBoxPosSize((boxPos - pointA).normalized * radiusA + pointA, boxPos, boxSize))
        //    return true;

        //Debug.DrawRay(pointB, Vector2.ClampMagnitude(boxPos - pointB, radiusB), Color.blue);
        //DrawCircle(boxPos,radiusB, Color.blue);

        //if (TestBoxPosSize(boxPos, boxSize, Vector2.ClampMagnitude(boxPos - pointA, radiusA) + pointA, debugLines))
        //    return true;

        //if (TestBoxPosSize(boxPos, boxSize, Vector2.ClampMagnitude(boxPos - pointB, radiusB) + pointB, debugLines))
        //    return true;


        if (DoesLineIntersectBoxPosSize(pointA, pointB, boxPos, boxSize))
            return true;

        var cornerArray = GetBoxCornersPosSize(boxPos, boxSize);

        foreach (var corner in cornerArray)
        {
            if (TestPointWithinTaperedCapsule(corner, pointA, radiusA, pointB, radiusB,debugLines))
                return true;
        }

        //var cornerArray = GetBoxCornersPosSize(boxPos, boxSize);

        //foreach (var corner in cornerArray) //tests if the any corners are within the circle
        //{
        //    //if ((corner - circlePos).magnitude <= circleRadius)
        //    //if (TestBoxPosSize(boxPos,boxSize,ClosestPointToLineSegment(corner,boxPos,circlePos)))
        //    //if (boxPos.x == 0)
        //    var cornerClosestPoint = ClosestPointToLineSegment()
        //}

        //var cornerArray = GetBoxCornersPosSize(boxCenter, boxSize);

        //foreach (var corner in cornerArray)
        //{
        //    if (TestPointWithinTaperedCapsule(corner,pointA,radiusA,pointB,radiusB))
        //    {
        //        return true;
        //    }
        //}

        return false;
    }

    //public static bool TestBoxPosSizeTouchesCircle (Vector2 boxPos, Vector2 boxSize, Vector2 circlePos, float circleRadius)
    //{
    //    //if (TestBoxPosSize(boxPos, boxSize, circlePos)) //tests if the center is within the box
    //    //    return true;

    //    //if ((boxPos - circlePos).magnitude <= circleRadius) //tests if the box center is within the circle
    //    //    return true;

    //    if (TestBoxPosSize(boxPos, boxSize, (boxPos - circlePos).normalized * circleRadius + circlePos))
    //        return true;

    //    return false;
    //}

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

    public static void MarkPoint(Vector2 point, float size, Color color)
    {
        var min = -Vector2.one * size;
        var max = -min;

        Debug.DrawLine(min + point, max + point, color);

        min = Vector2.Perpendicular(min);
        max = Vector2.Perpendicular(max);

        Debug.DrawLine(min + point, max + point, color);
    }

    public static void DrawBoxMinMax(Vector2 min, Vector2 max, Color color)
    {
        GetTopLeftAndBottomRight(min, max, out var topLeft, out var bottomRight);
        Debug.DrawLine(min, topLeft, color);
        Debug.DrawLine(topLeft, max, color);
        Debug.DrawLine(max, bottomRight, color);
        Debug.DrawLine(bottomRight, min, color);
    }

    public static void DrawBoxPosSize(Vector2 pos, Vector2 size, Color color)
    {
        Vector2 min = new Vector2(-size.x, -size.y) / 2 + pos;
        Vector2 max = new Vector2(size.x, size.y) / 2 + pos;
        DrawBoxMinMax(min, max, color);
    }

    public static void GetTopLeftAndBottomRight(Vector2 min, Vector2 max, out Vector2 topLeft, out Vector2 bottomRight)
    {
        topLeft = new(min.x, max.y);
        bottomRight = new(max.x, min.y);
    }

    public static Vector2 RandomPosInBoxMinMax (Vector2 min , Vector2 max)
    {
        return new Vector2(UnityEngine.Random.Range(min.x,max.x),UnityEngine.Random.Range(min.y,max.y));
    }


    /// <summary>
    /// orders points from min in clockwise order
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    public static Vector2[] GetBoxCornersPosSize (Vector2 pos, Vector2 size)
    {
        var halfSize = size / 2;

        return new Vector2[] {
            pos + new Vector2(-halfSize.x,-halfSize.y),
            pos + new Vector2(-halfSize.x,halfSize.y),
            pos + new Vector2(halfSize.x,halfSize.y),
            pos + new Vector2(halfSize.x, -halfSize.y)
        };
    }

    public static Vector2 RandomPosInBoxPosSize (Vector2 pos, Vector2 size)
    {
        return RandomPosInBoxMinMax(pos - size / 2, pos + size / 2);
    }

    public static Vector2 RandomInsideRing (Vector2 center, float minRad, float maxRad)
    {
        var dist = UnityEngine.Random.Range(minRad, maxRad);
        return (Vector2)(Quaternion.AngleAxis(UnityEngine.Random.Range(0, 360), Vector3.forward) * Vector3.up * dist + (Vector3)center);
    }

    public static Vector2[,] DistributePointsInBoxPosSize(Vector2 pos, Vector2 size, Vector2 distributeSize)
    {

        Vector2Int intSize = Vector2Int.CeilToInt(size / distributeSize);
        Vector2 outerMin = pos - size / 2 + (size - new Vector2(intSize.x, intSize.y) * distributeSize) / 2;

        var array = new Vector2[intSize.x+1, intSize.y+1];

        var boxMin = pos - size / 2;
        var boxMax = pos + size / 2;

        for (var y = 0; y < intSize.y+1; y++)
        {
            for (int x = 0; x < intSize.x+1; x++)
            {
                Vector2 point = outerMin + new Vector2(x, y) * distributeSize;
                                
                if (x == 0 || y == 0)
                    point = Vector2.Max(point, boxMin);

                if (x == intSize.x || y == intSize.y)
                    point = Vector2.Min(point, boxMax);

                array[x, y] = point;
            }
        }

        return array;
    }


    public static void DrawLine (List<Vector2> points, Color color)
    {
        Vector2 lastPoint = Vector2.zero;

        for (var i = 0; i < points.Count; i++)
        {
            var point = points[i];

            if (i > 0)
            {
                Debug.DrawLine(lastPoint, point, color);
            }

            lastPoint = point;
        }
    }

    public static float GetLineLength (List<Vector2> points, bool debugLines = false)
    {
        float total = 0;

        Vector2 lastPoint = Vector2.zero;

        for (var i = 0; i < points.Count; i++)
        {
            var point = points[i];

            if (i > 0)
            {
                total += Vector2.Distance(lastPoint, point);

                if (debugLines)
                    Debug.DrawLine(lastPoint, point, Color.cyan);
            }

            lastPoint = point;
        }

        return total;
    }

    public static Vector2 ClampToBoxMinMax(Vector2 point, Vector2 min, Vector2 max)
    {
        return Vector2.Max(min, Vector2.Min(max, point));
    }

    public static Vector2 ClampToBoxPosSize (Vector2 point, Vector2 pos, Vector2 size)
    {
        var delta = size / 2;
        var min = pos - delta;
        var max = pos + delta;

        return ClampToBoxMinMax (point, min, max);
    }
}

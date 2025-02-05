using System;
using System.Collections;
using System.Collections.Generic;
//using System.Drawing;

//using System.Drawing;
using Unity.VisualScripting;


//using System.Drawing;
//using Unity.VisualScripting;
//using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using UnityEngine.UIElements;
//using UnityEngine.Rendering.PostProcessing;

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
        if (Vector2.Distance(circleCenter,lineStart) <= circleRadius)
        {
            return lineStart;
        }

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

        var boxCenter = (boxMin + boxMax) / 2;

        var closestPoint = ClosestPointToLineSegment(boxCenter, pointA, pointB);

        return TestBoxMinMax(boxMin,boxMax, closestPoint); 
        //i haven't confirmed this works properly since changing it but it probably does
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
            if (debugLines)
            {
                Debug.DrawLine(line1Start, line1End, Color.green);
                Debug.DrawLine(line2Start, line2End, Color.green);
            }
            var intersection = new Vector2(line1Start.x + (t * s1_x), line1Start.y + (t * s1_y));
            return intersection;
        }

        // No intersection
        if (debugLines)
        {
            Debug.DrawLine(line1Start, line1End, Color.red);
            Debug.DrawLine(line2Start, line2End, Color.red);
        }
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




    public static bool TestPointWithinTaperedCapsule (Vector2 testPoint, Vector2 pointA, float radiusA, Vector2 pointB, float radiusB
    //,out Vector2 closestPoint, out float thickness
    , bool debugLines = false
    )
    {
        var closestPoint = GetClosestPointOnTaperedCapsule(testPoint, pointA, radiusA, pointB, radiusB, out var thickness, debugLines);

        return (closestPoint-testPoint).magnitude <= thickness;
    }

    public static Vector2 GetClosestPointOnTaperedCapsule (Vector2 testPoint, Vector2 pointA, float radiusA, Vector2 pointB, float radiusB,
        out float distance,
        bool debugLines = false)
    {
        var closestPoint = ClosestPointToLineSegment(testPoint, pointA, pointB, out var ratio);
        distance = radiusA - ((radiusA - radiusB) * ratio);

        if (debugLines)
        {
            DrawCircle(closestPoint, distance, Color.magenta);
            Debug.DrawLine(closestPoint, testPoint, Color.magenta);
        }

        return closestPoint;
    }

    public static bool TestCirlceTouchesTaperedCapsule (Vector2 circlePos, float circleRadius, Vector2 pointA, float radiusA, Vector2 pointB, float radiusB,
        bool debugLines = false)
    {
        var point = GetClosestPointOnTaperedCapsule(circlePos, pointA, radiusA, pointB, radiusB, out var thickness, debugLines);

        if (CirclesOverlap(circlePos, circleRadius, point, thickness))
        {
            if (debugLines)
            {
                DrawCircle(circlePos, circleRadius, Color.green);
            }

            return true;
        }
        else if (debugLines)
        {
            DrawCircle(circlePos, circleRadius, Color.red);
        }

        return false;
    }

    //i just realilzed this will only work if the box is small enough. it could have the entire capsule inside of it lol
    //public static bool TestBoxTouchesTaperedCapsule(Vector2 boxPos, Vector2 boxSize, Vector2 pointA, float radiusA, Vector2 pointB, float radiusB,
    //    bool debugLines = false)
    //{
    //    //var cornerArray = GetBoxCornersPosSize(boxPos, boxSize);

    //    if (TestBoxPosSizeTouchesCircle(boxPos, boxSize, pointA, radiusA, debugLines))
    //        return true;

    //    if (TestBoxPosSizeTouchesCircle(boxPos, boxSize, pointB, radiusB, debugLines))
    //        return true;

    //    //var point = GetClosestPointOnTaperedCapsule(boxPos, pointA, radiusA, pointB, radiusB, out var distance, debugLines);

    //    //if (TestBoxPosSizeTouchesCircle(boxPos, boxSize, point, distance, debugLines))
    //    //    return true;

    //    //thinking i could decrease boxcircle tests if i just make the corner lines the radius of the biggest circle

    //    var maxRadius = Mathf.Max(radiusA, radiusB);

    //    var crossLine = (boxSize.normalized * (maxRadius*2 + boxSize.magnitude)) /2;

    //    Vector2
    //        intersection = FindIntersection(pointA, pointB,
    //        boxPos - crossLine,
    //        boxPos + crossLine,
    //        debugLines);

    //    if (intersection != Vector2.positiveInfinity)
    //    {
    //        var point = GetClosestPointOnTaperedCapsule(intersection, pointA, radiusA, pointB, radiusB, out var distance, debugLines);

    //        if (TestBoxPosSizeTouchesCircle(boxPos, boxSize, point, distance, debugLines))
    //            return true;
    //    }

    //    var perpCrossLine = crossLine.Perpendicular1();

    //    intersection = FindIntersection(pointA, pointB,
    //        boxPos - perpCrossLine,
    //        boxPos + perpCrossLine,
    //        debugLines);

    //    if (intersection != Vector2.positiveInfinity)
    //    {
    //        var point = GetClosestPointOnTaperedCapsule(intersection, pointA, radiusA, pointB, radiusB, out var distance, debugLines);

    //        if (TestBoxPosSizeTouchesCircle(boxPos, boxSize, point, distance, debugLines))
    //            return true;
    //    }

    //    //foreach (var corner in cornerArray)
    //    //{
    //    //    if (TestPointWithinTaperedCapsule(corner, pointA, radiusA, pointB, radiusB, debugLines))
    //    //        return true;
    //    //}

    //    return false;
    //}

    public static bool TestBoxPosSizeTouchesCircle(Vector2 boxPos, Vector2 boxSize, Vector2 circlePos, float circleRadius,
        bool debugLines = false)
    {
        var clampedPoint = ClampToBoxPosSize(circlePos, boxPos, boxSize);

        if (debugLines)
            MarkPoint(clampedPoint, boxSize.x * .05f, Color.green);

        if ((clampedPoint - circlePos).magnitude <= circleRadius)
            return true;

        return false;
    }

    //public static Vector2 GetCircleBoxCollisionPoint(Vector2 start, Vector2 end, float circleRadius, Vector2 boxMin, Vector2 boxMax)
    //{
    //    var corners = GetBoxCornersMinMax(boxMin, boxMax);

    //    var closestPoint 
    //}

    public static bool CirclesOverlap(Vector2 center1, float radius1, Vector2 center2, float radius2, bool debugLines = false)
    {
        var result = (center1 - center2).magnitude <= radius1 + radius2;

        if (debugLines)
        {
            var color = result ? Color.green : Color.red;

            DrawCircle(center1, radius1, color);
            DrawCircle(center2, radius2, color);
        }

        // If the squared distance is less than or equal to the squared radii sum, they overlap
        return result;
    }

    /// <summary>
    /// returns distance before hitting point
    /// </summary>
    /// <param name="circleStart"></param>
    /// <param name="radius"></param>
    /// <param name="direction"></param>
    /// <param name="point"></param>
    /// <returns></returns>
    public static float CircleCollideWithPoint (Vector2 circleStart, float radius, Vector2 direction, Vector2 point, out bool wasBehind, bool returnIfBehind = false)
    {
        var collisionPointDot = Vector2.Dot(direction.normalized, point - circleStart);

        wasBehind = collisionPointDot < 0;

        if (wasBehind && returnIfBehind)
            return 0;

        // Calculate the perpendicular distance to the collision point
        var b = radius;
        var c = Mathf.Abs(Vector2.Dot(Vector2.Perpendicular(direction), point - circleStart));
        var a = Mathf.Sqrt((b * b) - (c * c));

        return Mathf.Max(0, collisionPointDot - a);
    }

    public static bool TestSegmentsWithinDistance (Vector2 line1Start, Vector2 line1End, Vector2 line2Start, Vector2 line2End, float distance)
    {
        if (Vector2.Distance(ClosestPointToLineSegment(line2Start, line1Start, line1End), line2Start) >= distance)
            return true;
        if (Vector2.Distance(ClosestPointToLineSegment(line2End, line1Start, line1End), line2End) >= distance)
            return true;
        if (Vector2.Distance(ClosestPointToLineSegment(line1Start, line2Start, line2End), line1Start) >= distance)
            return true;
        if (Vector2.Distance(ClosestPointToLineSegment(line1End, line2Start, line2End), line1End) >= distance)
            return true;

        return false; //idk if ill use this lol
    }

    //public static float CircleCollideWithBoxMinMax(Vector2 circleStart, float circleRadius, Vector2 circleDelta, Vector2 boxMin, Vector2 boxMax)
    //{
    //find point on line closest to box center, then roll it back towards the start until just barely touching the box
    //}

    public static Vector2? CircBoxCollButDoesntWork(Vector2 start, Vector2 end, float radius, Vector2 boxMin, Vector2 boxMax,
        bool debugLines = false)
    {
        var boxClampedStart = ClampToBoxMinMax(start, boxMin, boxMax);

        if (debugLines)
        {
            DrawCircle(start, radius, Color.green);
            DrawBoxMinMax(boxMin, boxMax, Color.blue);
        }


        Vector2 delta = end - start; //im tired too late ahh

        if (Vector2.Distance(boxClampedStart,start) <= radius)
        {
            if (debugLines)
            {
                DrawCircle(end, radius, Color.red);
                MarkPoint(boxClampedStart, .5f, Color.green);
            }

            return boxClampedStart;
        }

        var boxCenter = (boxMin + boxMax) / 2;

        var closestPoint = ClosestPointToLineSegment(boxCenter, start, end);

        var clampedClosest = ClampToBoxMinMax(closestPoint, boxMin, boxMax);

        DrawCircle(closestPoint, radius, Color.blue);
        MarkPoint(clampedClosest, .5f, Color.blue);

        if (Vector2.Distance(closestPoint, clampedClosest) > radius)
        {
            if (debugLines)
            {
                DrawCircle(end, radius, Color.green); 
                Debug.DrawRay(start, delta, Color.green);

                var circleEdgeOffset = Vector2.Perpendicular(delta).normalized * radius;
                Debug.DrawRay(start + circleEdgeOffset, delta, Color.green);
                Debug.DrawRay(start - circleEdgeOffset, delta, Color.green);
            }

            return null;
        }

        if (debugLines)
        {
            DrawCircle(end, radius, Color.red);
        }

        var m = delta.y / delta.x;

        var movingRight = delta.x > 0;
        var movingUp = delta.y > 0;

        var horizontalB = start.y - (m * start.x + (movingRight ? radius : -radius));

        var horizontalX = movingRight ? boxMin.x : boxMax.x;

        var horizontalY = (m * horizontalX) + horizontalB;

        var horizontalMissed = movingRight ?
            movingUp && horizontalY > boxMax.y :
            !movingUp && horizontalY < boxMin.y;

        if (horizontalMissed)
        {
            if (debugLines)
                MarkPoint(boxClampedStart, .5f, Color.green);

            return boxClampedStart;
        }

        var hitY = movingRight ?
            movingUp && horizontalY <= boxMax.y :
            !movingUp && horizontalY >= boxMin.y;

        if (hitY)
        {
            var point = new Vector2(horizontalX, horizontalY);

            if (debugLines)
                MarkPoint(point, .5f, Color.green);

            return point;
        }

        var verticalB = start.y + (movingUp ? radius : -radius) - (m * start.x);

        var verticalY = movingUp ? boxMin.y : boxMax.y;

        var verticalX = (verticalY - verticalB) / m;

        var verticalMissed = movingUp ?
            movingRight && verticalX > boxMax.x :
            !movingRight && verticalX < boxMin.x;

        if (verticalMissed)
        {
            if (debugLines)
                MarkPoint(boxClampedStart, .5f, Color.green);

            return boxClampedStart;
        }

        var hitX = movingUp ?
            movingRight && verticalX <= boxMax.x :
            !movingRight && verticalX >= boxMin.x;

        if (hitX)
        {
            return new Vector2(verticalX, verticalY);
        }

        return boxClampedStart;

        //if ((m * boxCenter.x) + horizontalB) //breh this is so complicated
    }

    //not even optimised
    //public static Vector2? FindPointBoxCollisionPoint (Vector2 point, Vector2 delta, Vector2 boxMin, Vector2 boxMax, out Vector2Int edgeHit)
    //{
    //    //removed box test because it's probably redundant

    //    var end = point + delta;

    //    edgeHit = Vector2Int.zero;

    //    if ((point.x > boxMax.x && end.x > boxMax.x) ||
    //        (point.x < boxMin.x && end.x < boxMin.x) ||
    //        (point.y > boxMax.y && end.y > boxMax.y) ||
    //        (point.y < boxMin.y && end.y < boxMin.y)
    //)
    //    {
    //        return null;
    //    }

    //    if (delta.x == 0)// if the direction is perfectly vertical, 
    //    {
    //        if (point.x < boxMin.x || point.x > boxMax.x) //if the point is to the left or right of the box, return null
    //            return null;

    //        if (point.y >= boxMax.y && (end.y <= boxMax.y)) //if point is above box, and would move through the box
    //        {
    //            return new Vector2(point.x, boxMax.y); //return the point it would hit
    //        }
    //        else if (point.y <= boxMin.y && (end.y >= boxMin.y)) //if point is above box, and would move through the box
    //        {
    //            return new Vector2(point.x, boxMin.y); //return the point it would hit
    //        }
    //    }

    //    if (TestBoxMinMax(boxMin, boxMax, point))
    //    {
    //        return point;
    //    }

    //    var m = delta.y / delta.x;

    //    var b = point.y - (m * point.x);

    //    var movingRight = delta.x > 0f;

    //    var leftY = (m * boxMin.x) + b;
    //    var hitsLeft = (leftY >= boxMin.y && leftY <= boxMax.y);

    //    if (hitsLeft && movingRight) //if ends inside or is moving right
    //    {
    //        edgeHit = Vector2Int.left;
    //        return new(boxMin.x, leftY); //return intercept
    //    }

    //    var rightY = (m * boxMax.x) + b;
    //    var hitsRight = (rightY >= boxMin.y && rightY <= boxMax.y);

    //    //FIX; probably should clamp the intercepts to the end pos

    //    if (hitsRight && !movingRight) //if it ends inside or is moving left
    //    {
    //        edgeHit = Vector2Int.right;
    //        return new(boxMax.x, rightY); //return right point
    //    }

    //    var bottomX = (boxMin.y - b) / m;
    //    var hitsBottom = (bottomX >= boxMin.x && bottomX <= boxMax.x);

    //    if (hitsBottom && delta.y > 0)
    //    {
    //        edgeHit = Vector2Int.down;
    //        return new Vector2(bottomX, boxMin.y);
    //    }

    //    var topX = (boxMax.y - b) / m;
    //    var hitsTop = (topX >= boxMin.x && topX <= boxMax.x);

    //    if (hitsTop)
    //    {
    //        edgeHit = Vector2Int.up;
    //        return new Vector2(topX, boxMax.y);
    //    }

    //    return null;
    //}

    public static void DrawCircle(Vector2 center, float radius, Color color, int res = 4)
    {
        Vector2 lastPoint = Vector2.up * radius;

        int verts = res * 4;
        var angle = 360f / verts;

        for (int i = 1; i < verts + 1; i++)
        {
            Vector2 point = Quaternion.AngleAxis(angle, Vector3.forward) * lastPoint;

            Debug.DrawLine(point + center, lastPoint + center, color);

            lastPoint = point;
        }
    }

    public static void DrawRingOfCircles (Vector2 ringCenter, float ringRadius, float circleRadius, int circleCount, Color color, int circleRes = 4)
    {
        Vector2 lastPoint = Vector2.up * ringRadius;
        var angle = 360f / circleCount;

        for (int i = 1; i < circleCount + 1; i++)
        {
            Vector2 point = Quaternion.AngleAxis(angle, Vector3.forward) * lastPoint;

            DrawCircle(point + ringCenter, circleRadius, color, circleRes);

            //Debug.DrawLine(point + center, lastPoint + center, color);

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

    public static Vector2[] GetBoxCornersMinMax (Vector2 min, Vector2 max)
    {
        var pos = (max + min) / 2;
        var size = max - min;

        return GetBoxCornersPosSize(pos, size);
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

    public static IEnumerable<Vector2Int> CellsFromLine(Vector2 start, Vector2 end, float cellSize)
    {
        start /= cellSize;
        end /= cellSize;

        var delta = end - start;

        var xDirection = delta.x > 0 ? 1 : -1;
        var yDirection = delta.y > 0 ? 1 : -1;

        // Convert start and end to grid coordinates based on the cell size
        Vector2Int startCell = RoundToInt(start);

        var startCellDelta = startCell - start;

        Vector2 offset = Vector2.zero;

        if (Mathf.Abs(startCellDelta.x) == 0.5f)
            offset += 0.5f * xDirection * Vector2.right;

        if (Mathf.Abs(startCellDelta.y) == 0.5f)
            offset += 0.5f * yDirection * Vector2.up;

        startCell = RoundToInt(start + offset);

        Vector2Int endCell = RoundToInt(end);

        static Vector2Int RoundToInt (Vector2 pos)
        {
            var floored = Vector2Int.FloorToInt(pos);
            var remainder = pos - floored;

            return new Vector2Int(remainder.x >= .5f ? 1 : 0, remainder.y >= .5f ? 1 : 0) + floored;
        }

        if (startCell.x == endCell.x)
        {
            var cell = startCell;

            var xDelta = Mathf.Clamp(endCell.y - startCell.y, -1, 1);

            while (true)
            {
                yield return cell;

                if (cell.y == endCell.y)
                    yield break;

                cell.y += xDelta;
            }
        } //perfectly vertical
        else if (startCell.y == endCell.y)
        {
            var cell = startCell;

            var yDelta = Mathf.Clamp(endCell.x - startCell.x, -1, 1);

            while (true)
            {
                yield return cell;

                if (cell.x == endCell.x)
                    yield break;

                cell.x += yDelta;
            }
        } //perfectly horizontal

        var slope = delta.y / delta.x;

        var yIntercept = start.y - (slope * start.x);

        //MarkPoint(new Vector2(0, yIntercept) * cellSize, 1, Color.magenta);

        var cellDelta = endCell - startCell;

        var predictedTotal = Mathf.Abs(cellDelta.x) + Mathf.Abs(cellDelta.y) + 2;

        //if (delta.x < 0 || delta.y < 0)
        //    yield break;

        yield return startCell;

        var cellsCalculated = 1;

        var currentCell = startCell;

        while (cellsCalculated < predictedTotal)
        {
            var nextRowX = currentCell.x + .5f * xDirection;

            var nextRowIntercept = slope * nextRowX + yIntercept;

            //MarkPoint(new Vector2(nextRowX, nextRowIntercept) * cellSize, .2f, Color.green);

            var deltaToNextRow = Mathf.Abs(nextRowIntercept - currentCell.y);

            if (deltaToNextRow > .5f)
            {
                currentCell += Vector2Int.up * yDirection;
            }
            else
            {
                currentCell += Vector2Int.right * xDirection;
            }

            yield return currentCell;
            if (currentCell == endCell)
                yield break;
            cellsCalculated++;
        }

        yield break;
    }

    public static void BoxMinMaxToPosSize (Vector2 min, Vector2 max, out Vector2 pos, out Vector2 size)
    {
        pos = (min + max) / 2;
        size = max - min;
    }

    public static void BoxPosSizeToMinMax (Vector2 pos, Vector2 size, out Vector2 min, out Vector2 max)
    {
        var delta = size / 2;
        min = pos - delta;
        max = pos + delta;
    }

    public static void CircleToBoxPosSize (Vector2 center, float radius, out Vector2 pos, out Vector2 size)
    {
        pos = center;
        size = radius * 2 * Vector2.one;
    }

    //public static IEnumerable<Vector2Int> CellsFromArc(Vector2 arcPivot, float radius, float centerAngle, float spreadAngle, float cellSize)
    //{
    //    Vector2 pointA = (Vector2)(Quaternion.AngleAxis(centerAngle - (spreadAngle * 2), Vector3.forward) * Vector2.up) * radius + arcPivot;
    //}

    //public static void DrawArc (Vector2 pivot, float radius, float dirAngle, float spreadAngle, int res = 4)
    //{
        
    //}
}

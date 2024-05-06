using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class TrenchLine
{
    public List<Vector2> points = new();
    public float width;
    public bool loop = false, debugLines = false;

    List<Vector3> verts = new();
    List<int> tris = new();
    public Mesh mesh;

    public void NewMesh(int endRes, int cornerRes)
    {
        if (!mesh) mesh = new();

        verts.Clear();
        tris.Clear();

        if (points.Count > 1)
        {
            Vector2 lastPoint;
            if (loop) lastPoint = points[^1];
            else lastPoint = Vector2.zero;

            for (var i = 0; i < points.Count; i++)
            {
                var point = points[i];

                if (i == 0 && !loop)
                {
                    point = points[i];
                    Vector2 nextPoint = point;

                    for (var l = 0; l < points.Count; l++)
                    {
                        var thisPoint = points[l];
                        if (thisPoint != point)
                        {
                            nextPoint = thisPoint;
                            i = l - 1;
                            break;
                        }
                    }

                    if (point == nextPoint)
                    {
                        DrawEnd(point, Vector2.up, endRes);
                        DrawEnd(point, Vector2.down, endRes);
                    }
                    else
                    {
                        DrawEnd(point, nextPoint, endRes);
                    }
                }
                else
                {
                    while (i + 1 < points.Count && points[i + 1] == point)
                    {
                        point = points[i + 1];
                        i++;
                    }

                    if (i == points.Count - 1 && !loop)
                    {
                        DrawEnd(point, lastPoint, endRes);
                    }


                    //if (point == lastPoint)
                    //{
                    //    points.RemoveAt(i);
                    //    i--;
                    //}

                    if (i != 0 && debugLines)
                    {
                        Debug.DrawLine(point, lastPoint, Color.cyan);
                    }

                    if (i < points.Count - 1 && i > 0)
                    {
                        var nextPoint = points[i + 1];

                        if (i > 0)
                        {
                            DrawCorner(lastPoint, point, nextPoint, cornerRes);
                        }
                    }
                    else if (loop)
                    {
                        var a = points[(int)Mathf.Repeat(i - 1, points.Count)];
                        var c = points[(int)Mathf.Repeat(i + 1, points.Count)];
                        DrawCorner(a, point, c, cornerRes);

                        //Debug.DrawLine(a, point, Color.cyan);
                    }
                }

                lastPoint = point;
            }
        }

        mesh.triangles = new int[] { };
        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
    }

    void DrawEnd(Vector2 end, Vector2 other, int res)
    {
        Vector3 centerVert = end;

        Vector3 dir = (end - other).normalized;

        var startDelta = Vector2.Perpendicular(dir) * width/2;

        var extVertCount = res * 2;

        var angle = 180f / extVertCount;

        var lastPoint = Vector3.zero;

        for (var l = 0; l < extVertCount + 1; l++)
        {
            var newPoint = Quaternion.AngleAxis(angle * l, -Vector3.forward) * startDelta + centerVert;

            if (l > 0)
            {
                AddTri(lastPoint, newPoint, centerVert);
            }

            lastPoint = newPoint;
        }

        GetMidPoints(end, other, out var aBEdgeA, out var aBEdgeB);
        var edgeDelta = Vector2.Perpendicular(dir).normalized * width / 2;
        AddTri(centerVert, end + edgeDelta, aBEdgeA);
        AddTri(centerVert, end - edgeDelta, aBEdgeB);
        AddTri(centerVert, aBEdgeA, aBEdgeB);
    }

    void DrawCorner(Vector2 a, Vector2 b, Vector2 c, int res, bool lines = false)
    {
        var toA = (a - b).normalized;
        var toC = (c - b).normalized;

        if (toA == -toC)
        {
            GetMidPoints(a, b, out var w, out var x);
            GetMidPoints(b, c, out var y, out var z);
            DrawBox(w, x, y, z, lines);
            return;
        }

        var aBDelta = b - a;
        var bCDelta = b - c;

        Vector2 furthestEnd;

        if (aBDelta.magnitude > bCDelta.magnitude)
        {
            furthestEnd = a;
        }
        else
        {
            furthestEnd = c;
        }

        if (toA == toC)
        {
            DrawEnd(b, furthestEnd, res);
            //GetMidPoints(a, b, out var w, out var x);
            return; // Exit early if toA is equal to toC
        }

        // Calculate the angle between vectors ba and bc in radians
        var angleRad = Mathf.Acos(Vector2.Dot(toA, toC));

        Vector2 tangentPointA, tangentPointC;

        bool flip = true;

        if (Vector3.Cross(toA, toC).z < 0)
        {
            tangentPointA = b + Vector2.Perpendicular(toA) * width / 2;
            tangentPointC = b - Vector2.Perpendicular(toC) * width / 2;

            flip = !flip;
        }
        else
        {
            tangentPointA = b - Vector2.Perpendicular(toA) * width / 2;
            tangentPointC = b + Vector2.Perpendicular(toC) * width / 2;
        }

        // Debug draw the inner corner and tangent points
        //Debug.DrawLine(b, innerCorner, Color.red);
        if (debugLines || lines)
        {
            Debug.DrawLine(b, tangentPointA, Color.green);
            Debug.DrawLine(b, tangentPointC, Color.green);
        }

        //AddTri(tangentPointA, innerCorner, b);
        //AddTri(tangentPointC, innerCorner, b);

        GetMidPoints(a, b, out var aBEdgeA, out var aBEdgeB);
        if (flip)
        {
            var temp = aBEdgeA;
            aBEdgeA = aBEdgeB;
            aBEdgeB = temp;
        }
        var innerTangentA = (b - tangentPointA)+b;
        DrawBox(tangentPointA, aBEdgeA, innerTangentA, aBEdgeB, lines);

        GetMidPoints(b, c, out var bCEdgeA, out var bCEdgeB);
        if (flip)
        {
            var temp = bCEdgeA;
            bCEdgeA = bCEdgeB;
            bCEdgeB = temp;
        }
        var innerTangentC = (b - tangentPointC) + b;
        DrawBox(tangentPointC, bCEdgeA, innerTangentC, bCEdgeB, lines);

        var degs = angleRad * Mathf.Rad2Deg;
        degs = 180 - degs;

        var cornerVertCount = Mathf.CeilToInt(degs / 90 * res);

        float cornerVertAngle = degs;
        if (cornerVertCount > 0) cornerVertAngle /= cornerVertCount;

        var lastOne = tangentPointA;
        Vector3 tangentDelta = (tangentPointA - b);

        for (var i = 1; i < cornerVertCount + 1; i++)
        {
            var axis = Vector3.forward;
            if (flip) axis = -axis;

            Vector2 newVert = Quaternion.AngleAxis(cornerVertAngle * i, axis) * tangentDelta;
            newVert += b;

            AddTri(lastOne, b, newVert, lines);
            lastOne = newVert;
        }
    }

    public static Vector2 CircleLineIntersect(Vector2 lineOrigin, Vector2 targetPos, Vector2 circlePos, float circleDiameter)
    {
        // Calculate the direction of the line
        Vector2 lineDirection = (targetPos - lineOrigin).normalized;

        // Calculate the vector from the line origin to the circle center
        Vector2 circleToLine = lineOrigin - circlePos;

        // Calculate the dot product of the line direction and the circle to line vector
        float dotProduct = Vector2.Dot(lineDirection, circleToLine);

        // Calculate the discriminant of the quadratic equation
        float discriminant = dotProduct * dotProduct - circleToLine.sqrMagnitude + (circleDiameter * circleDiameter) / 4;

        // If the discriminant is negative, the line does not intersect with the circle
        if (discriminant < 0)
        {
            return targetPos;
        }

        // Calculate the two possible intersection distances along the line
        float t1 = -dotProduct + Mathf.Sqrt(discriminant);
        float t2 = -dotProduct - Mathf.Sqrt(discriminant);

        // Get the furthest intersection point from the line origin
        float t = Mathf.Max(t1, t2);

        // Calculate the intersection point
        Vector2 intersectionPoint = lineOrigin + lineDirection * t;

        // Modify the target position if the line intersects with the circle
        targetPos = intersectionPoint;

        return targetPos;
    }

    void GetMidPoints (Vector2 pointA, Vector2 pointB,  out Vector2 edgeA, out Vector2 edgeB)
    {
        var dir = pointA - pointB;
        var middle = (pointA + pointB) / 2;
        var edgeDelta = Vector2.Perpendicular(dir).normalized * width / 2 ;
        edgeA = middle + edgeDelta;
        edgeB = middle - edgeDelta;
    }

    void DrawBox (Vector2 a, Vector2 b, Vector2 c, Vector2 d, bool draw = false)
    {
        AddTri(a, b, c, draw);
        AddTri(b, c, d, draw);
    }

    void AddTri (Vector3 a, Vector3 b, Vector3 c, bool draw = false)
    {
        tris.Add(GetVertIndex(a));
        tris.Add(GetVertIndex(b));
        tris.Add(GetVertIndex(c));

        if (draw || debugLines)
        {
            Debug.DrawLine(a, b, Color.yellow);
            Debug.DrawLine(b, c, Color.yellow);
            Debug.DrawLine(c, a, Color.yellow);
        }
    }

    int GetVertIndex (Vector3 vert)
    {
        var index = verts.IndexOf(vert);
        if (index < 0)
        {
            verts.Add(vert);
            return verts.Count - 1;
        }

        return index;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class TrenchLine
{
    public List<Vector2> points = new();
    public float width;

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
            var lastPoint = Vector2.zero;

            for (var i = 0; i < points.Count; i++)
            {
                var point = points[i];

                if (i == 0)
                {
                    point = points[i];
                    DrawEnd(point, points[i+1], endRes);
                }
                else
                {
                    if (points[i-1] != point || (i < points.Count-1 && points[i+1] != point))
                        lastPoint = points[i - 1];

                    while (i < points.Count - 1 && points[i - 1] == point)
                    {
                        point = points[i + 1];
                        i++;
                    }

                    if (i == points.Count - 1)
                    {
                        DrawEnd(point, lastPoint, endRes);
                    }

                    Debug.DrawLine(point, lastPoint, Color.cyan);

                    //if (point == lastPoint)
                    //{
                    //    points.RemoveAt(i);
                    //    i--;
                    //}

                    if (i < points.Count - 1)
                    {
                        var nextPoint = points[i + 1];

                        if (i > 0)
                        {
                            DrawCorner(lastPoint, point, nextPoint, cornerRes);
                        }
                    }
                }
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

    void DrawCorner(Vector2 a, Vector2 b, Vector2 c, int res)
    {
        var toA = (a - b).normalized;
        var toC = (c - b).normalized;

        if (toA == -toC)
        {
            GetMidPoints(a, b, out var w, out var x);
            GetMidPoints(b, c, out var y, out var z);
            DrawBox(w, x, y, z);
            return;
        }

        if (toA == toC)
        {
            var furthestEnd = a;
            if (Vector2.Distance(a, b) < Vector2.Distance(b, c)) furthestEnd  = b;

            DrawEnd(b, furthestEnd, res);
            GetMidPoints(a, b, out var w, out var x);
            return; // Exit early if toA is equal to toC
        }

        // Calculate the angle between vectors ba and bc in radians
        var angleRad = Mathf.Acos(Vector2.Dot(toA, toC));

        // Calculate the bisector direction of the angle between ba and bc
        var bisectorDir = (toA + toC).normalized;

        var innerCornerDist = width / (2 * Mathf.Sin(angleRad / 2));
        
        Vector2 innerCorner = b + bisectorDir * innerCornerDist;

        // Calculate the distance from b to the inner corner
        //if (Vector2.Distance(b, a) > Vector2.Distance(b, c))
        //{
        //    var target = b + toA * 10;
        //    var idk = CircleLineIntersect(-Vector2.Perpendicular(toA) + b, target, c, width);
        //    if (idk != target) innerCorner = idk;
        //}
        //else
        //{
        //    innerCorner = CircleLineIntersect(b, innerCorner, a, width);
        //}


        // Calculate the tangent points on the outside of the corner
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
        Debug.DrawLine(b, innerCorner, Color.red);
        Debug.DrawLine(b, tangentPointA, Color.green);
        Debug.DrawLine(b, tangentPointC, Color.green);

        AddTri(tangentPointA, innerCorner, b);
        AddTri(tangentPointC, innerCorner, b);

        GetMidPoints(a, b, out var aBEdgeA, out var aBEdgeB);
        if (flip)
        {
            var temp = aBEdgeA;
            aBEdgeA = aBEdgeB;
            aBEdgeB = temp;
        }
        DrawBox(tangentPointA, aBEdgeA, innerCorner, aBEdgeB);

        GetMidPoints(b, c, out var bCEdgeA, out var bCEdgeB);
        if (flip)
        {
            var temp = bCEdgeA;
            bCEdgeA = bCEdgeB;
            bCEdgeB = temp;
        }
        DrawBox(tangentPointC, bCEdgeA, innerCorner, bCEdgeB);

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

            AddTri(lastOne, b, newVert);
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

    void DrawBox (Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        AddTri(a, b, c);
        AddTri(b, c, d);
    }

    void AddTri (Vector3 a, Vector3 b, Vector3 c, bool draw = false)
    {
        tris.Add(GetVertIndex(a));
        tris.Add(GetVertIndex(b));
        tris.Add(GetVertIndex(c));

        if (draw)
        {
            Debug.DrawLine(a, b);
            Debug.DrawLine(b, c);
            Debug.DrawLine(c, a);
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class LineMesh
{
    public List<Vector2> points = new();
    public float width;
    public bool loop = false;

    readonly List<Vector3> verts = new();
    readonly List<int> tris = new();
    public Mesh mesh;

    public Vector2 boxMin, boxMax;
    public float area;

    public void AddPoint (Vector2 point, int index)
    {
        if (index == points.Count || points.Count == 0)
        {
            points.Add(point);
        }
        else
        {
            points.Insert(points.Count - 1, point);
        }

        if (points.Count == 1)
        {
            var widthDelta = Vector2.one * width/2;
            boxMin = point - widthDelta;
            boxMax = point + widthDelta;
        }
        else
        {
            ExtendBox(point);
        }
    }

    public void PurgePoints()
    {
        Vector2 lastPoint = Vector2.zero;

        for (var i = 0; i < points.Count; i++)
        {
            var point = points[i];

            if (i > 0 && point == lastPoint)
            {
                points.RemoveAt(i);
                i--;
            }

            lastPoint = point;
        }
    }

    Vector2 FindNextUnique(int index, out int nextIndex)
    {
        var point = points[index];

        for (var i = index; i < points.Count; i++)
        {
            var nextPoint = points[i];
            if (nextPoint != point)
            {
                nextIndex = i;
                return nextPoint;
            }
        }

        nextIndex = index;
        return point;
    }

    Vector2 FindPrevUnique(int index)
    {
        var point = points[index];

        for (var i = index - 1; i >= 0; i--)
        {
            var prevPoint = points[i];
            if (prevPoint != point)
            {
                return prevPoint;
            }
        }

        return point;
    }

    public void SetWidth (float newWidth)
    {
        var widthDiff = newWidth - width;

        var boxDelta = Vector2.one * widthDiff / 2;
        boxMin -= boxDelta;
        boxMax += boxDelta;
        width = newWidth;
    }

    public void NewMesh(int endRes, int cornerRes, bool debugLines = false)
    {
        verts.Clear();
        tris.Clear();

        if (points.Count == 0) return;

        if (!mesh) mesh = new();

        //PurgePoints();

        for (var i = 0; i < points.Count; i++)
        {
            var point = points[i];

            var a = FindPrevUnique(i);
            var c = FindNextUnique(i, out var newIndex);

            if (a == point && c == point)
            {
                EndGeometry(point, point + Vector2.up, endRes, false, debugLines);
                EndGeometry(point, point - Vector2.up, endRes, false, debugLines);
            }

            if (newIndex > i + 1) i = newIndex - 1;

            if (a == point)
            {
                if (loop)
                {
                    a = points[^1];
                }
                else
                {
                    EndGeometry(point, c, endRes, true, debugLines);
                    continue;
                }
            }

            if (point == c)
            {
                if (loop)
                {
                    c = points[0];
                }
                else
                {
                    EndGeometry(point, a, endRes, true, debugLines);
                    break;
                }
            }

            CornerGeometry(a, point, c, cornerRes, debugLines);
        }

        mesh.triangles = new int[] { };
        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
    }

    void EndGeometry(Vector2 end, Vector2 other, int res, bool midBlock = true, bool debugLines = false)
    {
        Vector3 centerVert = end;

        Vector3 dir = (end - other).normalized;

        var startDelta = Vector2.Perpendicular(dir) * width / 2;

        var extVertCount = res * 2;

        var angle = 180f / extVertCount;

        var lastPoint = Vector3.zero;

        for (var l = 0; l < extVertCount + 1; l++)
        {
            var newPoint = Quaternion.AngleAxis(angle * l, -Vector3.forward) * startDelta + centerVert;

            if (l > 0)
            {
                AddTri(lastPoint, newPoint, centerVert, debugLines);
            }

            lastPoint = newPoint;
        }

        if (midBlock)
        {
            GetMidPoints(end, other, out var aBEdgeA, out var aBEdgeB);
            var edgeDelta = Vector2.Perpendicular(dir).normalized * width / 2;
            AddTri(centerVert, end + edgeDelta, aBEdgeA, debugLines);
            AddTri(centerVert, end - edgeDelta, aBEdgeB, debugLines);
            AddTri(centerVert, aBEdgeA, aBEdgeB, debugLines);
        }
    }

    void CornerGeometry(Vector2 a, Vector2 b, Vector2 c, int res, bool lines = false)
    {
        var toA = (a - b).normalized;
        var toC = (c - b).normalized;

        if (toA == -toC)
        {
            GetMidPoints(a, b, out var w, out var x);
            GetMidPoints(b, c, out var y, out var z);
            QuadGeometry(w, x, y, z, lines);
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
            EndGeometry(b, furthestEnd, res, true, lines);
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
        if (lines)
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
        var innerTangentA = (b - tangentPointA) + b;
        QuadGeometry(tangentPointA, aBEdgeA, innerTangentA, aBEdgeB, lines);

        GetMidPoints(b, c, out var bCEdgeA, out var bCEdgeB);
        if (flip)
        {
            var temp = bCEdgeA;
            bCEdgeA = bCEdgeB;
            bCEdgeB = temp;
        }
        var innerTangentC = (b - tangentPointC) + b;
        QuadGeometry(tangentPointC, bCEdgeA, innerTangentC, bCEdgeB, lines);

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

    void GetMidPoints(Vector2 pointA, Vector2 pointB, out Vector2 edgeA, out Vector2 edgeB)
    {
        var dir = pointA - pointB;
        var middle = (pointA + pointB) / 2;
        var edgeDelta = Vector2.Perpendicular(dir).normalized * width / 2;
        edgeA = middle + edgeDelta;
        edgeB = middle - edgeDelta;
    }

    void QuadGeometry(Vector2 a, Vector2 b, Vector2 c, Vector2 d, bool draw = false)
    {
        AddTri(a, b, c, draw);
        AddTri(b, c, d, draw);
    }

    void AddTri(Vector3 a, Vector3 b, Vector3 c, bool draw = false)
    {
        tris.Add(GetVertIndex(a));
        tris.Add(GetVertIndex(b));
        tris.Add(GetVertIndex(c));

        if (draw)
        {
            Debug.DrawLine(a, b, Color.yellow);
            Debug.DrawLine(b, c, Color.yellow);
            Debug.DrawLine(c, a, Color.yellow);
        }
    }

    int GetVertIndex(Vector3 vert)
    {
        var index = verts.IndexOf(vert);
        if (index < 0)
        {
            verts.Add(vert);
            return verts.Count - 1;
        }

        return index;
    }

    public void ExtendBox(Vector2 point, bool debugLines = false)
    {
        var radius = this.width / 2;

        if (point.x - radius < boxMin.x) boxMin.x = point.x - radius;
        if (point.y - radius < boxMin.y) boxMin.y = point.y - radius;

        if (point.x + radius > boxMax.x) boxMax.x = point.x + radius;
        if (point.y + radius > boxMax.y) boxMax.y = point.y + radius;

        CalculateArea();

        DrawBox();
    }

    /// <summary>
    /// Only to be used when unsure of past points
    /// </summary>
    public void CalculateBox()
    {
        float minX = Mathf.Infinity;
        float minY = Mathf.Infinity;
        float maxX = -Mathf.Infinity;
        float maxY = -Mathf.Infinity;

        for (var i = 0; i < points.Count; i++)
        {
            var point = points[i];

            if (point.x < minX) minX = point.x;
            if (point.y < minY) minY = point.y;

            if (point.x > maxX) maxX = point.x;
            if (point.y > maxY) maxY = point.y;
        }

        var radiusVector = Vector2.one * width / 2;

        boxMin = new(minX, minY);
        boxMin -= radiusVector;
        boxMax = new(maxX, maxY);
        boxMax += radiusVector;

        CalculateArea();
    }

    public bool TestBox(Vector2 point, bool debugLines = false)
    {
        if (debugLines) DrawBox();

        if (point.x < boxMin.x ||
            point.y < boxMin.y ||
            point.x > boxMax.x ||
            point.y > boxMax.y)
        {
            return false;
        }

        return true;
    }

    void CalculateArea()
    {
        var dimensions = boxMax - boxMin;
        area = dimensions.x * dimensions.y;
    }

    public void DrawBox()
    {
        var color = Color.blue;
        var topLeft = new Vector2(boxMin.x, boxMax.y);
        var bottomRight = new Vector2(boxMax.x, boxMin.y);
        Debug.DrawLine(boxMin, topLeft, color);
        Debug.DrawLine(topLeft, boxMax, color);
        Debug.DrawLine(boxMax, bottomRight, color);
        Debug.DrawLine(bottomRight, boxMin, color);
    }
}

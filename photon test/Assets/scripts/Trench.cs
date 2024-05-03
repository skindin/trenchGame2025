using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trench : MonoBehaviour
{
    public LineRenderer line;
    public float width, totalLength = 0, area = 0; //length discludes width
    public Vector2 boxMin, boxMax;
    public TrenchAgent agent;
    public bool calculateLength;
    public List<TrenchChunk> chunks = new();

    private void Awake()
    {
    }

    private void Update()
    {
        if (calculateLength) CalculateLength();
    }

    public void DrawBox ()
    {
        var color = Color.blue;
        var topLeft = new Vector2(boxMin.x, boxMax.y);
        var bottomRight = new Vector2(boxMax.x, boxMin.y);
        Debug.DrawLine(boxMin, topLeft, color);
        Debug.DrawLine(topLeft, boxMax, color);
        Debug.DrawLine(boxMax, bottomRight, color);
        Debug.DrawLine(bottomRight, boxMin, color);
    }

    public void AddPoint(Vector2 point)
    {
        bool hadOne = false;
        if (line.positionCount == 0)
        {
            boxMin = -Vector2.one * width / 2 + point;
            boxMax = Vector2.one * width / 2 + point;
            hadOne = true;
        }

        if (line.positionCount > 1)
        {
            if (!calculateLength)
            {
                Vector2 lastPoint = line.GetPosition(line.positionCount - 1);
                var segLength = Vector2.Distance(lastPoint, point);
                totalLength += segLength;
            }
            else
            {
                CalculateLength();
            }
        }

        int currentPointCount = line.positionCount;
        line.positionCount = currentPointCount + 1;
        line.SetPosition(currentPointCount, point);

        ExtendBox(point);

        if (hadOne)
        {
            AddPoint(point); //line renderer needs atleast 2 points to render
        }
    }

    public void MoveEnd (Vector2 point)
    {

        if (line.positionCount < 2) return;

        if (!calculateLength)
        {

            var secLastPoint = line.GetPosition(line.positionCount - 2);
            var lastPoint = line.GetPosition(line.positionCount - 1);

            var prevLength = Vector2.Distance(secLastPoint, lastPoint);
            var newLength = Vector2.Distance(secLastPoint, point);

            var distDiff = newLength - prevLength;

            totalLength += distDiff;
        }
        else
        {
            CalculateLength();
        }

        line.SetPosition(line.positionCount - 1, point);

        if (TrenchManager.instance.debugLines)
            DrawBox();

        ExtendBox(point);
    }

    public void IncreaseWidth (float increase)
    {
        line.widthMultiplier = width += increase;

        boxMin += -Vector2.one / 2 * increase;
        boxMax += Vector2.one / 2 * increase;

        CalculateArea();
    }

    public void SetWidth (float newWidth)
    {
        IncreaseWidth(newWidth - width);
    }

    public void ExtendBox (Vector2 point)
    {
        var radius = this.width / 2;

        if (point.x - radius < boxMin.x) boxMin.x = point.x - radius;
        if (point.y - radius < boxMin.y) boxMin.y = point.y - radius;

        if (point.x + radius > boxMax.x) boxMax.x = point.x + radius;
        if (point.y + radius > boxMax.y) boxMax.y = point.y + radius;

        CalculateArea();
    }

    public bool TestBox(Vector2 point)
    {
        if (point.x < boxMin.x || 
            point.y < boxMin.y || 
            point.x > boxMax.x || 
            point.y > boxMax.y)
        {
            return false;
        }

        return true;
    }

    public void CalculateArea ()
    {
        var dimensions = boxMax - boxMin;
        area = dimensions.x * dimensions.y;
    }

    public void CalculateLength ()
    {
        float totalLength = 0;

        Vector2 pointA = line.GetPosition(0);
        Vector2 pointB;


        for (var i = 1; i < line.positionCount; i++)
        {
            pointB = line.GetPosition(i);

            var length = Vector2.Distance(pointA, pointB);

            totalLength += length;

            pointA = pointB;
        }

        this.totalLength = totalLength;
    }

    public void CalculateBox ()
    {
        float minX = Mathf.Infinity;
        float minY = Mathf.Infinity;
        float maxX = -Mathf.Infinity;
        float maxY = -Mathf.Infinity;

        for (var i = 0; i < line.positionCount; i++)
        {
            var point = line.GetPosition(i);

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
}

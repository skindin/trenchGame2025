using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trench : MonoBehaviour
{
    public LineRenderer line;
    public float width, totalLength = 0; //length discludes width
    public Vector2 boxTopLeft, boxBottomRight;
    public TrenchAgent agent;
    public bool calculateLength;

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
        var topRight = new Vector2(boxBottomRight.x, boxTopLeft.y);
        var bottomLeft = new Vector2(boxTopLeft.x, boxBottomRight.y);
        Debug.DrawLine(boxTopLeft, topRight, color);
        Debug.DrawLine(topRight, boxBottomRight, color);
        Debug.DrawLine(boxBottomRight, bottomLeft, color);
        Debug.DrawLine(bottomLeft, boxTopLeft, color);
    }

    public void AddPoint(Vector2 point)
    {
        if (line.positionCount == 0)
        {
            boxTopLeft = new Vector2(-1, 1) * width / 2 + point;
            boxBottomRight = new Vector2(1, -1) * width / 2 + point;
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
    }

    public void IncreaseWidth (float increase)
    {
        line.widthMultiplier = width += increase;

        boxTopLeft += new Vector2(-1, 1) / 2 * increase;
        boxBottomRight += new Vector2(1, -1) / 2 * increase;
    }

    public void SetWidth (float newWidth)
    {
        IncreaseWidth(newWidth - width);
    }

    public void ExtendBox (Vector2 point)
    {
        var radius = this.width / 2;

        if (point.x - radius < boxTopLeft.x) boxTopLeft.x = point.x - radius;
        if (point.y + radius > boxTopLeft.y) boxTopLeft.y = point.y + radius;

        if (point.x + radius > boxBottomRight.x) boxBottomRight.x = point.x + radius;
        if (point.y - radius < boxBottomRight.y) boxBottomRight.y = point.y - radius;
    }

    public bool TestBox(Vector2 point)
    {
        if (point.x < boxTopLeft.x || 
            point.y > boxTopLeft.y || 
            point.x > boxBottomRight.x || 
            point.y < boxBottomRight.y)
        {
            return false;
        }

        return true;
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
}

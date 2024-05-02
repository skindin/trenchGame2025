using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trench : MonoBehaviour
{
    public LineRenderer line;
    public float width;
    public Vector2 boxTopLeft, boxBottomRight;
    public TrenchAgent agent;

    private void Awake()
    {
    }

    private void Update()
    {
        DrawBox();
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

        int currentPointCount = line.positionCount;

        // Increase the position count by one
        line.positionCount = currentPointCount + 1;

        // Set the position of the new point
        line.SetPosition(currentPointCount, point);

        ExtendBox(point);
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

    //public Vector2 GetBoxDelta(Vector2 point)
    //{
    //    Vector2 delta = Vector2.zero;

    //    if (point.x < boxTopLeft.x) delta.x = boxTopLeft.x - point.x;
    //    if (point.y > boxTopLeft.y) delta.y = point.y - boxTopLeft.y;

    //    if (point.x > boxBottomRight.x) delta.x = point.x - boxBottomRight.x;
    //    if (point.y < boxBottomRight.y) delta.y = boxBottomRight.y - point.y;

    //    return delta;
    //}

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
}

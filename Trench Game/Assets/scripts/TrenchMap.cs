using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrenchMap
{
    public MapBlock[,] blocks;
    public int size;

    public TrenchMap (int size)
    {
        blocks = new MapBlock[size,size];
        this.size = size;
    }


    /// <summary>
    /// Must convert points and radii to map's local space!
    /// </summary>
    /// <param name="pointA"></param>
    /// <param name="radA"></param>
    /// <param name="pointB"></param>
    /// <param name="radB"></param>
    public void DrawLine(Vector2 pointA, float radA, Vector2 pointB, float radB)
    {
        for (int blockY = 0; blockY < size; blockY++)
        {
            for (int blockX = 0; blockX < size; blockX++)
            {
                var block = blocks[blockX, blockY];

                if (block.IsFull()) continue;

                for (int bitY = 0; bitY < 4; bitY++)
                {
                    var y = blockY * 4 + bitY;

                    for (int bitX = 0; bitX < 4; bitX++)
                    {
                        var x = blockX * 4 + bitX;

                        var fill = TestPointWithLine(new(x, y), pointA, radA, pointB, radB);
                        if (fill)
                            block.SetPoint(true, bitX, bitY);
                    }
                }
            }
        }
    }

    public void DrawPoint (Vector2 point, float radius)
    {
        DrawLine(point, radius, point, radius);
    }

    public bool TestPointWithLine (Vector2 testPoint, Vector2 pointA, float radA, Vector2 pointB, float radB)
    {
        var edgeDist = Vector2.Distance(pointA, pointB) - radA - radB;
        if (edgeDist <= 0)
        {
            Vector2 biggestPoint;
            float biggestRadii;

            if (radA > radB)
            {
                biggestPoint = pointA;
                biggestRadii = radA;
            }
            else
            {
                biggestPoint = pointB;
                biggestRadii = radB;
            }

            var testDist = Vector2.Distance(testPoint, biggestPoint);

            return testDist <= biggestRadii;
        }

        var closestPoint = GeoFuncs.ClosestPointToLineSegment(testPoint, pointA, pointB, out var ratio);
        var dist = Vector2.Distance(closestPoint, testPoint);

        if (radA == radB)
        {
            return dist <= radA;
        }

        var deltaRadius = radB - radA;
        var radius = ratio * deltaRadius + radA;

        return dist <= radius;
    }
}

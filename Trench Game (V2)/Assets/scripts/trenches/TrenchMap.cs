using Google.Protobuf.WellKnownTypes;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Rendering;

public class TrenchMap : MonoBehaviour
{
    public Sprite sprite;
    public SpriteRenderer spriteRenderer;
    public MapBlock[,] blocks;
    public int resolution = 8;
    public float scale = 1;
    public bool drawMap = true, debugLines = false;
    public Transform pointA, pointB;

    private void Update()
    {
        if (drawMap)
        {
            DrawMap();
        }
    }

    public void DrawMap ()
    {
        blocks = null;

        GeoUtils.DrawCircle(pointA.position, pointA.localScale.x, Color.green);
        GeoUtils.DrawCircle(pointB.position, pointB.localScale.x, Color.green);
        Debug.DrawLine(pointA.position, pointB.position, Color.green);
        //var perpendicular = Vector2.Perpendicular(pointA.position - pointB.position).normalized;

        //Debug.DrawLine(
        //    (Vector2)pointA.position + perpendicular * pointA.localScale.x,
        //    (Vector2)pointB.position + perpendicular * pointB.localScale.x,
        //    Color.green);
        //Debug.DrawLine(
        //    (Vector2)pointA.position - perpendicular * pointA.localScale.x,
        //    (Vector2)pointB.position - perpendicular * pointB.localScale.x,
        //    Color.green);

        DigTaperedCapsule(pointA.position, pointA.localScale.x, pointB.position, pointB.localScale.x, debugLines);

        //if (blocks == null)
        //    return;
    }

    public void SetTaperedCapsule(Vector2 startPoint, float startRadius, Vector2 endPoint, float endRadius, bool value, bool debugLines = false)
    {
        float blockWidth = scale / resolution; // Width of each MapBlock
        float bitWidth = blockWidth / 4f;      // Width of each bit in a MapBlock

        for (int blockY = 0; blockY < resolution; blockY++)
        {
            for (int blockX = 0; blockX < resolution; blockX++)
            {
                var block = blocks[blockX, blockY];

                // Create a block if it doesn't exist and the value is being set to true
                if (block == null)
                {
                    if (value)
                        block = new MapBlock();
                    else
                        continue;
                }

                // Compute the position of the block's center in world space
                var boxPos = (new Vector2(blockX + 0.5f, blockY + 0.5f) - Vector2.one * resolution * 0.5f) * blockWidth
                    + (Vector2)transform.position;

                // If the block already fully matches the target value, skip processing
                if (block.TestWhole(value))
                {
                    if (debugLines)
                    {
                        GeoUtils.DrawBoxPosSize(boxPos, Vector2.one * blockWidth, Color.white);
                    }
                    continue;
                }

                // Check if the block overlaps with the tapered capsule
                if (!GeoUtils.TestBoxTouchesTaperedCapsule(boxPos, Vector2.one * blockWidth, startPoint, startRadius, endPoint, endRadius,
                    debugLines))
                {
                    if (debugLines)
                    {
                        GeoUtils.DrawBoxPosSize(boxPos, Vector2.one * blockWidth, Color.red);
                    }
                    continue; // Skip if the block doesn't touch the capsule
                }

                // Debug visualization for blocks that touch the capsule
                if (debugLines)
                {
                    GeoUtils.DrawBoxPosSize(boxPos, Vector2.one * blockWidth * .9f, Color.green);
                }

                // Get the 4x4 bit array from the block
                var blockArray = block.GetArray();
                int totalBitsAtValue = 0;

                for (int bitY = 0; bitY < 4; bitY++)
                {
                    for (int bitX = 0; bitX < 4; bitX++)
                    {
                        var bitValue = blockArray[bitX, bitY];

                        // Skip if this bit already has the desired value
                        if (bitValue == value)
                        {
                            totalBitsAtValue++;
                            continue;
                        }

                        // Compute the position of the bit in world space
                        var bitPos = boxPos + new Vector2((bitX - 1.5f) * bitWidth, (bitY - 1.5f) * bitWidth);

                        // Test if the bit is within the capsule
                        if (GeoUtils.TestPointWithinTaperedCapsule(bitPos, startPoint, startRadius, endPoint, endRadius))
                        {
                            blockArray[bitX, bitY] = value;
                            totalBitsAtValue++;
                        }
                    }
                }

                // Remove the block if all bits are cleared and `value` is false
                if (!value && totalBitsAtValue >= 16)
                {
                    blocks[blockX, blockY] = null;
                }
                else
                {
                    blocks[blockX, blockY] = block;
                    block.SetArray(blockArray);
                }
            }
        }
    }

    public void DigTaperedCapsule(Vector2 startPoint, float startRadius, Vector2 endPoint, float endRadius, bool debugLines = false)
    {
        if (blocks == null)
        {
            blocks = new MapBlock[resolution,resolution];
        }

        SetTaperedCapsule(startPoint, startRadius, endPoint, endRadius, true, debugLines);
    }

    public void FillTaperedCapsule (Vector2 startPoint, float startRadius, Vector2 endPoint, float endRadius, bool debugLines = false)
    {
        if (blocks == null) //if there's no dug points, no need to calculate anything lol
            return;

        SetTaperedCapsule(startPoint, startRadius, endPoint, endRadius, false, debugLines);
    }
}

using Google.Protobuf.WellKnownTypes;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Rendering;

public class TrenchMap : MonoBehaviour
{
    //public Sprite sprite;
    //public SpriteRenderer spriteRenderer;
    public MapBlock[,] blocks;
    public int resolution = 8;
    public float scale = 1;
    public bool drawMap = true, debugLines = false;
    public Transform pointA, pointB;
    public Vector2 pos;
    public bool testValue = true;
    public Color trenchColor = Color.gray, groundColor = Color.clear;
    public Mesh imageMesh;
    public Material imageMaterial;
    public Texture2D imageTexture;

    private void Awake()
    {
        imageMaterial = new Material(imageMaterial);

        imageTexture = new Texture2D(resolution * 4, resolution * 4);

        imageMaterial.mainTexture = imageTexture;
    }

    private void Update()
    {
        if (drawMap || debugLines) //shrigging emoji
        {
            DrawMap();
        }
    }

    public void DrawMap ()
    {
        //blocks = null;

        //DrawCapsule();
        //var perpendicular = Vector2.Perpendicular(pointA.position - pointB.position).normalized;

        //Debug.DrawLine(
        //    (Vector2)pointA.position + perpendicular * pointA.localScale.x,
        //    (Vector2)pointB.position + perpendicular * pointB.localScale.x,
        //    Color.green);
        //Debug.DrawLine(
        //    (Vector2)pointA.position - perpendicular * pointA.localScale.x,
        //    (Vector2)pointB.position - perpendicular * pointB.localScale.x,
        //    Color.green);

        if (testValue)
            DigTaperedCapsule(pointA.position, pointA.localScale.x, pointB.position, pointB.localScale.x, debugLines);
        else
            FillTaperedCapsule(pointA.position, pointA.localScale.x, pointB.position, pointB.localScale.x, debugLines);

        //if (blocks == null)
        //    return;
    }

    void DrawCapsule()
    {
        GeoUtils.DrawCircle(pointA.position, pointA.localScale.x, Color.green);
        GeoUtils.DrawCircle(pointB.position, pointB.localScale.x, Color.green);
        Debug.DrawLine(pointA.position, pointB.position, Color.green);
    }

    public void SetTaperedCapsule(Vector2 startPoint, float startRadius, Vector2 endPoint, float endRadius, bool value, bool debugLines = false)
    {
        float blockWidth = scale / resolution; // Width of each MapBlock
        float bitWidth = blockWidth / 4f;      // Width of each bit in a MapBlock

        var startMax = Vector2.one * startRadius;
        var endMax = Vector2.one * endRadius;

        var mapMin = pos - scale / 2 * Vector2.one;
        var mapMax = pos + scale / 2 * Vector2.one;

        var capsuleMin = Vector2.Min(startPoint - startMax, endPoint - endMax);
        capsuleMin = Vector2.Max(capsuleMin, mapMin);

        var capsuleMax = Vector2.Max(startPoint + startMax, endPoint + endMax);
        capsuleMax = Vector2.Min(capsuleMax, mapMax);

        var halfVector2One = Vector2.one * .5f;

        var startPos = Vector2Int.FloorToInt(((capsuleMin - pos) / blockWidth) + (resolution * halfVector2One));
        startPos = Vector2Int.Max(startPos, Vector2Int.zero);

        var endPos = Vector2Int.CeilToInt(((capsuleMax - pos) / blockWidth) + (resolution * halfVector2One));

        //GeoUtils.DrawBoxMinMax(startPos, endPos, Color.magenta);

        bool somethingChanged = true;
        //honestly probably better to switch between setting all the pixels and some of them depending on how many

        Color32[] pixels = new Color32[resolution * 4 * resolution * 4];

        for (int i = 0; i < resolution * 4 * resolution * 4; i++)
        {
            var color = Random.ColorHSV();
            pixels[i] = new((byte)color.r, (byte)color.g, (byte)color.b, (byte)color.a);
        }

        for (int blockY = startPos.y; blockY < endPos.y; blockY++)
        {
            for (int blockX = startPos.x; blockX < endPos.x; blockX++)
            {
                //pixels[blockX + blockY * resolution * 4] = new Color32(0,0,0,1);

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
                var boxPos = (new Vector2(blockX + 0.5f, blockY + 0.5f) - resolution * halfVector2One) * blockWidth
                    + pos;

                // If the block already fully matches the target value, skip processing
                if (block.TestWhole(value))
                {
                    if (drawMap)
                    {
                        GeoUtils.MarkPoint(boxPos, blockWidth/2, Color.white);
                    }
                    continue;
                }

                // Check if the block overlaps with the tapered capsule
                if (!GeoUtils.TestBoxTouchesTaperedCapsule(boxPos, Vector2.one * blockWidth, startPoint, startRadius, endPoint, endRadius,
                    debugLines))
                {
                    if (drawMap)
                    {
                        GeoUtils.MarkPoint(boxPos, blockWidth / 2, Color.red);
                        GeoUtils.DrawBoxPosSize(boxPos, Vector2.one * blockWidth, Color.red);
                    }
                    continue; // Skip if the block doesn't touch the capsule
                }

                // Debug visualization for blocks that touch the capsule
                if (drawMap)
                {
                    GeoUtils.DrawBoxPosSize(boxPos, Vector2.one * blockWidth, Color.green);
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
                            if (drawMap)
                            {
                                GeoUtils.MarkPoint(bitPos, bitWidth, Color.green);
                            }

                            somethingChanged = true;
                        }
                        else if (drawMap)
                        {
                            GeoUtils.MarkPoint(bitPos, bitWidth/2, Color.red);
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

        imageTexture.SetPixels32(pixels);

        imageTexture.Apply();

        Graphics.DrawMesh(imageMesh,Matrix4x4.identity,  imageMaterial,0);
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

    private void OnDrawGizmos()
    {
        if (drawMap || debugLines)
        {
            DrawCapsule();
        }
    }
}

//using Google.Protobuf.WellKnownTypes;
using System.Collections;
using System.Collections.Generic;
//using System.Net;
using UnityEngine;
using System;
using System.Net;
//using static Unity.Collections.AllocatorManager;
//using UnityEngine.Profiling;
//using UnityEngine.Rendering;

public class TrenchMap
{
    //public Sprite sprite;
    //public SpriteRenderer spriteRenderer;
    public MapBlock[,] blocks;
    public int resolution = 8;
    public float scale = 1;
    //public Transform pointA, pointB;
    public Vector2 pos;
    public Color32 trenchColor = new(255,255,255,255), groundColor = new(255, 255, 255, 255);
    public Mesh imageMesh;
    public Material imageMaterial;//
    public Texture2D imageTexture;
    Color32[] pixels;
    public
        int totalEditedBlocks = 0;
    public bool empty = true;

    public TrenchMap (int resolution, float scale, Color32 trenchColor, Color32 groundColor, Vector2 pos, Mesh imageMesh, Material imageMaterial, FilterMode filter)
    {
        this.resolution = resolution;

        this.scale = scale;

        this.pos = pos;

        this.trenchColor = trenchColor;
        this.groundColor = groundColor;

        imageTexture = new Texture2D(resolution * 4, resolution * 4);

        this.imageMaterial = new(imageMaterial);

        this.imageMaterial.mainTexture = imageTexture;

        this.imageMesh = imageMesh;

        blocks = new MapBlock[resolution, resolution];

        pixels = new Color32[resolution * 4 * 4 * resolution];

        imageTexture.filterMode = filter;
        imageTexture.SetPixels32(pixels);
        imageTexture.Apply();
    }

    //private void Awake()
    //{
    //    //imageMaterial = new Material(imageMaterial);

    //    imageTexture = new Texture2D(resolution * 4, resolution * 4);

    //    imageMaterial.mainTexture = imageTexture;

    //    pixels = new Color32[resolution * 4 * 4 * resolution];
    //}

    //private void Update()
    //{
    //    //if (runTest) //shrigging emoji
    //    //{
    //    //    DrawMap();
    //    //}
    //}

    //public void DrawMap ()
    //{
    //    //blocks = null;

    //    //DrawCapsule();
    //    //var perpendicular = Vector2.Perpendicular(pointA.position - pointB.position).normalized;

    //    //Debug.DrawLine(
    //    //    (Vector2)pointA.position + perpendicular * pointA.localScale.x,
    //    //    (Vector2)pointB.position + perpendicular * pointB.localScale.x,
    //    //    Color.green);
    //    //Debug.DrawLine(
    //    //    (Vector2)pointA.position - perpendicular * pointA.localScale.x,
    //    //    (Vector2)pointB.position - perpendicular * pointB.localScale.x,
    //    //    Color.green);

    //    //if (testValue)
    //    //    DigTaperedCapsule(pointA.position, pointA.localScale.x, pointB.position, pointB.localScale.x, debugLines);
    //    //else
    //    //    FillTaperedCapsule(pointA.position, pointA.localScale.x, pointB.position, pointB.localScale.x, debugLines);

    //    //if (blocks == null)
    //    //    return;
    //}

    //void DrawCapsule()
    //{
    //    GeoUtils.DrawCircle(pointA.position, pointA.localScale.x, Color.green);
    //    GeoUtils.DrawCircle(pointB.position, pointB.localScale.x, Color.green);
    //    Debug.DrawLine(pointA.position, pointB.position, Color.green);
    //}

    public void SetTaperedCapsule(Vector2 startPoint, float startRadius, Vector2 endPoint, float endRadius, bool value, out float areaChanged,
        bool debugLines = false)
    {
        //if (blocks == null)
        //{
        //    if (value)
        //    {
        //        blocks = new MapBlock[resolution, resolution];
        //    }
        //    else
        //    {
        //        return;
        //    }
        //}

        var manager = TrenchManager.Manager;

        float blockWidth = manager.GetBlockWidth(); // Width of each MapBlock
        float bitWidth = manager.GetBitWidth(blockWidth);      // Width of each bit in a MapBlock

        var startMax = Vector2.one * startRadius;
        var endMax = Vector2.one * endRadius;

        var mapMin = pos - scale / 2 * Vector2.one;
        var mapMax = pos + scale / 2 * Vector2.one;

        var capsuleMin = Vector2.Min(startPoint - startMax, endPoint - endMax);
        capsuleMin = Vector2.Max(capsuleMin, mapMin);

        var capsuleMax = Vector2.Max(startPoint + startMax, endPoint + endMax);
        capsuleMax = Vector2.Min(capsuleMax, mapMax);

        //var halfVector2One = Vector2.one * .5f;

        var startPos = manager.GetBlockAdressFloored(capsuleMin, pos, blockWidth); //Vector2Int.FloorToInt(((capsuleMin - pos) / blockWidth) + (resolution * halfVector2One));
        startPos = Vector2Int.Max(startPos, Vector2Int.zero);

        var endPos = manager.GetBlockAdressCield(capsuleMax, pos, blockWidth);
            //Vector2Int.CeilToInt(((capsuleMax - pos) / blockWidth) + (resolution * halfVector2One));

        //GeoUtils.DrawBoxMinMax(startPos, endPos, Color.magenta);
        areaChanged = 0;
        var bitArea = bitWidth * bitWidth;
        //honestly probably better to switch between setting all the pixels and some of them depending on how many

        //Color32[] pixels = new Color32[resolution * 4 * resolution * 4];

        //for (int i = 0; i < resolution * 4 * resolution * 4; i++)
        //{
        //    pixels[i] = groundColor;
        //}

        int totalBitsTested = 0;
        int totalBlocksTested = 0;

        for (int blockY = startPos.y; blockY < endPos.y; blockY++)
        {
            for (int blockX = startPos.x; blockX < endPos.x; blockX++)
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
                var blockPos = manager.GetBlockPos(pos, new(blockX, blockY),blockWidth);

                // If the block already fully matches the target value, skip processing
                if (block.TestWhole(value))
                {
                    if (debugLines)
                    {
                        GeoUtils.MarkPoint(blockPos, blockWidth/2, Color.white);
                    }

                    continue;
                }

                bool wasCompletelyFull = block.TestWhole(false);
                bool blockChanged = false;

                totalBlocksTested++;
                // Check if the block overlaps with the tapered capsule
                if (!GeoUtils.TestCirlceTouchesTaperedCapsule(blockPos, Vector2.one.magnitude * bitWidth * 1.5f, startPoint, startRadius, endPoint, endRadius,
                    debugLines))
                {
                    if (debugLines)
                    {
                        GeoUtils.MarkPoint(blockPos, blockWidth / 2, Color.red);
                        GeoUtils.DrawBoxPosSize(blockPos, Vector2.one * blockWidth, Color.red);
                    }
                    continue; // Skip if the block doesn't touch the capsule
                }

                // Debug visualization for blocks that touch the capsule
                if (debugLines)
                {
                    GeoUtils.DrawBoxPosSize(blockPos, Vector2.one * blockWidth, Color.green);
                }

                // Get the 4x4 bit array from the block
                //var blockArray = block.GetArray();
                int totalBitsAtValue = 0;

                //Profiler.BeginSample("TestCircleTouchesTaperedCapsule");

                for (int bitY = 0; bitY < 4; bitY++)
                {
                    for (int bitX = 0; bitX < 4; bitX++)
                    {
                        var bitValue = block[bitX,bitY];

                        totalBitsTested++;

                        // Skip if this bit already has the desired value
                        if (bitValue == value)
                        {
                            totalBitsAtValue++;
                            continue;
                        }

                        // Compute the position of the bit in world space
                        var bitPos = manager.GetBitPos(blockPos, bitWidth, new(bitX,bitY));

                        //if (!GeoUtils.TestBoxMinMax(capsuleMin, capsuleMax, bitPos, debugLines))
                        //    continue;

                        // Test if the bit is within the capsule
                        if (GeoUtils.TestPointWithinTaperedCapsule(bitPos, startPoint, startRadius, endPoint, endRadius))
                        {
                            block[bitX,bitY] = value;
                            totalBitsAtValue++;
                            if (debugLines)
                            {
                                GeoUtils.MarkPoint(bitPos, bitWidth, Color.green);
                            }

                            areaChanged += bitArea;

                            blockChanged = true;

                            var arrayIndex = bitY * resolution * 4 + bitX + blockY * 16 * resolution + blockX * 4;

                            pixels[arrayIndex] = value ? trenchColor : groundColor;
                        }
                        else if (debugLines)
                        {
                            GeoUtils.MarkPoint(bitPos, bitWidth/2, Color.red);
                        }
                    }
                }

                if (wasCompletelyFull && blockChanged)
                {
                    totalEditedBlocks++;
                }

                //Profiler.EndSample();

                // Remove the block if all bits are cleared and `value` is false
                if (!value && totalBitsAtValue >= 16)
                {
                    blocks[blockX, blockY] = null;
                    totalEditedBlocks--;
                }
                else
                {
                    blocks[blockX, blockY] = block;
                    //block.SetArray(blockArray);
                }
            }
        }

        empty = totalEditedBlocks == 0;

        //if (logBitsTested)
        //{
        //    Debug.Log($"tested {totalBlocksTested} blocks and {totalBitsTested} bits");
        //}

        if (areaChanged > 0)
        {
            imageTexture.SetPixels32(pixels);

            imageTexture.Apply();
        }
    }

    public bool TestCircleTouchesValue(Vector2 circlePos, float circleRadius, bool value, bool debugLines = false)
    {
        var blockWidth = TrenchManager.Manager.GetBlockWidth();
        var bitWidth = TrenchManager.Manager.GetBitWidth(blockWidth);

        var mapMin = pos - scale / 2 * Vector2.one;
        var mapMax = pos + scale / 2 * Vector2.one;

        var circleMin = circlePos - (circleRadius * Vector2.one);
        circleMin = Vector2.Max(circleMin, mapMin);

        var circleMax = circlePos + (circleRadius * Vector2.one);
        circleMax = Vector2.Min(circleMax, mapMax);

        //var halfVector2One = Vector2.one * .5f;

        var startPos = TrenchManager.Manager.GetBlockAdressFloored(circleMin, pos, blockWidth); //Vector2Int.FloorToInt(((capsuleMin - pos) / blockWidth) + (resolution * halfVector2One));
        startPos = Vector2Int.Max(startPos, Vector2Int.zero);

        var endPos = TrenchManager.Manager.GetBlockAdressCield(circleMax, pos, blockWidth);

        for (var blockY = startPos.y; blockY < endPos.y; blockY++)
        {
            for (var blockX = startPos.x; blockX < endPos.x; blockX++)
            {
                var block = blocks[blockX, blockY];

                var blockPos = TrenchManager.Manager.GetBlockPos(pos, new Vector2Int(blockX, blockY), blockWidth);

                if (block == null)
                {
                    if (!value && GeoUtils.TestBoxPosSizeTouchesCircle(blockPos, blockWidth * Vector2.one, circlePos, circleRadius, debugLines))
                    {
                        return true;
                    }
                    else
                    {
                        continue;
                    }
                }

                if (block.TestWhole(!value))
                {
                    if (debugLines)
                        GeoUtils.DrawBoxPosSize(blockPos, blockWidth * Vector2.one,Color.green);
                    continue;
                }

                for (var bitY = 0; bitY < 4; bitY++)
                {
                    for (var bitX = 0; bitX < 4; bitX++)
                    {
                        var bitPos = TrenchManager.Manager.GetBitPos(blockPos, bitWidth, new(bitX, bitY));

                        if (debugLines)
                        {
                            GeoUtils.DrawBoxPosSize(bitPos, bitWidth * Vector2.one, Color.green);
                        }

                        var dist = (bitPos - circlePos).magnitude;

                        if (dist <= circleRadius && block[bitX,bitY] == value)
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    public MapBlock GetBlock (Vector2Int adress)
    {
        return blocks[adress.x, adress.y];
    }

    public void Draw ()
    {
        var transform = Matrix4x4.TRS(pos, Quaternion.identity, Vector2.one * scale);

        Graphics.DrawMesh(imageMesh, transform, imageMaterial, 0);
    }

    //private void OnDrawGizmos()
    //{
    //    if (debugLines)
    //    {
    //        DrawCapsule();
    //    }
    //}
}

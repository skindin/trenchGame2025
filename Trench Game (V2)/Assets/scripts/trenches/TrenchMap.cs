//using Google.Protobuf.WellKnownTypes;
using System.Collections;
using System.Collections.Generic;
//using System.Net;
using UnityEngine;
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

    public TrenchMap (int resolution, float scale, Color32 trenchColor, Color32 groundColor, Vector2 pos, Mesh imageMesh, Material imageMaterial)
    {
        this.resolution = resolution;

        this.scale = scale;

        this.pos = pos;

        this.trenchColor = trenchColor;
        this.groundColor = groundColor;

        imageTexture = new Texture2D(resolution * 4, resolution * 4);

        this.imageMaterial = imageMaterial;

        this.imageMaterial.mainTexture = imageTexture;

        this.imageMesh = imageMesh;

        blocks = new MapBlock[resolution, resolution];

        pixels = new Color32[resolution * 4 * 4 * resolution];
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

    public void SetTaperedCapsule(Vector2 startPoint, float startRadius, Vector2 endPoint, float endRadius, bool value, bool debugLines = false)
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

        bool somethingChanged = false;
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
                var boxPos = (new Vector2(blockX + 0.5f, blockY + 0.5f) - resolution * halfVector2One) * blockWidth
                    + pos;

                // If the block already fully matches the target value, skip processing
                if (block.TestWhole(value))
                {
                    if (debugLines)
                    {
                        GeoUtils.MarkPoint(boxPos, blockWidth/2, Color.white);
                    }
                    continue;
                }

                totalBlocksTested++;
                // Check if the block overlaps with the tapered capsule
                if (!GeoUtils.TestCirlceTouchesTaperedCapsule(boxPos, Vector2.one.magnitude * bitWidth * 1.5f, startPoint, startRadius, endPoint, endRadius,
                    debugLines))
                {
                    if (debugLines)
                    {
                        GeoUtils.MarkPoint(boxPos, blockWidth / 2, Color.red);
                        GeoUtils.DrawBoxPosSize(boxPos, Vector2.one * blockWidth, Color.red);
                    }
                    continue; // Skip if the block doesn't touch the capsule
                }

                // Debug visualization for blocks that touch the capsule
                if (debugLines)
                {
                    GeoUtils.DrawBoxPosSize(boxPos, Vector2.one * blockWidth, Color.green);
                }

                // Get the 4x4 bit array from the block
                var blockArray = block.GetArray();
                int totalBitsAtValue = 0;

                //Profiler.BeginSample("TestCircleTouchesTaperedCapsule");

                for (int bitY = 0; bitY < 4; bitY++)
                {
                    for (int bitX = 0; bitX < 4; bitX++)
                    {
                        var bitValue = blockArray[bitX, bitY];

                        totalBitsTested++;

                        // Skip if this bit already has the desired value
                        if (bitValue == value)
                        {
                            totalBitsAtValue++;
                            continue;
                        }

                        // Compute the position of the bit in world space
                        var bitPos = boxPos + new Vector2((bitX - 1.5f) * bitWidth, (bitY - 1.5f) * bitWidth);

                        //if (!GeoUtils.TestBoxMinMax(capsuleMin, capsuleMax, bitPos, debugLines))
                        //    continue;

                        // Test if the bit is within the capsule
                        if (GeoUtils.TestPointWithinTaperedCapsule(bitPos, startPoint, startRadius, endPoint, endRadius))
                        {
                            blockArray[bitX, bitY] = value;
                            totalBitsAtValue++;
                            if (debugLines)
                            {
                                GeoUtils.MarkPoint(bitPos, bitWidth, Color.green);
                            }

                            somethingChanged = true;

                            var arrayIndex = bitY * resolution * 4 + bitX + blockY * 16 * resolution + blockX * 4;

                            pixels[arrayIndex] = value ? trenchColor : groundColor;
                        }
                        else if (debugLines)
                        {
                            GeoUtils.MarkPoint(bitPos, bitWidth/2, Color.red);
                        }
                    }
                }

                //Profiler.EndSample();

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

        //if (logBitsTested)
        //{
        //    Debug.Log($"tested {totalBlocksTested} blocks and {totalBitsTested} bits");
        //}

        if (somethingChanged)
        {
            imageTexture.SetPixels32(pixels);

            imageTexture.Apply();
        }
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

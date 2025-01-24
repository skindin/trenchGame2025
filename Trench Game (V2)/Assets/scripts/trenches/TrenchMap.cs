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
    public readonly MapBlock[,] blocks;
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
        int totalTrenchCells = 0;
    public bool allFull = true, allTrench = false;

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

    public void SetTaperedCapsule(Vector2 startPoint, float startRadius, Vector2 endPoint, float endRadius, bool value, float maxArea, out float areaChanged,
        bool debugLines = false, bool logCounters = false)
    {
        areaChanged = 0;

        if (maxArea == 0)
        {
            return;
        }

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

        if (allTrench)
        {
            if (value)
            {
                return;
            }
            else
            {
                for (int y = 0; y < resolution; y++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        blocks[x, y] = MapBlock.GetFull(true);
                    }
                }
            }
        }

        float blockWidth = manager.blockWidth; // Width of each MapBlock
        float cellWidth = manager.cellWidth;      // Width of each bit in a MapBlock

        var startMax = Vector2.one * startRadius;
        var endMax = Vector2.one * endRadius;

        var mapMin = pos - scale / 2 * Vector2.one;
        var mapMax = pos + scale / 2 * Vector2.one;

        var capsuleMin = Vector2.Min(startPoint - startMax, endPoint - endMax);
        capsuleMin = Vector2.Max(capsuleMin, mapMin);

        var capsuleMax = Vector2.Max(startPoint + startMax, endPoint + endMax);
        capsuleMax = Vector2.Min(capsuleMax, mapMax);

        //var halfVector2One = Vector2.one * .5f;

        var startPos = manager.GetBlockAdressFloored(capsuleMin, pos); //Vector2Int.FloorToInt(((capsuleMin - pos) / blockWidth) + (resolution * halfVector2One));
        startPos = Vector2Int.Max(startPos, Vector2Int.zero);

        var endPos = manager.GetBlockAdressCield(capsuleMax, pos);
            //Vector2Int.CeilToInt(((capsuleMax - pos) / blockWidth) + (resolution * halfVector2One));

        //GeoUtils.DrawBoxMinMax(startPos, endPos, Color.magenta);
        areaChanged = 0;
        var bitArea = cellWidth * cellWidth;
        //honestly probably better to switch between setting all the pixels and some of them depending on how many

        //Color32[] pixels = new Color32[resolution * 4 * resolution * 4];

        //for (int i = 0; i < resolution * 4 * resolution * 4; i++)
        //{
        //    pixels[i] = groundColor;
        //}

        float blockCircleRadius = (Vector2.one.magnitude * cellWidth * 1.5f) + (cellWidth * 1.5f);

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
                var blockPos = manager.GetBlockPos(pos, new(blockX, blockY));

                // If the block already fully matches the target value, skip processing
                if (block.TestWhole(value))
                {
                    if (debugLines)
                    {
                        GeoUtils.MarkPoint(blockPos, blockWidth / 2, Color.white);
                    }

                    continue;
                }
                bool blockChanged = false;

                totalBlocksTested++;
                // Check if the block overlaps with the tapered capsule
                if (!GeoUtils.TestCirlceTouchesTaperedCapsule(blockPos, blockCircleRadius, startPoint, startRadius, endPoint, endRadius,
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
                        var bitValue = block[bitX, bitY];

                        totalBitsTested++;

                        // Skip if this bit already has the desired value
                        if (bitValue == value)
                        {
                            totalBitsAtValue++;
                            continue;
                        }

                        // Compute the position of the bit in world space
                        var localCell = new LocalCell (new(blockX,blockY), new(bitX, bitY));

                        var cellPos = GetCellPos(localCell);

                        //if (!GeoUtils.TestBoxMinMax(capsuleMin, capsuleMax, bitPos, debugLines))
                        //    continue;

                        // Test if the bit is within the capsule
                        if (GeoUtils.TestCirlceTouchesTaperedCapsule(cellPos, cellWidth / 2, startPoint, startRadius, endPoint, endRadius))
                        {
                            //block[bitX, bitY] = value;
                            totalBitsAtValue++;
                            if (debugLines)
                            {
                                GeoUtils.DrawCircle(cellPos, cellWidth / 2, Color.green);
                            }

                            areaChanged += bitArea;

                            blockChanged = true;

                            SetCellValue(localCell, value);

                            if (areaChanged >= maxArea)
                            {
                                ApplyPixels();
                                return;
                            }
                        }
                        else if (debugLines)
                        {
                            GeoUtils.DrawCircle(cellPos, cellWidth / 2, Color.red);
                        }
                    }
                }

                if (blockChanged)
                {

                    var wholeIsNowValue = totalBitsAtValue >= 16;

                    // Remove the block if all bits are cleared and `value` is false
                    if (!value && wholeIsNowValue)
                    {
                        blocks[blockX, blockY] = null;
                    }
                }
            }
        }

        allFull = totalTrenchCells == 0;

        allTrench = totalTrenchCells == resolution * resolution * 16;

        //if (allTrench)
        //    blocks = null; yeah noo lol

        //Debug.Log($"{totalEditedBlocks} have been edited");

        if (areaChanged > 0)
        {
            ApplyPixels();
        }
    }

    //public IEnumerable<Vector2> GetBitsFromBox(Vector2 min, Vector2 max, )

    public IEnumerable<Vector2> GetCellsTouchingTaperedCapsule (Vector2 startPoint, float startRadius, Vector2 endPoint, float endRadius, bool value)
    {
        var manager = TrenchManager.Manager;

        var startMax = Vector2.one * startRadius;
        var endMax = Vector2.one * endRadius;

        var mapMin = pos - scale / 2 * Vector2.one;
        var mapMax = pos + scale / 2 * Vector2.one;

        var capsuleMin = Vector2.Min(startPoint - startMax, endPoint - endMax);
        capsuleMin = Vector2.Max(capsuleMin, mapMin);

        var capsuleMax = Vector2.Max(startPoint + startMax, endPoint + endMax);
        capsuleMax = Vector2.Min(capsuleMax, mapMax);

        //var halfVector2One = Vector2.one * .5f;

        //var cellsFromBlock = GetCellsFromBox(capsuleMin, capsuleMax, true, !value);

        //foreach (var cell in cellsFromBlock)
        //{

        //}

        var cellRadius = manager.cellWidth / 2;

        var blockCircleRadius = (manager.cellWidth * 2 * Vector2.one).magnitude + cellRadius;

        Func<Vector2Int, bool> blockCondition =  (blockAdress) =>
        {
            var block = blocks[blockAdress.x, blockAdress.y];// breh i gotta redo this cause it gave me a bunch of null reference errors

            if (
                (value && block == null) || //if the block is empty and we are looking for trenches
                (!value && block != null && block.TestFull())
            )
            {
                return false;
            }

            var blockPos = manager.GetBlockPos(pos, blockAdress);

            return GeoUtils.TestCirlceTouchesTaperedCapsule(blockPos, blockCircleRadius, startPoint, startRadius, endPoint, endRadius);
        };

        var cellsFromBox = GetCellsFromBox(capsuleMin, capsuleMax, blockCondition);

        foreach (var cell in cellsFromBox)
        {
            var cellValue = GetCellValue(cell);

            if (cellValue != value)
                continue;

            var cellPos = GetCellPos(cell);

            if (GeoUtils.TestCirlceTouchesTaperedCapsule(cellPos,cellRadius,startPoint,startRadius,endPoint,endRadius))
            {
                yield return cellPos;
            }
        }
    }

    //public void SetBit (Vector2Int blockAdress, Vector2Int bitAdress, bool value, bool applyPixels = false)
    //{
    //    if (blocks[blockAdress.x,blockAdress.y] == null)
    //    {
    //        if (value)
    //        {
    //            blocks[blockAdress.x, blockAdress.y] = new();
    //        }
    //        else
    //        {
    //            return;
    //        }
    //    }

    //    blocks[blockAdress.x, blockAdress.y][bitAdress] = value;

    //    var arrayIndex = bitAdress.y * resolution * 4 + bitAdress.x + blockAdress.y * 16 * resolution + blockAdress.x * 4;

    //    pixels[arrayIndex] = value ? trenchColor : groundColor;

    //    if (applyPixels)
    //    {
    //        ApplyPixels();
    //    }
    //}

    public void ApplyPixels ()
    {
        imageTexture.SetPixels32(pixels);

        imageTexture.Apply();
    }

    public bool TestCircleTouchesValue(Vector2 circlePos, float circleRadius, bool value, bool debugLines = false)
    {
        if (allTrench)
            return value;

        var blockWidth = TrenchManager.Manager.blockWidth;
        var bitWidth = TrenchManager.Manager.cellWidth;

        var mapMin = pos - scale / 2 * Vector2.one;
        var mapMax = pos + scale / 2 * Vector2.one;

        var circleMin = circlePos - (circleRadius * Vector2.one);
        circleMin = Vector2.Max(circleMin, mapMin);

        var circleMax = circlePos + (circleRadius * Vector2.one);
        circleMax = Vector2.Min(circleMax, mapMax);

        //var halfVector2One = Vector2.one * .5f;

        var startPos = TrenchManager.Manager.GetBlockAdressFloored(circleMin, pos); //Vector2Int.FloorToInt(((capsuleMin - pos) / blockWidth) + (resolution * halfVector2One));
        startPos = Vector2Int.Max(startPos, Vector2Int.zero);

        var endPos = TrenchManager.Manager.GetBlockAdressCield(circleMax, pos);

        float blockCircleRadius = Vector2.one.magnitude * bitWidth * 1.5f;

        for (var blockY = startPos.y; blockY < endPos.y; blockY++)
        {
            for (var blockX = startPos.x; blockX < endPos.x; blockX++)
            {
                var block = blocks[blockX, blockY];

                var blockPos = TrenchManager.Manager.GetBlockPos(pos, new Vector2Int(blockX, blockY));

                if (block == null)
                {
                    if (!value && GeoUtils.CirclesOverlap(blockPos, blockCircleRadius, circlePos, circleRadius, debugLines))
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
                        var bitPos = TrenchManager.Manager.GetCellPos(blockPos, new(bitX, bitY));

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

    public bool TestPoint (Vector2 point)
    {
        var blockAdress = TrenchManager.Manager.GetBlockAdressFloored(point, pos);

        if (blocks[blockAdress.x, blockAdress.y] == null)
            return false;

        var blockPos = TrenchManager.Manager.GetBlockPos(pos, blockAdress);

        var bitAdress = TrenchManager.Manager.GetCellAdressFloored(point, blockPos);

        //bitAdress = Vector2Int.Min(bitAdress, Vector2Int.one * 3);

        return blocks[blockAdress.x, blockAdress.y][bitAdress];
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

    public int LocalCellToPixelIndex (LocalCell cell)
    {
        var blockAdress = cell.blockAdress;
        var cellAdress = cell.cellAdress;

        return cellAdress.y * resolution * 4 + cellAdress.x + blockAdress.y * 16 * resolution + blockAdress.x * 4;
    }

    public IEnumerable<LocalCell> GetCellsFromBox (Vector2 min, Vector2 max, Func<Vector2Int, bool> blockCondition)
    {
        var startBlock = TrenchManager.Manager.GetBlockAdressFloored(min, pos); //Vector2Int.FloorToInt(((capsuleMin - pos) / blockWidth) + (resolution * halfVector2One));
        startBlock = Vector2Int.Max(startBlock, Vector2Int.zero);

        var endBlock = TrenchManager.Manager.GetBlockAdressCield(max, pos);

        return GetCellsFromBoundBlocks(startBlock,endBlock,blockCondition);
    }

    public IEnumerable<LocalCell> GetCellsFromBoundBlocks (Vector2Int startBlock, Vector2Int endBlock, 
        Func<Vector2Int, bool> blockCondition)
    {
        //this could cause null reference exceptions for block array, but i'll wait until it does

        for (var blockY = startBlock.y; blockY < endBlock.y; blockY++)
        {
            for (var blockX = startBlock.x; blockX < endBlock.x; blockX++)
            {
                Vector2Int blockAdress = new(blockX, blockY);

                if (blockCondition != null && !blockCondition(blockAdress))
                    continue;

                //could be a good idea to clamp cell start and end...?

                for (var cellY = 0; cellY < 4; cellY++)
                {
                    for (var cellX = 0; cellX < 4; cellX++)
                    {
                        var cellAdress = new Vector2Int(cellX, cellY);

                        yield return new(blockAdress, cellAdress);
                    }
                }
            }
        }
    }

    public IEnumerable<LocalCell> GetCells (Func<Vector2Int, bool> blockCondition)
    {
        var maxBlock = resolution;

        for (var blockY = 0; blockY < maxBlock; blockY++)
        {
            for (var blockX = 0; blockX < maxBlock; blockX++)
            {
                Vector2Int blockAdress = new(blockX, blockY);

                if (blockCondition != null && !blockCondition(blockAdress))
                    continue;

                //could be a good idea to clamp cell start and end...?

                for (var cellY = 0; cellY < 4; cellY++)
                {
                    for (var cellX = 0; cellX < 4; cellX++)
                    {
                        var cellAdress = new Vector2Int(cellX, cellY);

                        yield return new(blockAdress, cellAdress);
                    }
                }
            }
        }
    }

    public LocalCell GetLocalCell(Vector2 pos)
    {
        var blockAdress = TrenchManager.Manager.GetBlockAdressFloored(pos, this.pos);
        var blockPos = TrenchManager.Manager.GetBlockPos(this.pos, blockAdress);
        var cellAdress = TrenchManager.Manager.GetCellAdressFloored((Vector2)blockAdress, blockPos);

        return new(blockAdress, cellAdress);
    }

    public Vector2 GetCellPos(LocalCell cell)
    {
        var blockPos = TrenchManager.Manager.GetBlockPos(pos, cell.blockAdress);
        return TrenchManager.Manager.GetCellPos(blockPos, cell.cellAdress);
    }

    public bool GetCellValue (LocalCell cell)
    {
        var block = blocks[cell.blockAdress.x,cell.blockAdress.y];

        return block != null ? block[cell.cellAdress] : false;
    }

    public bool GetCellValue (Vector2 pos)
    {
        var cell = GetLocalCell(pos);

        return GetCellValue(cell);
    }

    public void SetCellValue (LocalCell cell, bool value)
    {
        var block = blocks[cell.blockAdress.x, cell.blockAdress.y];

        if (block == null)
        {
            if (value)
            {
                block = blocks[cell.blockAdress.x,cell.blockAdress.y] = new();
            }
            else
            {
                return;
            }
        }

        totalTrenchCells += value ? 1 : -1;

        var pixelIndex = LocalCellToPixelIndex(cell);

        pixels[pixelIndex] = value ? trenchColor : groundColor;

        block[cell.cellAdress] = value;
    }

    public void SetCellValue(Vector2 pos, bool value)
    {
        var cell = GetLocalCell(pos);

        SetCellValue(cell, value);
    }

    public class LocalCell
    {
        public Vector2Int blockAdress, cellAdress;

        public LocalCell(Vector2Int blockAdress, Vector2Int cellAdress)
        {
            //this.chunkAdress = chunkAdress;
            this.blockAdress = blockAdress;
            this.cellAdress = cellAdress;
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.PlayerSettings;
using UnityEngine.UIElements;

public class TrenchManager : ManagerBase<TrenchManager>
{
    public float maxPixelSize = .01f;
    int mapResolution;
    public Color32 trenchColor, groundColor;
    public Mesh imageMesh;
    public Material imageMaterial;//
    public FilterMode filter;
    public Transform pointA, pointB;
    public bool runTest = false, rayLines = false, fillLines = false, drawAllBits = false;

    private void Start()
    {
        //mapResolution = ChunkManager.Manager.chunkSize //determine resolution with maxPixelSize

        mapResolution = Mathf.FloorToInt(ChunkManager.Manager.chunkSize / maxPixelSize/4);
    }

    private void Update()
    {
        if (runTest)
        {
            TestRayHitsValue(pointA.position, pointB.position, false, out _, true);
        }

        if (drawAllBits)
        {
            DrawAllBits();
        }

        foreach (var chunk in ChunkManager.Manager.chunks)
        {
            if (chunk != null && chunk.map != null)
            {
                chunk.map.Draw();
            }
        }
    }

    public void SetTaperedCapsule(Vector2 startPoint, float startRadius, Vector2 endPoint, float endRadius, bool value, out bool wasModified)
    {
        var startMax = Vector2.one * startRadius;
        var endMax = Vector2.one * endRadius;

        var capsuleMin = Vector2.Min(startPoint - startMax, endPoint - endMax);
        //capsuleMin = Vector2.Max(capsuleMin, mapMin);

        var capsuleMax = Vector2.Max(startPoint + startMax, endPoint + endMax);
        //capsuleMax = Vector2.Min(capsuleMax, mapMax);

        var chunks = ChunkManager.Manager.ChunksFromBoxMinMax(capsuleMin, capsuleMax, value);

        wasModified = false;

        foreach (var chunk in chunks)
        {
            if (chunk == null)
                continue;

            if (chunk.map == null)
            {
                if (value)
                {
                    var pos = ChunkManager.Manager.GetChunkPos(chunk) + ChunkManager.Manager.chunkSize * .5f * Vector2.one;

                    chunk.map = new(mapResolution,ChunkManager.Manager.chunkSize, trenchColor, groundColor,pos,imageMesh,imageMaterial,filter);
                }
                else
                {
                    continue;
                }
            }

            chunk.map.SetTaperedCapsule(startPoint, startRadius, endPoint, endRadius, value, out var mapChanged, fillLines);

            if (mapChanged)
                wasModified = true;

            //Debug.Log($"chunk {chunk.adress} has {chunk.map.totalEditedBlocks} edited blocks");

            if (chunk.map.empty)
            {
                chunk.RemoveTrenchMap();
            }
        }
    }

    public bool TestRayHitsValue(Vector2 startPoint, Vector2 endPoint, bool value, out float distance, bool logTotal = false)
    {
        var blockWidth = GetBlockWidth();
        var bitWidth = GetBitWidth(blockWidth);

        if (rayLines)
        {
            Debug.DrawLine(startPoint, endPoint, Color.blue);
        }

        distance = Vector2.Distance(startPoint, endPoint);

        var bitCorner = bitWidth * .5f * Vector2.one;

        startPoint -= bitCorner;
        endPoint -= bitCorner;

        //return GeoUtils.ForeachCellTouchingLine<bool>(startPoint, endPoint, bitWidth, null, returnCondition, returnCondition, out _, logTotal);

        var lineToCells = GeoUtils.GetLineCells(startPoint, endPoint, bitWidth);

        foreach (var cell in lineToCells)
        {
            var pos = (Vector2)cell * bitWidth;

            var chunk = ChunkManager.Manager.ChunkFromPos(pos);

            if (chunk == null || chunk.map == null)
            {
                if (rayLines)
                    GeoUtils.DrawBoxPosSize(pos + bitCorner, bitWidth * Vector2.one,
                        !value ? Color.green : Color.white);

                if (value)
                    continue;
                else
                {
                    distance = Vector2.Distance(startPoint, pos + bitCorner);
                    return true;
                }
            }
            else if (rayLines)
            {

            }

            var blockAdress = GetBlockAdressFloored(pos, chunk.map.pos, blockWidth);

            //if (debugLines)
            //    GeoUtils.DrawBoxPosSize(pos, bitWidth * Vector2.one, Color.green);

            var block = chunk.map.GetBlock(blockAdress);

            var blockPos = GetBlockPos(chunk.map.pos, blockAdress, blockWidth);

            if (block == null)
            {
                if (rayLines)
                {
                    var color = !value ? Color.green : Color.white;
                    GeoUtils.DrawBoxPosSize(blockPos, blockWidth * Vector2.one, color);
                    GeoUtils.DrawBoxPosSize(pos+ bitCorner, bitWidth * Vector2.one, color);
                }

                if (value)
                    continue;
                else
                {
                    distance = Vector2.Distance(startPoint, pos + bitCorner);
                    return true;
                }
            }

            if (block.TestWhole(value))
            {
                if (rayLines)
                {
                    GeoUtils.DrawBoxPosSize(blockPos, blockWidth * Vector2.one, Color.green);
                    GeoUtils.DrawBoxPosSize(pos + bitCorner, bitWidth * Vector2.one, Color.green);
                }

                distance = Vector2.Distance(startPoint, pos + bitCorner);
                return true;
            }

            if (rayLines)
            {
                GeoUtils.DrawBoxPosSize(blockPos, blockWidth * Vector2.one, Color.red);
            }

            var bitAdress = GetBitAdressFloored(pos+ bitCorner, blockPos, blockWidth, bitWidth);

            //bitAdress = Vector2Int.Min(bitAdress, Vector2Int.one * 3);
            //bitAdress = Vector2Int.Max(bitAdress, Vector2Int.zero);

            //if (bitAdress.x < 0 || bitAdress.y < 0)
            //    continue;

            if (block[bitAdress] == value)
            {
                if (rayLines)
                {
                    GeoUtils.DrawBoxPosSize(pos + bitCorner, bitWidth * Vector2.one, Color.green);
                    GeoUtils.MarkPoint(pos + bitCorner, bitWidth / 2, Color.green);
                }

                distance = Vector2.Distance(startPoint, pos + bitCorner);
                return true;
            }
            else if (rayLines)
            {
                GeoUtils.DrawBoxPosSize(pos + bitCorner, bitWidth * Vector2.one, Color.red);
            }
        }

        return false;
    }

    public bool TestCircleTouchesValue(Vector2 circlePos, float circleRadius, bool value, bool debugLines = false)
    {
        var chunks = ChunkManager.Manager.ChunksFromBoxPosSize(circlePos, circleRadius * 2 * Vector2.one, false);

        foreach (var chunk in chunks)
        {
            if (chunk == null || chunk.map == null)
            {
                if (value)
                {
                    continue;
                }
                else
                {
                    return true;
                }
            }

            if (chunk.map.TestCircleTouchesValue(circlePos, circleRadius, value, debugLines))
                return true;
        }

        return false;
    }

    public void DrawAllBits ()
    {
        var trenchBits = GetBits(true);
        var bitWidth = GetBitWidth(GetBlockWidth());

        foreach (var bit in trenchBits)
        {
            GeoUtils.DrawBoxPosSize(bit, bitWidth * Vector2.one,Color.green);
        }
    }

    public IEnumerable<Vector2> GetBits (bool value)
    {
        var blockWidth = GetBlockWidth();
        var bitWidth = GetBitWidth(blockWidth);

        foreach (var chunk in ChunkManager.Manager.chunks)
        {
            if (chunk == null || chunk.map == null)
                continue;

            for (var blockY = 0; blockY < mapResolution; blockY++)
            {
                for (var blockX = 0; blockX < mapResolution; blockX++)
                {
                    var block = chunk.map.blocks[blockX, blockY];

                    var blockPos = GetBlockPos(chunk.map.pos, new Vector2Int(blockX, blockY), blockWidth);

                    if (block == null)
                        continue;

                    for (var bitY = 0; bitY < 4; bitY++)
                    {
                        for (var bitX = 0; bitX < 4; bitX++)
                        {
                            if (block[bitX,bitY] == value)
                            {
                                yield return GetBitPos(blockPos, bitWidth, new(bitX, bitY));
                            }
                        }
                    }
                }
            }
        }
    }

    public float GetBitWidth(float blockWidth)
    {
        return blockWidth / 4f;
    }

    public float GetBlockWidth()
    {
        return ChunkManager.Manager.chunkSize / mapResolution;
    }

    public Vector2 GetBlockPos(Vector2 mapPos, Vector2Int blockAdress, float blockWidth)
    {
        return (new Vector2(blockAdress.x + 0.5f, blockAdress.y + 0.5f) - mapResolution * .5f * Vector2.one) * blockWidth
                    + mapPos;
    }

    public Vector2 GetBitPos(Vector2 blockPos, float bitWidth, Vector2Int bitAdress)
    {
        return blockPos + new Vector2((bitAdress.x - 1.5f) * bitWidth, (bitAdress.y - 1.5f) * bitWidth);
    }

    public Vector2 GetBlockAdressPoint(Vector2 pos, Vector2 mapPos, float blockWidth)
    {
        return ((pos - mapPos) / blockWidth) + (mapResolution * .5f * Vector2.one);
    }

    public Vector2Int GetBlockAdressFloored(Vector2 pos, Vector2 mapPos, float blockWidth)
    {
        return Vector2Int.FloorToInt(GetBlockAdressPoint(pos, mapPos, blockWidth));
    }

    public Vector2Int GetBlockAdressCield(Vector2 pos, Vector2 mapPos, float blockWidth)
    {
        return Vector2Int.CeilToInt(GetBlockAdressPoint(pos, mapPos, blockWidth));
    }

    public Vector2Int GetBlockAdressRounded(Vector2 pos, Vector2 mapPos, float blockWidth)
    {
        return Vector2Int.RoundToInt(GetBlockAdressPoint(pos, mapPos, blockWidth));
    }

    public Vector2 GetBitAdressPoint(Vector2 pos, Vector2 blockPos, float blockWidth, float bitWidth)
    {
        return (pos - blockPos + .5f * blockWidth * Vector2.one) / bitWidth;
    }

    public Vector2Int GetBitAdressFloored (Vector2 pos, Vector2 blockPos, float blockWidth, float bitWidth)
    {
        var point = GetBitAdressPoint(pos, blockPos, blockWidth, bitWidth);
        var floored = Vector2Int.FloorToInt(point);
        return floored;
    }
}

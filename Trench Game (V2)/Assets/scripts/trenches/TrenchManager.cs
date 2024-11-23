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

    private void Start()
    {
        //mapResolution = ChunkManager.Manager.chunkSize //determine resolution with maxPixelSize

        mapResolution = Mathf.FloorToInt(ChunkManager.Manager.chunkSize / maxPixelSize/4);
    }

    private void Update()
    {
        foreach (var chunk in ChunkManager.Manager.chunks)
        {
            if (chunk != null && chunk.map != null)
            {
                chunk.map.Draw();
            }
        }
    }

    public void SetTaperedCapsule(Vector2 startPoint, float startRadius, Vector2 endPoint, float endRadius, bool value, out bool wasModified,
        bool debugLines = false)
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

                    chunk.map = new(mapResolution,ChunkManager.Manager.chunkSize, trenchColor, groundColor,pos,imageMesh,imageMaterial);
                }
                else
                {
                    continue;
                }
            }

            chunk.map.SetTaperedCapsule(startPoint, startRadius, endPoint, endRadius, value, out var mapChanged, debugLines);

            if (mapChanged)
                wasModified = true;

            Debug.Log($"chunk {chunk.adress} has {chunk.map.totalEditedBlocks} edited blocks");

            if (chunk.map.empty)
            {
                chunk.RemoveTrenchMap();
            }
        }
    }

    public bool TestRayHitsValue(Vector2 startPoint, Vector2 endPoint, bool value, out Vector2 hitPoint, bool debugLines = false, bool logTotal = false)
    {
        //var lineMin = Vector2.Min(startPoint,endPoint);
        //var lineMax = Vector2.Max(startPoint,endPoint);

        //Func<Vector2Int, bool> returnCondition = cell =>
        //{
        //    var pos = 

        //    var chunk = ChunkManager.Manager.ChunkFromPos(cell);

        //    if (chunk == null) //if completely filled
        //    {
        //        return !value;
        //    }
        //};

        //Func<Vector2Int, Vector2> getOutput = cell =>
        //{

        //};

        //var blockWidth = ChunkManager.Manager.chunkSize / mapResolution;

        //return GeoUtils.ForeachCellTouchingLine<Vector2>(startPoint, endPoint, blockWidth, null, returnCondition, returnCondition, out _, logTotal);
        hitPoint = Vector2.zero;

        return false;
    }

    public float GetBitWidth(float blockWidth)
    {
        return blockWidth / 4f;
    }

    public float GetBlockWidth()
    {
        return ChunkManager.Manager.chunkSize / mapResolution;
    }

    public Vector2 GetBlockPos(Vector2 mapPos, Vector2 blockAdress, float blockWidth)
    {
        return (new Vector2(blockAdress.x + 0.5f, blockAdress.y + 0.5f) - mapResolution * .5f * Vector2.one) * blockWidth
                    + mapPos;
    }

    public Vector2 GetBitPos(Vector2 blockPos, float bitWidth, Vector2 bitAdress)
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
}

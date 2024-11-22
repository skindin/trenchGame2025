using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
}

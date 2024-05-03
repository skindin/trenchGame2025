using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    public List<Chunk> chunks = new();
    public float chunkSize = 50;

    private void Awake()
    {
        Chunk.manager = this;
    }

    public Chunk ChunkFromPos(Vector2 pos, bool newIfNone = true)
    {
        var coords = PosToCoords(pos);
        return ChunkFromCoords(coords, newIfNone);
    }

    public Chunk ChunkFromCoords(Vector2Int coords, bool newIfNone = true)
    {
        foreach (var chunk in chunks)
        {
            if (TrenchManager.instance.debugLines) DrawChunk(chunk, Color.red);

            if (chunk.coords == coords)
            {
                return chunk;
            }
        }

        if (newIfNone)
        {
            var newChunk = new Chunk(coords);
            chunks.Add(newChunk);
            if (!Chunk.manager) Chunk.manager = this;
            return newChunk;
        }

        return null;
    }

    public List<Chunk> ChunksFromBox(Vector2 min, Vector2 max, List<Chunk> chunks, bool newIfNone = true)
    {
        var intMin = PosToCoords(min);
        var intMax = PosToCoords(max);

        for (var y = intMin.y; y < intMax.y + 1; y++)
        {
            for (var x = intMin.x; x < intMax.x + 1; x++)
            {
                var coords = new Vector2Int(x, y);
                var chunk = ChunkFromCoords(coords, newIfNone);
                if (chunk != null && !chunks.Contains(chunk)) chunks.Add(chunk);
            }
        }

        return chunks;
    }

    /// <summary>
    /// Calculates chunks.
    /// Should only be used after a trench has gone through significant change without acknowledging chunks
    /// </summary>
    /// <param name="trench"></param>
    public void AutoAssignChunks(Trench trench)
    {
        var chunks = trench.chunks = ChunksFromBox(trench.boxMin, trench.boxMax, trench.chunks);

        foreach (var chunk in chunks)
        {
            chunk.trenches.Add(trench);
        }
    }

    public void UnassignChunks(Trench trench, bool removeEmptyChunks = false)
    {
        for (var i = 0; i < trench.chunks.Count; i++)
        {
            var chunk = trench.chunks[i];
            chunk.trenches.Remove(trench);

            if (chunk.trenches.Count == 0 && removeEmptyChunks)
            {
                chunks.Remove(chunk);
            }
        }

        trench.chunks.Clear();
    }

    public void DrawChunk(Chunk chunk, Color color = default)
    {
        if (color == default) color = Color.white;

        var center = (Vector2)chunk.coords * chunkSize;
        var halfChunk = chunkSize / 2;
        var maxDelta = Vector2.one * halfChunk;

        var topRight = center + maxDelta;
        var bottomLeft = center - maxDelta;
        var bottomRight = new Vector2(topRight.x, bottomLeft.y);
        var topLeft = new Vector2(bottomLeft.x, topRight.y);

        Debug.DrawLine(topRight, bottomRight, color);
        Debug.DrawLine(bottomRight, bottomLeft, color);
        Debug.DrawLine(bottomLeft, topLeft, color);
        Debug.DrawLine(topLeft, topRight, color);
    }

    public Vector2Int PosToCoords (Vector2 pos)
    {
        return Vector2Int.RoundToInt(pos / chunkSize);
    }


    /// <summary>
    /// Tests if pontA and pointB are in the same chunk
    /// </summary>
    /// <param name="pointA"></param>
    /// <param name="pointB"></param>
    /// <returns></returns>
    public bool TestDifferentChunks (Vector2 pointA, Vector2 pointB)
    {
        var aCoords = PosToCoords(pointA);
        var bCoords = PosToCoords(pointB);

        return aCoords != bCoords;
    }



    public void AssignTrenchToChunk(Trench trench, Chunk chunk)
    {
        if (!trench.chunks.Contains(chunk))
        {
            trench.chunks.Add(chunk);
            chunk.trenches.Add(trench);
        }
    }
}

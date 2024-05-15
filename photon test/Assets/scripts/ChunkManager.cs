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

    private void Update()
    {
        Chunk.manager = this;
    }

    public Vector2Int PosToCoords(Vector2 pos)
    {
        return Vector2Int.RoundToInt(pos / chunkSize);
    }

    public Chunk ChunkFromPos(Vector2 pos, bool newIfNone = true, bool debugLines = false)
    {
        var coords = PosToCoords(pos);
        return ChunkFromCoords(coords, newIfNone, debugLines);
    }

    public Vector2 GetRealChunkPos (Vector2Int coords)
    {
        return (Vector2)coords * chunkSize;
    }

    public Chunk ChunkFromCoords(Vector2Int coords, bool newIfNone = true, bool debugLines = false)
    {
        foreach (var chunk in chunks)
        {
            if (chunk.coords == coords)
            {
                if (debugLines) DrawChunk(chunk, Color.green);
                return chunk;
            }

            if (debugLines) DrawChunk(chunk, Color.red);
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

    public List<Chunk> ChunksFromBox(Vector2 min, Vector2 max, List<Chunk> chunks, bool newIfNone = true, bool debugLines = false)
    {
        var intMin = PosToCoords(min);
        var intMax = PosToCoords(max);

        for (var y = intMin.y; y < intMax.y + 1; y++)
        {
            for (var x = intMin.x; x < intMax.x + 1; x++)
            {
                var coords = new Vector2Int(x, y);
                var chunk = ChunkFromCoords(coords, newIfNone, debugLines);
                if (chunk != null && !chunks.Contains(chunk)) chunks.Add(chunk);
            }
        }

        return chunks;
    }

    public List<Chunk> ChunksFromLine(Vector2 pointA, Vector2 pointB, List<Chunk> chunks, bool newIfNone = true, bool debugLines = false)
    {
        Vector2 first;
        Vector2 last;

        if (pointA.x < pointB.x)
        {
            first = pointA;
            last = pointB;
        }
        else
        {
            first = pointB;
            last = pointA;
        }

        var firstCoords = PosToCoords(first);
        var lastCoords = PosToCoords(last);

        if (firstCoords == lastCoords)
        {
            var chunk = ChunkFromCoords(firstCoords, newIfNone, false);
            if (chunk != null && !chunks.Contains(chunk))
            {
                chunks.Add(chunk);
            }

        }
        else
        {
            var coordsDelta = lastCoords - firstCoords;

            var reversed = coordsDelta.y < 0;

            int yIncrement = 1;
            if (reversed)
                yIncrement = -1;

            for (int y = 0; y != coordsDelta.y+yIncrement; y += yIncrement)
            {
                for (int x = 0; x < coordsDelta.x + 1; x++)
                {
                    var coords = new Vector2Int(x, y) + firstCoords;
                    var boxDelta = chunkSize / 2 * Vector2.one;
                    var chunkPos = GetRealChunkPos(coords);
                    var min = chunkPos - boxDelta;
                    var max = chunkPos + boxDelta;
                    if (GeoFuncs.DoesLineIntersectBox(pointA, pointB, min, max, debugLines))
                    {
                        var chunk = ChunkFromCoords(coords, newIfNone, debugLines);
                        if (chunk != null && !chunks.Contains(chunk))
                        {
                            chunks.Add(chunk);
                        }
                    }
                }
            }
        }

        if (debugLines)
        {
            foreach (var chunk in chunks)
            {
                DrawChunk(chunk, Color.green);
            }

            Debug.DrawLine(pointA, pointB, Color.green);
        }

        return chunks; //this doesn't work at all
    }

    /// <summary>
    /// Calculates chunks.
    /// Should only be used after a trench has gone through significant change without acknowledging chunks
    /// </summary>
    /// <param name="trench"></param>
    public void AutoAssignChunks(Trench trench)
    {
        UnassignChunks(trench);

        var bounds = trench.lineMesh.mesh.bounds;
        var chunks = trench.chunks = ChunksFromBox(bounds.min, bounds.max, trench.chunks);

        foreach (var chunk in chunks)
        {
            if (!chunk.trenches.Contains(trench))
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

    public List<Chunk> GetAdjacenChunks (Vector2 pos, List<Chunk> chunks, bool debugLines = false)
    {
        var boxDelta = Vector2.one * chunkSize;

        var boxMin = pos - boxDelta;
        var boxMax = pos + boxDelta;

        return ChunksFromBox(boxMin, boxMax, chunks, false, debugLines);
    }
}

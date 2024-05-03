using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    public static ChunkManager manager;
    public Vector2Int coords;
    public List<Trench> trenches = new();

    public Chunk(Vector2Int newCoords)
    {
        coords = newCoords;
    }
}

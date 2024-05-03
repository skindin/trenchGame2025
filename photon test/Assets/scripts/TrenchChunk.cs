using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrenchChunk
{
    public Vector2Int coords;
    public List<Trench> trenches = new();

    public TrenchChunk(Vector2Int newCoords)
    {
        coords = newCoords;
    }
}

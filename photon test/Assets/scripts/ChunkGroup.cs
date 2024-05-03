using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkGroup
{
    public List<List<TrenchChunk>> posPos = new(); 
    public List<List<TrenchChunk>> posNeg = new();
    public List<List<TrenchChunk>> negNeg = new();
    public List<List<TrenchChunk>> negPos = new();

    public void AddChunk (Vector2Int pos)
    {
        if (pos.x >= 0)
        {
            if (pos.y >= 0)
            {

            }
            else
            {

            }
        }
        else
        {

        }
    }

    public void AddToList (Vector2Int pos)
    {

    }
}

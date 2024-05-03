using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkGroup
{
    public List<List<Chunk>> posPos = new(); 
    public List<List<Chunk>> posNeg = new();
    public List<List<Chunk>> negNeg = new();
    public List<List<Chunk>> negPos = new();

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

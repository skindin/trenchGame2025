using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrenchMap
{
    public MapBlock[,] blocks;
    public int size;
    public Texture texture;

    public TrenchMap(int size)
    {
        blocks = new MapBlock[size, size];
        this.size = size;
    }

    /// <summary>
    /// Must convert points and radii to map's local space!
    /// </summary>
}

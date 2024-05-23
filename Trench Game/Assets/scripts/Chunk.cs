using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    public TrenchMap map;
    public Vector2Int adress;
    public readonly List<Character> characters = new();
    public readonly List<Collider> colliders = new();

    public Chunk (Vector2Int adress, int mapSize)
    {
        this.adress = adress;
        map = new(mapSize);
    }
}

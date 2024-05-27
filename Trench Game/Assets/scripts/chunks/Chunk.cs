using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[System.Serializable]
public class Chunk
{
    public TrenchMap map;
    public Vector2Int adress;
    public readonly List<Character> characters = new();
    public readonly List<Collider> colliders = new();
    public readonly List<Item> items = new();

    public Chunk (Vector2Int adress, int mapSize)
    {
        this.adress = adress;
        map = new(mapSize);
    }

    //public void AddItem(Item item)
    //{
    //    items.Add(item);
    //    foreach ()
    //}

    //public void RemoveItem(Item item)
    //{
    //    items.Remove(item);
    //}
}

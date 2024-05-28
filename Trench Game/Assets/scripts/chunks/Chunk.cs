using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

//[System.Serializable]
public class Chunk
{
    public TrenchMap map;
    public Vector2Int adress;
    public readonly List<Character> characters = new();
    public readonly List<Collider> colliders = new();
    public readonly List<Item> items = new();
    public UnityEvent<Item> onNewItem = new();

    public Chunk (Vector2Int adress, int mapSize)
    {
        this.adress = adress;
        map = new(mapSize);
    }

    public void AddCharacter (Character character)
    {
        characters.Add(character);
        colliders.Add(character.collider);
    }

    public void RemoveCharacter (Character character)
    {
        characters.Remove(character);
        colliders.Remove(character.collider);
    }

    public void AddItem(Item item)
    {
        items.Add(item);
        onNewItem.Invoke(item);
    }

    public void RemoveItem(Item item)
    {
        items.Remove(item);
    }
}

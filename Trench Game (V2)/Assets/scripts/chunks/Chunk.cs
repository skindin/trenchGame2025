using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    public Chunk() { }

    public void AddCharacter (Character character)
    {
        characters.Add(character);
        colliders.Add(character.collider);
    }

    public void RemoveCharacter (Character character)
    {
        characters.Remove(character);
        colliders.Remove(character.collider);
        DestroyIfEmpty();
    }

    public void AddItem(Item item)
    {
        items.Add(item);
        onNewItem.Invoke(item);
    }

    public void RemoveItem(Item item)
    {
        items.Remove(item);
        DestroyIfEmpty();
    }

    public void DestroyIfEmpty ()
    {
        if (items.Count == 0 && characters.Count == 0 && ChunkManager.Manager) //should test for colliders too, but don't need to atm
        {
            ChunkManager.Manager.RemoveChunk(this); //this line caused two null refference exceptions at one point...?
        }
    }

    public void Reset (Vector2Int newAdress)
    {
        Reset();
        adress = newAdress;
    }

    public void Reset()
    {
        items.Clear();
        characters.Clear();
        colliders.Clear();
        onNewItem.RemoveAllListeners();
    }

    public T[] GetItems<T>(T[] array, bool clearArray = false) where T : Item
    {
        if (clearArray)
        {
            Array.Clear(array, 0, array.Length);
        }

        var type = typeof(T);
        int count = array.Length;

        foreach (var item in items)
        {
            if (type.IsAssignableFrom(item.GetType()))
            {
                count++;
                Array.Resize(ref array, count);
                array[count-1] = item as T;
            }
        }

        // Resize the array to fit the number of valid items found
        //Array.Resize(ref array, count);

        return array;
    }


    public T[] GetCharacters<T>(T[] array, bool clearArray = false) where T : Character
    {
        if (clearArray)
        {
            Array.Clear(array, 0, array.Length);
        }

        var type = typeof(T);
        int count = array.Length;

        foreach (var item in characters)
        {
            if (type.IsAssignableFrom(item.GetType()))
            {
                count++;
                Array.Resize(ref array, count);
                array[count-1] = item as T;
            }
        }

        return array;
    }

}

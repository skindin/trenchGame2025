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
    public readonly List<TrenchCollider> colliders = new();
    public readonly List<Item> items = new();
    //public List<Inventory> listeningInventories = new();

    readonly List<Bullet> bullets = new();
    public List<Bullet> Bullets //this is not an efficient way to do this but whatever
    {
        get
        {
            bullets.Clear();

            foreach (var bullet in ProjectileManager.Manager.activeBullets)
            {
                if (ChunkManager.Manager.PosToAdress(bullet.pos) == adress)
                    bullets.Add(bullet);
            }

            return bullets;
        }
    }

    public Chunk (Vector2Int adress, int mapSize)
    {
        this.adress = adress;
        //map = new(mapSize);
    }

    public Chunk() { }

    public void AddCharacter (Character character)
    {
        characters.Add(character);
        //colliders.Add(character.collider);
    }

    public void RemoveCharacter (Character character)
    {
        characters.Remove(character);
        //colliders.Remove(character.collider);
        DestroyIfEmpty();
    }

    public void AddItem(Item item)
    {
        items.Add(item);
        //foreach (var inventory in listeningInventories)
        //{
        //    inventory.onItemAdded(item);
        //}

        //Debug.Log($"Item {item} {item.gameObject.GetInstanceID()} was added to chunk {adress}");
    }

    public void RemoveItem(Item item)
    {
        items.Remove(item);

        //foreach (var inventory in listeningInventories)
        //{
        //    inventory.onItemRemoved?.Invoke(item);
        //}

        DestroyIfEmpty();

        //Debug.Log($"Item {item} {item.gameObject.GetInstanceID()} was removed from chunk {adress}");
    }
    
    public void RemoveTrenchMap ()
    {
        map = null;

        DestroyIfEmpty ();
    }

    public void DestroyIfEmpty ()
    {
        if (items.Count == 0 && characters.Count == 0 && map == null && ChunkManager.Manager) //should test for colliders too, but don't need to atm
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
        //listeningInventories.Clear();
    }

    public T[] GetItems<T>() where T : Item
    {
        return items.OfType<T>().ToArray();
    }

    public T[] GetCharacters<T>() where T : Character
    {
        return characters.OfType<T>().ToArray();
    }

    //public T[] GetObjects<T> (T[] array, IEnumerable<T> collection, bool clearArray = false) where T : MonoBehaviour
    //{
    //    if (clearArray)
    //    {
    //        Array.Clear(array, 0, array.Length);
    //    }

    //    var type = typeof(T);
    //    int count = array.Length;

    //    foreach (var behavior in collection)
    //    {
    //        if (type.IsAssignableFrom(behavior.GetType()))
    //        {
    //            count++;
    //            Array.Resize(ref array, count);
    //            array[count - 1] = behavior as T;
    //        }
    //    }

    //    return array;
    //}
}

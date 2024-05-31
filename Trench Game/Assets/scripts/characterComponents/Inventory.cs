using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Inventory : MonoBehaviour
{
    public Character character;
    public float passivePickupRad = 1, activePickupRad = 2; //passive should be smaller than active
    public List<Item> items = new();
    public List<Item> withinRadius = new();
    public Chunk[,] chunks = new Chunk[0,0];
    public Item closestItem;
    public bool debugLines = false;

    private void Start()
    {
        DetectItems();
    }

    public void DetectItems()
    {
        withinRadius.Clear();

        UpdateChunks();

        foreach (var chunk in chunks)
        {
            if (chunk == null) continue;
            for (int i = 0; i < chunk.items.Count; i++)
            {
                var item = chunk.items[i];
                DetectItem(item);

                if (!chunk.items.Contains(item)) i--;
            }
        }
    }

    public void UpdateChunks ()
    {
        var radius = activePickupRad;

        var min = (Vector2)transform.position - Vector2.one * radius;
        var max = (Vector2)transform.position + Vector2.one * radius;
        var newChunks = ChunkManager.Manager.ChunksFromBox(min, max);
        AddChunkListeners(chunks, newChunks);
        chunks = newChunks;
    }

    public void AddChunkListeners (Chunk[,] oldChunks, Chunk[,] newChunks)
    {
        if (oldChunks == newChunks) return;

        //find old
        foreach (var newChunk in newChunks)
        {
            if (newChunk == null) continue;

            bool found = false;

            foreach (var oldChunk in oldChunks)
            {
                if (oldChunk == newChunk)
                {
                    found = true;
                    break;
                }
            }

            if (!found) //if new chunk is not within old chunks
            {
                newChunk.onNewItem.AddListener(DetectItem);
            }
        }        
        
        foreach (var oldChunk in oldChunks)
        {
            if (oldChunk == null) continue;

            bool found = false;

            foreach (var newChunk in newChunks)
            {
                if (oldChunk == newChunk)
                {
                    found = true;
                    break;
                }
            }

            if (!found) //if old chunk is not in new chunks
            {
                oldChunk.onNewItem.RemoveListener(DetectItem);
            }
        }
    }

    public void DetectItem(Item item)
    {
        if (item.wielder) return; //this should never be true if on the ground

        var dist = Vector2.Distance(transform.position, item.transform.position);

        if (dist > activePickupRad) return;

        if (item.passivePickup && dist <= passivePickupRad)
        {
            item.Pickup(character, out var pickedUp, out _);
            if (!pickedUp)
            {
                withinRadius.Add(item);
            }
        }
        else
        {
            withinRadius.Add(item);
        }
    }

    public void PickupItem (Item item)
    {
        //if (item.wielder) return; //don't have to run this twice

        if (withinRadius.Contains(item))
        {
            item.Pickup(character, out var pickedUp, out var destroyed);
            if (pickedUp)
                withinRadius.Remove(item);

            if (pickedUp && !destroyed)
                items.Add(item);


            if (item is Gun gun)
            {
                //if (character.gun != null && character.gun.model == item.model)
                //{
                //put unload logic here
                //}
                //else
                if (character.gun) DropItem(character.gun); //we only want to drop the gun when they already have the gun

                character.gun = gun;
            }
        }
    }

    public void DropItem (Item item)
    {
        if (item is Gun)
        {
            character.gun = null;
        }

        item.Drop();

        items.Remove(item);
        //withinRadius.Add(item); //its already being added by the chunk event dipshit
    }

    public void DropAllItems()
    {
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[0];
            DropItem(item);
        }
    }

    public void DropPrevItem ()
    {
        var item = items[^1];
        DropItem(item);
    }

    public Item SelectClosest (Vector2 pos)
    {
        var closestDist = Mathf.Infinity;
        Item closestItem = null;

        foreach (var item in withinRadius)
        {
            var dist = Vector2.Distance(item.transform.position, pos);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestItem = item;
            }
        }

        this.closestItem = closestItem;
        return closestItem;
    }

    public void PickupClosest ()
    {
        PickupItem(closestItem);
    }

    private void OnDrawGizmos()
    {
        if (debugLines)
        {
            GeoFuncs.DrawCircle(transform.position, passivePickupRad, Color.green);
            GeoFuncs.DrawCircle(transform.position, activePickupRad, Color.blue);

            foreach (var item in withinRadius)
            {
                Color color;

                if (item == closestItem) color = Color.magenta;
                else color = Color.cyan;

                GeoFuncs.MarkPoint(item.transform.position, .5f, color);
            }
        }
    }

    private void OnDestroy()
    {
        foreach (var chunk in chunks)
        {
            if (chunk == null) continue;

            chunk.onNewItem.RemoveListener(DetectItem);
        }
    }
}

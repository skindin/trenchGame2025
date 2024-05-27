using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Inventory : MonoBehaviour
{
    public Character character;
    public float passivePickupRad = 1, activePickupRad = 2; //passive should be smaller than active
    public readonly List<Item> items = new();
    public readonly List<Item> withinRadius = new();
    public Chunk[,] chunks;
    float closestDist = Mathf.Infinity;
    Item closestItem;

    public void DetectItems()
    {
        withinRadius.Clear();

        var radius = activePickupRad;

        var min = (Vector2)transform.position + Vector2.one * radius;
        var max = (Vector2)transform.position - Vector2.one * radius;
        chunks = ChunkManager.Manager.ChunksFromBox(min, max);

        closestDist = Mathf.Infinity;
        closestItem = null;

        foreach (var chunk in chunks)
        {
            if (chunk == null) continue;
            foreach (var item in chunk.items)
            {
                DetectItem(item);
            }
        }
    }

    public void DetectItem(Item item)
    {
        var dist = Vector2.Distance(transform.position, item.transform.position);

        if (dist > activePickupRad) return;

        if (item.passivePickup && dist <= passivePickupRad)
        {
            item.Pickup(character);
        }
        else
        {
            withinRadius.Add(item);

            if (dist < closestDist)
            {
                closestDist = dist;
                closestItem = item;
            }
        }
    }

    public void PickupItem (Item item)
    {
        if (withinRadius.Contains(item))
        {
            item.Pickup(character);
        }
    }

    public void PickupClosest ()
    {
        if (closestItem != null) closestItem.Pickup(character);
    }
}

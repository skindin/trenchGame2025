using System;
using System.Collections;
using System.Collections.Generic;
//using UnityEditor.UI;
using UnityEngine;
using UnityEngine.Events;

public class Inventory : MonoBehaviour
{
    public Character character;
    public float passivePickupRad = 1, activePickupRad = 2, selectionRad = .5f; //passive should be smaller than active
    public List<Item> items = new();
    public List<Item> withinRadius = new();
    public Chunk[,] chunks = new Chunk[0,0];
    Item selectedItem;
    public Action<Item> onItemAdded, onItemRemoved;

    private void Awake()
    {
        onItemAdded = item => DetectItem(item);
        onItemRemoved = item => withinRadius.Remove(item);
    }

    //public Action<Item> OnNewItem
    //{
    //    get => onNewItem;
    //    set => onNewItem = value ?? (_ => { }); // Ensure the value is not null
    //}

    public Item SelectedItem
    {
        get
        {
            if (selectedItem && (selectedItem.wielder || !selectedItem.gameObject.activeInHierarchy))
            {
                selectedItem = null;
            }

            return selectedItem;
        }

        set
        {
            selectedItem = value;
        }
    }

    public bool debugLines = false;

    //private void Start()
    //{
    //    DetectItems();
    //}

    public void ResetInventory (bool dropAllItems = false)
    {
        SelectedItem = null;
        var emptyChunkArray = new Chunk[0,0];
        AddChunkListeners(chunks, emptyChunkArray);
        chunks = emptyChunkArray;

        if (dropAllItems)
        {
            DropAllItems();
        }

        withinRadius.Clear();
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

                if (!item.gameObject.activeSelf)
                {
                    Debug.DrawLine(item.transform.position, transform.position, Color.red);
                    GeoUtils.MarkPoint(item.transform.position, 1, Color.red);
                    GeoUtils.MarkPoint(transform.position, 1, Color.red);
                    //Debug.LogError($"Item {item} isn't active but is refferenced by chunk {chunk.adress}");
                }

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
        var newChunks = ChunkManager.Manager.ChunksFromBoxMinMax(min, max);
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
                newChunk.listeningInventories.Add(this);
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
                oldChunk.listeningInventories.Remove(this);
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
        if (item.wielder)
        {
            withinRadius.Remove(item);
            return;
        }//actually, I do have to run this, because sometimes items are picked up by other characters, and the chunk won't alert them of that
        //would probably better for the chunk to manage all this...

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

        //var pos = UnityEngine.Random.insideUnitCircle * activePickupRad;
        var pos = character.transform.position;
        item.Drop(pos);

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
        if (items.Count > 0)
        {
            var item = items[^1];
            DropItem(item);
        }
    }

    public Item SelectClosest (Vector2 pos)
    {
        SelectedItem = LogicAndMath.GetClosest(pos, withinRadius.ToArray(), item => item.transform.position, out _, null, null, selectionRad, debugLines);
        return SelectedItem;
    }

    public void PickupClosest ()
    {
        if (SelectedItem)
        {
            PickupItem(SelectedItem);
            SelectedItem = null;
        }
    }

    private void OnDrawGizmos()
    {
        if (debugLines)
        {
            GeoUtils.DrawCircle(transform.position, passivePickupRad, Color.green);
            GeoUtils.DrawCircle(transform.position, activePickupRad, Color.blue);

            foreach (var item in withinRadius)
            {
                Color color;

                if (item == SelectedItem) color = Color.magenta;
                else color = Color.cyan;

                GeoUtils.MarkPoint(item.transform.position, .5f, color);
            }
        }
    }

    public void OnRemoved ()
    {
        foreach (var chunk in chunks)
        {
            if (chunk == null) continue;

            chunk.listeningInventories.Remove(this);
        }

        SelectedItem = null;
    }

    //private void OnDestroy()
    //{
    //    foreach (var chunk in chunks)
    //    {
    //        if (chunk == null) continue;

    //        chunk.onNewItem.RemoveListener(DetectItem);
    //    }
    //}
}

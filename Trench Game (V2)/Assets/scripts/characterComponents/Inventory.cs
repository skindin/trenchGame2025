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
    Item cachedActiveItem;
    Weapon cachedActiveWeapon;

    public Weapon ActiveWeapon
    {
        get
        {
            return cachedActiveWeapon;
        }

        private set
        {
            cachedActiveWeapon = value;
        }
    }

    public Item ActiveItem
    {
        set
        {
            if (value is Weapon weapon)
            {
                ActiveWeapon = weapon;
            }
            else
            {
                ActiveWeapon = null;
            }

            cachedActiveItem = value;
        }

        get
        {
            return cachedActiveItem;
        }
    }

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

    private void Update()
    {
        if (debugLines)
        {
            foreach (var chunk in chunks)
            {
                if (chunk == null)
                    continue;

                ChunkManager.Manager.DrawChunk(chunk, Color.magenta);
            }
        }
    }

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

        ActiveItem = null;
    }

    public void DetectItems()
    {
        withinRadius.Clear();

        UpdateChunks(false);

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

    public void UpdateChunks (bool updateListeners = false)
    {
        var radius = activePickupRad;

        var min = (Vector2)transform.position - Vector2.one * radius;
        var max = (Vector2)transform.position + Vector2.one * radius;
        var newChunks = ChunkManager.Manager.ChunksFromBoxMinMax(min, max);
        if (updateListeners)
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


    /// <summary>
    /// Only to be used when an item is to be destroyed while a character is holding it
    /// </summary>
    /// <param name="item"></param>
    public void RemoveItem (Item item)
    {
        if (!items.Contains(item)) //if the item is not in the inventory, do nothing
            return;

        if (item == ActiveItem)
        {
            ActiveItem = null;
        }

        items.Remove(item);
        //withinRadius.Add(item);
    }

    public void DetectItem(Item item)
    {
        if (item.wielder) return; //this should never be true if on the ground

        var dist = Vector2.Distance(transform.position, item.transform.position);

        if (dist > activePickupRad) //bruh the first time i've ever had to do ipsilon addition
            return;

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

    public void PickupItem(Item item, Vector2 dropPos, bool sync = false)
    {
        if (item.wielder)
        {
            withinRadius.Remove(item);
            return;
        }//actually, I do have to run this, because sometimes items are picked up by other characters, and the chunk won't alert them of that
        //would probably better for the chunk to manage all this...

        if (true || withinRadius.Contains(item))
        {
            item.Pickup(character, out var pickedUp, out var destroyed);
            if (pickedUp)
                withinRadius.Remove(item);

            if (pickedUp && !destroyed)
            {
                if (ActiveItem)
                    DropItem(ActiveItem, dropPos); //DO NOT SYNC HERE, pickup message should take care of it

                items.Add(item);
                ActiveItem = item;
            }

            if (sync)
            {
                NetworkManager.Manager.PickupItem(item,dropPos); //bruh not everything is picked up this way facepalm emoji
            }

            //if (item is Gun
            //    //|| item is MedPack
            //    ) //temporary, idk if ill like this
            //{
            //    //if (character.gun != null && character.gun.model == item.model)
            //    //{
            //    //put unload logic here
            //    //}
            //    //else
            //    if (character.gun) DropItem(character.gun, dropPos); //we only want to drop the gun when they already have the gun

            //    if (item is Gun gun)
            //        character.gun = gun;
            //}
        }
    }

    public void DropItem (Item item)
    {
        DropItem(item, transform.position);
        //withinRadius.Add(item); //its already being added by the chunk event dipshit
    }

    public void DropItem(Item item, Vector2 pos, bool sync = false)
    {
        var delta = pos - (Vector2)transform.position;
        var clampedDelta = Vector2.ClampMagnitude(delta, activePickupRad- .001f);
        var clampedPos = clampedDelta + (Vector2)transform.position;

        if (sync)
        {
            NetworkManager.Manager.DropItemClient(pos);
        }

        //var pos = UnityEngine.Random.insideUnitCircle * activePickupRad;
        item.Drop(clampedPos, out _);

        RemoveItem(item);

    }

    public void DropAllItems(bool sync = false)
    {
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[0];

            DropItem(item, transform.position, sync);
            i--;
        }
    }

    public void DropActiveItem ()
    {
        DropItem(ActiveItem);
    }

    public void DropPrevItem()
    {
        if (items.Count > 0)
        {
            var item = items[^1];
            DropItem(item);
        }
    }


    /// <summary>
    /// WARNING: ALWAYS SYNCS
    /// </summary>
    /// <param name="pos"></param>
    public void DropPrevItem(Vector2 pos)
    {
        if (items.Count > 0)
        {
            var item = items[^1];
            DropItem(item, pos, true);
        }
    }

    public Item SelectClosest (Vector2 pos)
    {
        SelectedItem = LogicAndMath.GetClosest(pos, withinRadius.ToArray(), item => item.transform.position, out _, null, null, selectionRad, debugLines);
        return SelectedItem;
    }


    /// <summary>
    /// WARNING: ALWAYS SYNCS
    /// </summary>
    /// <param name="dropPos"></param>
    public void PickupClosest (Vector2 dropPos)
    {
        if (SelectedItem)
        {
            PickupItem(SelectedItem, dropPos, true);
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

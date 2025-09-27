using System;
using System.Collections;
using System.Collections.Generic;
//using UnityEditor.UI;
using UnityEngine;
using UnityEngine.Events;

public class Inventory : MonoBehaviour //separate bevavior for inventory shrugging emoji
{
    public Character character;
    public float passivePickupRad = 1, //how close the character must be to automatically pickup items (that can be auto-picked up)
        activePickupRad = 2, //how close they must be to manually pickup items (should be more than or equal to passive radius)
        selectionRad = .5f; //passive should be smaller than active
    int currentSlot = 0;//index of current active clot
    public Item[] itemSlots; //it is an array for static size and empty slots (im probably overexplaining lol)
    public bool inventoryFull = false; //can't remember what this is for
    public List<Item> withinRadius = new(); //items within active pickup radius (this is only a property for ui purposes)
    //public Chunk[,] chunks = new Chunk[0,0];
    Item selectedItem; //item ON THE GROUND that the character is inspecting (ik, kind of messy, but really just for temporary ui... i think...)
    public Action<Item> onItemAdded, onItemRemoved; //these are both mainly intended for bot listening so ignore them
    public Transform itemContainer; //which game object transform we want to contain the items we pick up (mostly to easily change item pivots)
    IEnumerable<Vector2Int> chunkAddresses; //a relic from a past system...? i don't think this even needs to be a property

    public Weapon ActiveWeapon //basically just quick test to see if the character is even holding a weapon type item
    {
        get
        {
            if (ActiveItem is Weapon weapon)
                return weapon;
            else
                return null;
        }

        private set
        {
            ActiveItem = value;
        }
    }

    public Item ActiveItem //i gotta be careful with this fs
    {
        set
        {
            itemSlots[currentSlot] = value;
        }

        get
        {
            return itemSlots[currentSlot];
        }
    }

    public int CurrentSlot
    {
        get
        {
            return currentSlot;
        }

        set
        {
            var prevSlot = currentSlot;
            currentSlot = (int)Mathf.Repeat(value, itemSlots.Length);

            if (prevSlot != currentSlot)
            {
                if (itemSlots[prevSlot])
                    itemSlots[prevSlot].ToggleActive(false);
                if (itemSlots[currentSlot])
                    itemSlots[currentSlot].ToggleActive(true);
            }
        }
    }

    int? GetEmptySlot ()
    {
        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (itemSlots[i] == null)
            {
                return i;
            }
        }

        return null;
    }

    public int? GetSlotWithItem(Func<Item, bool> condition)
    {
        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (condition(itemSlots[i]))
            {
                return i;
            }
        }

        return null;
    }

    public bool SetSlotToItem(Func<Item, bool> condition)
    {
        var slot = GetSlotWithItem(condition);

        if (slot != null)
        {
            CurrentSlot = (int)slot;
            return true;
        }

        return false;
    }


    private void Awake()
    {
        //itemSlots = new Item[slotCount];

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
        //if (debugLines)
        //{
        //    foreach (var chunk in chunks)
        //    {
        //        if (chunk == null)
        //            continue;

        //        ChunkManager.Manager.DrawChunk(chunk, Color.magenta);
        //    }
        //}
    }

    public Item SelectedItem //this is for the sake of highlighting ground items, not the item the character is holding. tbh, this should probably be in a different script
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

    public void ResetInventory (bool dropAllItems = false, float dropRadius = 0)
    {
        SelectedItem = null;
        //var emptyChunkArray = new Chunk[0,0];
        //AddChunkListeners(chunks, emptyChunkArray);
        //chunks = emptyChunkArray;

        if (dropAllItems)
        {
            DropAllItems(dropRadius);
        }

        withinRadius.Clear();

        ActiveItem = null;
    }

    public void DetectItems() //gets list of all items within active pickup radius
    {
        withinRadius.Clear();

        UpdateChunks(false);

        foreach (var item in ItemManager.Manager.chunkArray.ObjectsFromAddresses(chunkAddresses))
        {
            if (!item) continue;
            //for (int i = 0; i < chunk.items.Count; i++)
            //{
            //    var item = chunk.items[i];

                //if (!item.gameObject.activeSelf)
                //{
                //    Debug.DrawLine(item.transform.position, transform.position, Color.red);
                //    GeoUtils.MarkPoint(item.transform.position, 1, Color.red);
                //    GeoUtils.MarkPoint(transform.position, 1, Color.red);
                //    //Debug.LogError($"Item {item} isn't active but is refferenced by chunk {chunk.adress}");
                //}

                DetectItem(item);

                //if (!chunk.items.Contains(item)) i--;
            //}
        }
    }

    public void UpdateChunks (bool updateListeners = false)
    {
        var radius = activePickupRad;

        var min = (Vector2)transform.position - Vector2.one * radius;
        var max = (Vector2)transform.position + Vector2.one * radius;
        chunkAddresses = Chunks.ChunkManager.AddressesFromBoxMinMax(min, max);
        //var newChunks = ChunkManager.Manager.ChunksFromBoxMinMax(min, max);
        ////if (updateListeners)
        ////    AddChunkListeners(chunks, newChunks);
        //chunks = newChunks;
    }


    /// <summary>
    /// Only to be used when an item is to be destroyed while a character is holding it (mostly for consumables)
    /// </summary>
    /// <param name="item"></param>
    public void RemoveItem (Item item)
    {
        for (var i = 0; i < itemSlots.Length; i++)
        {
            var slotItem = itemSlots[i];

            if (item == slotItem)
            {
                if (item == ActiveItem)
                {
                    ActiveItem = null;
                }

                itemSlots[i] = null;

                //Debug.Log($"item was removed from inventory");

                return;
            }
        }

        Debug.LogError($"character {character.id} {character.characterName} does not have item {item.id} {item.itemName} in their inventory");
        //withinRadius.Add(item);
    }

    public void DetectItem(Item item)
    {
        if (item.wielder) return; //this should never be true if on the ground

        var dist = Vector2.Distance(transform.position, item.transform.position);

        if (dist > activePickupRad) //bruh the first time i've ever had to do ipsilon addition
            //i have no idea what i meant by this
            return;

        if (item.passivePickup && dist <= passivePickupRad)
        {
            //Debug.Log($"attempting auto pickup...");
            item.Pickup(character, out var pickedUp, out _, true);
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

    /// <summary>
    /// removes item from ground, determines inventory placement, and drops previous item at dropPos
    /// </summary>
    /// <param name="item"></param>
    /// <param name="dropPos"></param>
    /// <param name="sync"></param>
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
            //var prevItem = ActiveItem;

            if (item.CanPickUp(character))
            {
                item.Pickup(character, out var pickedUp, out var wasDestroyed, sync);
                if (pickedUp && !wasDestroyed)
                {
                    withinRadius.Remove(item);


                    if (ActiveItem)
                    {
                        var emptySlot = GetEmptySlot();
                        if (emptySlot.HasValue) //if there is an empty slot
                        {
                            itemSlots[emptySlot.Value] = item;
                            item.ToggleActive(false);
                            //CurrentSlot = emptySlot;
                        }
                        else //if there isn't an empty slot
                        {
                            DropItem(ActiveItem, dropPos, sync); //DO NOT SYNC HERE, pickup message should take care of it

                            ActiveItem = item;
                        }
                    }
                    else
                    {
                        ActiveItem = item;
                    }

                }
            }

            //if (sync)
            //{
            //    NetworkManager.Manager.PickupItem(item);
            //}

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
    }

    /// <summary>
    /// detaches item transform, removes from inventory array, and places item at position
    /// </summary>
    /// <param name="item"></param>
    /// <param name="pos"></param>
    /// <param name="sync"></param>
    public void DropItem(Item item, Vector2 pos, bool sync = false)
    {
        var delta = pos - (Vector2)transform.position;
        var clampedDelta = Vector2.ClampMagnitude(delta, activePickupRad- .001f);
        var clampedPos = clampedDelta + (Vector2)transform.position;

        if (sync)
        {
            NetworkManager.Manager.DropItem(item, pos);
        }

        //var pos = UnityEngine.Random.insideUnitCircle * activePickupRad;
        item.ToggleActive(true);

        item.Drop(clampedPos, out _);

        RemoveItem(item);
    }

    /// <summary>
    /// drops all items at random positions within drop radius from this transform position
    /// </summary>
    /// <param name="dropRadius"></param>
    /// <param name="sync"></param>
    public void DropAllItems(float dropRadius, bool sync = false)
    {
        for (int i = 0; i < itemSlots.Length; i++)
        {
            var item = itemSlots[i];

            if (!item) continue;

            var dropPos = UnityEngine.Random.insideUnitCircle * dropRadius + (Vector2)transform.position;

            DropItem(item, dropPos, sync);
        }
    }

    public void DropActiveItem ()
    {
        DropItem(ActiveItem);
    }

    //public void DropPrevItem()
    //{
    //    if (itemSlots.Length > 0)
    //    {
    //        var item = itemSlots[^1];
    //        DropItem(item);
    //    }
    //}


    /// <summary>
    /// drops current active item at position. WARNING: ALWAYS SYNCS
    /// </summary>
    /// <param name="pos"></param>
    public void DropActiveItem(Vector2 pos)
    {
        if (ActiveItem)
        {
            //var item = ActiveItem
            DropItem(ActiveItem, pos, true);
        }
    }

    public Item SelectClosest (Vector2 pos) //again, for temporary ui purposes
    {
        SelectedItem = CollectionUtils.GetClosest(pos, withinRadius, item => item.transform.position, out _, null, null, selectionRad, debugLines);
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

    public void OnRemoved () //idk what this for
    {
        //foreach (var chunk in chunks)
        //{
        //    if (chunk == null) continue;

        //    chunk.listeningInventories.Remove(this);
        //}

        SelectedItem = null;
    }

    public string GetSlotsString () //just returns string for-you guessed it-temporary ui purposes
    {
        string slots = "";

        for (int i = 0; i < itemSlots.Length; i++)
        {
            var item = itemSlots[i];

            var activeSlot = currentSlot == i;

            slots += $"{(activeSlot ? "[" : " ")}{(item ? item.itemName : "-")}{(activeSlot ? "]" : " ")}";
        }

        return slots;
    }

    public void Aim (Vector2 direction) //for aiming items like guns and... idk what else...
    {
        if (ActiveItem is IDirectionalAction directional)
        {
            directional.Aim(direction);

            var angle = Vector2.SignedAngle(Vector3.up, direction);
            itemContainer.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            //Debug.DrawRay(itemContainer.position, direction.normalized, Color.red);
        }
        else
        {
            itemContainer.transform.rotation = Quaternion.identity;
        }
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

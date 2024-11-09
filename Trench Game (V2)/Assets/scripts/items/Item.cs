using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
//using UnityEngine.Events;
//using static UnityEditor.Progress;

public class Item : MonoBehaviour
{
    public string itemName;
    public int id, prefabId;
    public Transform defaultContainer;
    public Character wielder;
    //public bool currentlyHeld = false;
    public static List<Item> all = new();
    Chunk chunk = null;
    public Chunk Chunk
    {
        get
        {
            return chunk;
        }

        set
        {
            if (chunk == value)
            {
                //Debug.Log($"Item {this} {gameObject.GetInstanceID()} chunk was already {(chunk == null ? "null" : $"chunk {chunk.adress}")}");
                return;
            }

            chunk?.RemoveItem(this);

            chunk = value; //you wouldn't BELIEVE how important the order of these three lines is

            chunk?.AddItem(this);

            //Debug.Log($"Item {this}{gameObject.GetInstanceID()} chunk was set to {(value == null ? "null" : $"Chunk {chunk.adress}")}");
        }
    }

    public virtual string Verb { get; } = "use";

    public bool passivePickup = false;

    private void Awake()
    {
        //Debug.Log(gameObject.name + " ran base item awake function");
        //ResetItem();
        ItemAwake();

        //currentlyHeld = wielder;
    }

    public virtual void ItemAwake ()
    {
        if (!all.Contains(this))
            all.Add(this);
    }

    private void Start()
    {
        //if (!wielder)
        //    UpdateChunk();

        ItemStart();
    }

    public virtual void ItemStart()
    {

    }

    private void OnDestroy()
    {
        all.Remove(this);

        if (chunk != null)
            chunk.RemoveItem(this);
    }

    private void Update()
    {
        ItemUpdate();
    }

    public virtual void ItemUpdate ()
    {
        //shrugging emoji
    }

    public virtual void Action()
    {

    }

    public virtual bool CanPickUp (Character character)
    {
        return true;
    }

    public void UpdateChunk()
    {
        Chunk = ChunkManager.Manager.ChunkFromPosClamped(transform);
    }

    public virtual void Pickup (Character character, out bool wasPickedUp, out bool wasDestroyed, bool sync)
    {
        if (wielder != character)
        {
            wielder = character;
            transform.parent = character.inventory.itemContainer;
            transform.localPosition = Vector3.zero;
        }

        if (sync) 
            NetworkManager.Manager.PickupItem(wielder, this);

        Chunk = null;
        wasDestroyed = false;
        wasPickedUp = true;
    }

    public void Pickup (Character character, bool sync) //not overidable, becase it's just a shorthand
    {
        Pickup(character, out _, out _, sync);
    }


    /// <summary>
    /// Returns true if the item has been removed from the world
    /// </summary>
    public virtual void DropLogic (Vector2 pos, out bool destroyedSelf)
    {
        wielder = null;

        transform.SetParent(defaultContainer);
        transform.position = pos;
        transform.rotation = Quaternion.identity;
        //UpdateChunk();
        destroyedSelf = false;

        //Debug.Log($"Item {this} {this.gameObject.GetInstanceID()} was dropped");
    }

    public void Drop(Vector2 pos, out bool destroyedSelf)
    {
        DropLogic(pos, out destroyedSelf);
        if (!destroyedSelf && gameObject.activeSelf)
            UpdateChunk();
    }

    public void Drop (Vector2 pos)
    {
        Drop(pos, out _);
    }

    public void DestroyItem ()
    {
        //destroy logic here shruggin emoji
        if (NetworkManager.IsServer)
            SpawnManager.Manager.RemoveItem(this);
        else
            ItemManager.Manager.RemoveItem(this);

        if (wielder)
        {
            wielder.inventory.RemoveItem(this);
        }
    }

    public virtual void ResetItem ()
    {
        ItemAwake();
        wielder = null;
    }

    public virtual void ToggleActive (bool active)
    {
        gameObject.SetActive(active);
    }

    public void DeParent()
    {
        transform.SetParent(defaultContainer);
    }

    public virtual string InfoString(string separator = " ")
    {
        return itemName;// { $"Tier {model.tier}" };
    }

    //public virtual DataDict<object> PublicData //to be used when observing an item that another character is holding
    //{
    //    get
    //    {
    //        return new(
    //        (Naming.id, id),
    //        (Naming.name, model.name),
    //        (Naming.pos, new DataDict<float>((Naming.x, transform.position.x), (Naming.y, transform.position.y))),
    //        (Naming.wielderId, (wielder) ? wielder.id : -1)
    //        );
    //    }
    //}

    //public virtual DataDict<object> PrivateData //to be used when observing an item not being held or being held by wielder
    //{
    //    get
    //    {
    //        return PublicData;
    //    }
    //}
}

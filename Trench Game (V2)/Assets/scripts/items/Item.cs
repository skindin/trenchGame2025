using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Item : MonoBehaviour
{
    public Transform defaultContainer;
    public ItemModel model;
    public Character wielder;
    //public bool currentlyHeld = false;
    public static List<Item> all = new();
    Chunk chunk;
    public Chunk Chunk
    {
        get
        {
            return chunk;
        }

        set
        {
            if (chunk == value) return;

            if (chunk != null)
            {
                chunk.RemoveItem(this);
            }

            if (value != null)
            {
                value.AddItem(this);
            }
            chunk = value;
        }
    }

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
        all.Add(this);
    }

    private void Start()
    {
        if (!wielder)
            UpdateChunk();

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

    public void UpdateChunk()
    {
        Chunk = ChunkManager.Manager.ChunkFromPosClamped(transform);
    }

    public virtual void Pickup (Character character, out bool wasPickedUp, out bool wasDestroyed)
    {
        if (wielder != character)
        {
            wielder = character;
            transform.parent = character.transform;
            transform.localPosition = Vector3.zero;
        }

        Chunk = null;
        wasDestroyed = false;
        wasPickedUp = true;
    }

    public void Pickup (Character character) //not overidable, becase it's just a shorthand
    {
        Pickup(character, out _, out _);
    }


    /// <summary>
    /// Returns true if the item has been removed from the world
    /// </summary>
    public virtual void Drop ()
    {
        wielder = null;
        UpdateChunk();

        transform.SetParent(defaultContainer);
        transform.rotation = Quaternion.identity;
    }

    public virtual void DestroyItem ()
    {
        //destroy logic here shruggin emoji
        Chunk = null;
        ItemManager.Manager.RemoveItem(this);
    }

    public virtual void ResetItem ()
    {
        ItemAwake();
        wielder = null;
    }

    public virtual string GetInfo(string separator = " ")
    {
        return model.name;// { $"Tier {model.tier}" };
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Item : MonoBehaviour
{
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
        Chunk = ChunkManager.Manager.ChunkFromPos(transform.position, true);
    }

    public virtual bool Pickup (Character character)
    {
        if (wielder == null)
        {
            wielder = character;
            transform.parent = character.transform;
            transform.localPosition = Vector3.zero;
        }

        Chunk = null;
        return true;
    }


    /// <summary>
    /// Returns true if the item has been removed from the world
    /// </summary>
    public virtual void Drop ()
    {
        wielder = null;
        UpdateChunk();

        transform.SetParent(null);
        transform.rotation = Quaternion.identity;
    }

    public virtual void DestroyItem ()
    {
        //destroy logic here shruggin emoji
        Destroy(gameObject, Time.deltaTime);
    }

    public virtual string[] GetInfo ()
    {
        return new string[0];// { $"Tier {model.tier}" };
    }
}

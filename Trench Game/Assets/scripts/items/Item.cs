using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Item : MonoBehaviour
{
    public ItemModel model;
    public Character wielder;
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
                chunk.items.Remove(this);
            }

            if (value != null)
            {
                chunk.items.Add(this);
            }



            chunk = value;
        }
    }

    public bool passivePickup = false;

    private void Awake()
    {
        //Debug.Log(gameObject.name + " ran base item awake function");
        ItemAwake();
    }

    public virtual void ItemAwake ()
    {
        all.Add(this);
    }

    private void Start()
    {
        UpdateChunk();
    }

    private void OnDestroy()
    {
        all.Remove(this);
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

    public virtual void Pickup (Character character)
    {
        if (wielder == null)
            wielder = character;

        Chunk = null;
    }

    public virtual void Drop ()
    {
        wielder = null;
        UpdateChunk();
    }

    public virtual void DestroyItem ()
    {
        //destroy logic here shruggin emoji
        Destroy(gameObject, Time.deltaTime);
    }
}

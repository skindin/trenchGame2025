using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Item : MonoBehaviour
{
    public ItemModel model;
    public Character wielder;
    public static List<Item> all = new();
    public Chunk chunk;
    public bool passivePickup = false;
    public float passivePickupRadius = .5f;

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
        if (wielder == null && passivePickup)
        {
            foreach (var character in chunk.characters)
            {
                var dist = Vector2.Distance(character.transform.position, transform.position);
                if (dist < passivePickupRadius)
                {
                    Pickup(character);
                    break;
                }
            }
        }
    }

    public void UpdateChunk()
    {
        chunk = ChunkManager.Manager.ChunkFromPos(transform.position, true);
    }

    public virtual void Pickup (Character character)
    {
        wielder = character;
    }

    public virtual void Drop ()
    {
        wielder = null;
        UpdateChunk();
    }

    public virtual void DestroyItem ()
    {
        //destroy logic here shruggin emoji
        Destroy(gameObject, .0001f);
    }
}

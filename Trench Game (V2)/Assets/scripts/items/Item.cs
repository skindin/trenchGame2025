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
    //public static List<Item> all = new();
    public Vector3 groundRot, heldRot, heldPos;

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
        //if (!all.Contains(this))
        //    all.Add(this);
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

    //private void OnDestroy()
    //{
    //    all.Remove(this);

    //    //ItemManager.Manager.chunkArray.up
    //}

    private void Update()
    {
        ItemUpdate();
        if (!wielder)
        {
            UpdateChunk();
        }
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
        ItemManager.Manager.chunkArray.UpdateObjectChunk(this);
    }

    public virtual void Pickup (Character character, out bool wasPickedUp, out bool wasDestroyed, bool sync)
    {
        if (wielder != character)
        {
            wielder = character;
            var prevScale = transform.localScale;
            transform.parent = character.inventory.itemContainer;
            transform.localScale = prevScale;
            //transform.localPosition = Vector3.zero;
        }

        if (sync) 
            NetworkManager.Manager.PickupItem(wielder, this);

        //Chunk = null;
        wasDestroyed = false;
        wasPickedUp = true;

        HeldOrientation();
    }

    public void HeldOrientation ()
    {
        transform.localRotation = Quaternion.Euler(heldRot);
        transform.localPosition = heldPos;
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

        transform.parent = defaultContainer;
        transform.position = pos;
        //transform.rotation = Quaternion.identity;
        //UpdateChunk();
        destroyedSelf = false;

        transform.localScale = Vector3.one;

        transform.rotation = Quaternion.Euler(groundRot);

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

        HeldOrientation();
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

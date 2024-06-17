using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//no create asset menu, this is just a parent class
public abstract class ItemModel : ScriptableObject
{
    readonly static List<ItemModel> all = new();

    public ItemModel ()
    {
        all.Add(this);
    }

    int id = -1;
    public int Id
    {
        get
        {
            if (id == -1)
            {
                id = all.IndexOf (this);
            }

            return id;
        }
    }

    //public int tier = 1;
    //public Item prefab;

    public List<ItemTags> Tags = new List<ItemTags>();

    public enum ItemTags
    {
        resource,
        weapon,
        projectile,
        gun,
        amo,
    }
}

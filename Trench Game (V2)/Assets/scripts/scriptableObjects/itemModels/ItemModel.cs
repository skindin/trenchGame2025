using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//no create asset menu, this is just a parent class
public abstract class ItemModel : ScriptableObject
{
    //public int tier = 1;
    //public Item prefab;

    public List<ItemTags> Tags = new ();

    public enum ItemTags
    {
        resource,
        weapon,
        projectile,
        gun,
        ammo,
        tool,
    }
}

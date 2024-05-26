using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ItemManager : MonoBehaviour
{
    static ItemManager manager;

    public static ItemManager Manager
    {
        get
        {
            if (manager == null)
            {
                manager = FindObjectOfType<ItemManager>();
                //if (manager == null)
                //{
                //    GameObject go = new GameObject("Bullet");
                //    manager = go.AddComponent<BulletManager>();
                //    DontDestroyOnLoad(go);
                //}
            }
            return manager;
        }
    }

    public List<Item> itemPrefabs = new();

    public List<TierGroup> tierGroups = new();

    public void Sort ()
    {
        foreach (var item in itemPrefabs)
        {
            var group = tierGroups.Find(x => x.tier == item.model.tier);

            if (group == null)
            {
                group = new(new() {item}, item.model.tier);
                tierGroups.Add(group);
            }
            else
            {
                group.items.Add(item);
            }
        }
    }


    /// <summary>
    /// Generates a list of refferences to prefabs. These prefabs must still be instantiated.
    /// </summary>
    /// <returns></returns>
    public List<Item> GenerateItemList ()
    {
        return null;//one sec
    }

    [System.Serializable]
    public class TierGroup
    {
        public List<Item> items;
        public int tier;

        public TierGroup (List<Item> items, int tier)
        {
            this.items = items;
            this.tier = tier;
        }
    }
}
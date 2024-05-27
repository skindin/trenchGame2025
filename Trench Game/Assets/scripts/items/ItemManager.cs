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

    public float itemDropRadius = 5, minCount = 1, maxCount = 10, avgCount = 4, countConc = .5f;

    private void Start()
    {
        Sort();
        SpawnItems(Vector2.zero);
    }

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
    public List<Item> GenerateItemList(List<Item> list, float minCount, float maxCount, float avgCount, float countConc, bool clearList = true)
    {
        if (clearList) list.Clear();
        //return null;//one secd
        var count = LogicAndMath.MinMaxAvgConc(Random.value, minCount, maxCount, avgCount, countConc);

        for (int i = 0; i < count; i++)
        {
            var group = LogicAndMath.GetRandomItemFromListValues(Random.value, tierGroups, x => x.chance);
            //this is susceptible to finding the same tier group twice, wasting processing. could be optimized...

            if (group != null && group.items.Count > 0)
            {
                var itemIndex = Random.Range(0, group.items.Count);
                var item = group.items[itemIndex];

                list.Add(item);
            }
        }

        return list;
    }

    readonly List<Item> reusableItemList = new();

    public void SpawnItems(Vector2 spawnPos)
    {
        GenerateItemList(reusableItemList, minCount, maxCount, avgCount, countConc);

        foreach (var item in reusableItemList)
        {
            var itemPos = Random.insideUnitCircle * itemDropRadius + spawnPos;
            Instantiate(item, itemPos, item.transform.rotation, transform);
        }
    }

    [System.Serializable]
    public class TierGroup
    {
        public List<Item> items;
        public int tier;
        public float chance = 1;

        public TierGroup (List<Item> items, int tier)
        {
            this.items = items;
            this.tier = tier;
        }
    }
}
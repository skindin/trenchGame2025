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

    public Transform container;

    public float itemDropRadius = 5, minCount = 1, maxCount = 10, avgCount = 4, countConc = .5f, dropInterval = 60, dropTimer = 0;
    public bool dropOnStart = false;

    //WaitForSeconds wait;

    private void Awake()
    {
        Item.defaultContainer = container;
    }

    public void RunDropInterval (float seconds)
    {
        dropTimer += seconds;

        if (dropTimer >= dropInterval)
        {
            var spawnPoint = ChunkManager.Manager.GetRandomPosMargin(itemDropRadius);
            SpawnDrop(spawnPoint);
            dropTimer = 0;
        }
    }

    private void Start()
    {
        Sort();

        if (dropOnStart)
        {
            RunDropInterval(dropInterval);
        }
    }

    private void Update()
    {
        RunDropInterval(Time.deltaTime); //i think this line caused a stack overflow but i have no idea why
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

    public void SpawnDrop(Vector2 spawnPos)
    {
        GenerateItemList(reusableItemList, minCount, maxCount, avgCount, countConc);

        foreach (var item in reusableItemList)
        {
            var itemPos = Random.insideUnitCircle * itemDropRadius + spawnPos;
            NewItem(item,itemPos);
        }
    }

    public Item NewItem (Item prfab, Vector3 pos)
    {        
        var item = Instantiate(prfab, pos, Quaternion.identity, container).GetComponent<Item>();
        item.Drop();
        return item;
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
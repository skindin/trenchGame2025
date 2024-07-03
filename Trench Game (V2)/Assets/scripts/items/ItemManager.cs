using System.Collections.Generic;
using UnityEngine;
//using System.Linq;
//using JetBrains.Annotations;
using System;

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
            }
            return manager;
        }
    }

    public bool spawnDrops = true;
    public List<ItemPool> itemPools = new();
    public Transform container;
    public float grabDur = .1f;
    public float deleteDur = .1f;

    private void Awake()
    {
        foreach (var pool in itemPools)
        {
            pool.Setup(pool.prefab);
        }

        //dropRoutine = StartCoroutine(ItemDrop());
    }

    //public void RunDropInterval(float seconds)
    //{
    //    dropTimer += seconds;

    //    if (dropTimer >= dropInterval)
    //    {
    //        var spawnPoint = ChunkManager.Manager.GetRandomPos(itemDropRadius);
    //        SpawnDrop(spawnPoint);
    //        dropTimer = 0;
    //    }
    //}

    //System.Collections.IEnumerator ItemDrop()
    //{
    //    while (true)
    //    {
    //        if (spawnDrops)
    //        {
    //            var spawnPoint = ChunkManager.Manager.GetRandomPos(itemDropRadius);
    //            SpawnDrop(spawnPoint);
    //        }

    //        dropTimer = 0;

    //        while (dropTimer < dropInterval)
    //        {
    //            yield return null;
    //            dropTimer += Time.deltaTime;
    //        }
    //    }
    //}

    //private void Start()
    //{
    //    if (dropOnStart && spawnDrops)
    //    {
    //        RunDropInterval(dropInterval);
    //    }
    //}

    //private void Update()
    //{
    //    if (spawnDrops)
    //        RunDropInterval(Time.deltaTime);
    //}

    public Item NewItem(Item prefab)
    {
        var itemPool = itemPools.Find(x => x.prefab == prefab);

        if (itemPool != null)
        {
            var item = itemPool.NewItem();
            return item;
        }

        throw new Exception($"Item Manager does not have a pool setup for {prefab}");
    }

    public void RemoveItem(Item item, Item prefab)
    {
        var itemPool = itemPools.Find(pool => pool.prefab == prefab);

        if (itemPool != null)
        {
            itemPool.MoveTo(item);
        }
        else
            throw new Exception($"Item Manager does not have a pool setup for {prefab}");
    }

    //public List<Item> GenerateItemList(List<Item> list, int count, bool clearList = true) //repeating the same item throughout groups causes lots of problems lol
    //{
    //    if (clearList) list.Clear();

    //    var itemsGroupPairs = LogicAndMath.GetOccurancePairs(itemsGroups, count, x =>
    //    {
    //        return x.chance;
    //    });

    //    foreach (var itemsGroupPair in itemsGroupPairs)
    //    {
    //        var groups = itemsGroupPair.Item1.itemGroups;
    //        var groupsCount = itemsGroupPair.Item2;

    //        if (groupsCount == 0)
    //            continue;

    //        var itemGroupPairs = LogicAndMath.GetOccurancePairs(
    //            groups,
    //            groupsCount,
    //            x => {
    //                return x.chance; },
    //            x => {
    //                return x.MaxNew; },
    //            true,
    //            replaceCappedItems
    //        );

    //        foreach (var itemGroupPair in itemGroupPairs)
    //        {
    //            for (int i = 0; i < itemGroupPair.Item2; i++)
    //            {
    //                list.Add(itemGroupPair.Item1.prefab);
    //            }
    //        }
    //    }

    //    return list;
    //}

    //readonly List<Item> reusableItemList = new();

    //public void SpawnDrop(Vector2 spawnPos)
    //{
    //    GenerateItemList(reusableItemList, dropCount);

    //    foreach (var item in reusableItemList)
    //    {
    //        var itemPos = Random.insideUnitCircle * itemDropRadius + spawnPos;
    //        var newItem = NewItem(item);
    //        //Debug.Log($"{newItem} chunk was {(newItem.Chunk != null ? $"chunk {newItem.Chunk.adress}" : "null")} before it was dropped");
    //        newItem.Drop(itemPos, out _);
    //        //Debug.Log($"{newItem} chunk was {(newItem.Chunk != null ? $"chunk {newItem.Chunk.adress}" : "null")} after it was dropped");
    //    }
    //}

    [System.Serializable]
    public class ItemsGroup
    {
        public List<ItemPool> itemGroups;
        //public float chance = 1;

        //public float Chance
        //{
        //    get
        //    {
        //        if (itemGroups.Find(x => x.Chance > 0) == null) return 0;
        //        else return chance;
        //    }
        //}
    }

    [System.Serializable]
    public class ItemPool
    {
        public Item prefab;

        public ObjectPool<Item> pool;
        public Transform container;

        int nextId = 0;

        public void MoveTo(Item item)
        {
            if (item == null) return;

            item.Chunk = null;

            //if (!active.Contains(item)) return;

            ////Debug.Log($"Item removed: {item.name}");
            //active.Remove(item);
            pool.AddToPool(item);

        }

        public void Setup(Item prefab)
        {
            container = new GameObject((prefab&& prefab.itemModel)?prefab.itemModel.name:"").transform;
            container.parent = Manager.container;

            pool = new ObjectPool<Item>(
                minPooled: pool?.minPooled ?? 5,
                maxPooled: pool?.maxPooled ?? 100,
                newFunc: () => {
                    var item = Instantiate(prefab, container).GetComponent<Item>();
                    item.gameObject.name += $"{nextId}";
                    nextId++;
                    return item;
                },
                disableAction: item =>
                {
                    //var chunk = item.Chunk;
                    item.gameObject.SetActive(false); 
                    //Debug.Log($"Item {item} {item.gameObject.GetInstanceID()} was disabled");
                    item.transform.parent = container;
                },
                resetAction: item =>
                {
                    item.gameObject.SetActive(true);
                    item.ResetItem();
                    //Debug.Log($"Item {item} {item.gameObject.GetInstanceID()} was reset");
                    //item.Chunk = null;
                },
                destroyAction: item => Destroy(item.gameObject)
            );
        }

        public Item NewItem()
        {
            var item = pool.GetFromPool();
            //active.Add(item);
            item.defaultContainer = container;
            //item.Drop(pos);
            return item;
        }
    }
}

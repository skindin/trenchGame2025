using System.Collections.Generic;
using UnityEngine;
//using System.Linq;
//using JetBrains.Annotations;
using System;
//using static UnityEditor.Progress;

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

    public Dictionary<int, Item> active = new();

    private void Awake()
    {
        var index = 0;

        foreach (var pool in itemPools)
        {
            pool.Setup(pool.prefab);
            pool.index = index;
            index++;
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

    public void RemoveAll ()
    {
        foreach (var pair in active)
        {
            var item = pair.Value;

            RemoveItem(item,false);
        }

        active.Clear();
    }

    public Item NewItem(Item prefab, int id)
    {
        var itemPool = itemPools.Find(x => x.prefab == prefab);

        if (itemPool != null)
        {
            var item = itemPool.NewItem();
            item.id = id;
            active[id]  = item;
            return item;
        }

        throw new Exception($"item cachedManager does not have pool setup for {prefab}");
    }

    public Item NewItem (int prefabId, int itemId)
    {
        if (itemPools.Count > prefabId)
        {
            var newItem = itemPools[prefabId].NewItem();
            newItem.id = itemId;
            active[itemId] = newItem;
            //newItem.id = itemId;
            return newItem;
        }

        throw new Exception($"item manager does not have pool at index {prefabId}");
    }

    public void RemoveItem(Item item, Item prefab)
    {
        var itemPool = itemPools.Find(pool => pool.prefab == prefab);

        if (itemPool != null)
        {
            if (item.wielder)
            {
                item.wielder.inventory.DropItem(item);
            }
            itemPool.MoveTo(item);
            active.Remove(item.id);
        }
        else
            throw new Exception($"Item manager does not have server pool setup for {prefab}");
    }

    public void RemoveItem(Item item, bool clearActive = true)
    {
        if (item.prefabId < itemPools.Count)
        {
            if (item.wielder)
            {
                item.wielder.inventory.DropItem(item);
            }
            itemPools[item.prefabId].MoveTo(item);

            if (clearActive)
                active.Remove(item.id);
        }
        else
        {
            throw new Exception($"item manager does not have pool at index {item.prefabId}");
        }
    }

    public void RemoveItem (int id)
    {
        if (active.TryGetValue(id, out var item))
        {
            RemoveItem(item);
        }

        throw new Exception($"item manager does not have an item with id {item.id}");
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

        public int index;

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
            container = new GameObject((prefab && prefab.itemModel) ? prefab.itemModel.name : "").transform;
            container.parent = Manager.container;

            pool = new ObjectPool<Item>(
                minPooled: pool?.minPooled ?? 5,
                maxPooled: pool?.maxPooled ?? 100,
                newFunc: () => {
                    var item = Instantiate(prefab, container).GetComponent<Item>();
                    return item;
                },
                disableAction: item =>
                {
                    //var chunk = item.Chunk;
                    item.gameObject.SetActive(false);
                    //Debug.Log($"Item {item} {item.gameObject.GetInstanceID()} was disabled");
                    item.transform.SetParent(container);
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
            item.prefabId = index;
            //item.Drop(pos);
            return item;
        }
    }
}

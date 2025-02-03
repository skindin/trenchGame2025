using System.Collections.Generic;
using UnityEngine;
//using System.Linq;
//using JetBrains.Annotations;
using System;
using UnityEngine.Events;
//using System.Runtime.InteropServices.WindowsRuntime;
using Chunks;
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

    //public bool spawnDrops = true;
    public List<ItemPool> itemPools = new();
    public readonly MonoBehaviorChunkArray<Item> chunkArray = new();
    public Transform container;

    public UnityEvent<Item> onNewItem, onRemoveItem;
    //public float grabDur = .1f;
    //public float deleteDur = .1f;

    public Dictionary<int, Item> active = new();

    int nextItemId = 0;
    int NewItemId
    {
        get
        {
            if (NetworkManager.IsServer)
            {
                nextItemId++;
                //Debug.Log($"requested id {nextId}");
                return nextItemId;
            }

            throw new Exception("client attempted to get new item id");
        }
    }

    private void Awake()
    {
        var index = 0;

        foreach (var pool in itemPools)
        {
            pool.Setup(pool.prefab);
            pool.prefab.prefabId = index;
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
            return NewItem(itemPool, id);
        }

        throw new Exception($"item cachedManager does not have pool setup for {prefab}");
    }

    public Item NewItem (int prefabId, int itemId)
    {
        if (itemPools.Count > prefabId)
        {
            return NewItem(itemPools[prefabId], itemId);
        }

        throw new Exception($"item manager does not have an item pool at index {prefabId}");
    }

    Item NewItem (ItemPool pool, int itemId)
    {
        var newItem = pool.NewItem();
        newItem.id = itemId;
        active.Add(itemId, newItem);
        //newItem.id = itemId;
        onNewItem.Invoke(newItem);

        //idk where network logic should go, because this doesn't position the item, and that should always be included in the data

        return newItem;
    }

    public Item NewItemNewId(Item prefab)
    {
        return NewItem(prefab, NewItemId);
    }

    public Item NewItemNewId (int prefabId)
    {
        return NewItem(prefabId, NewItemId);
    }

    Item DropNewItem (Item prefab, Vector2 pos, int id, bool sync)
    {
        var newItem = NewItem(prefab, id);
        newItem.Drop(pos);

        if (sync && NetworkManager.IsServer)
        {
            NetworkManager.Manager.server.AddItem(newItem);
        }

        return newItem;
    }

    public Ammo DropAmmo (AmmoType type, Vector2 pos, int amount) //thinking the dropped ammo won't spawn on client until the server says to
    {
        if (!NetworkManager.IsServer)
            throw new Exception("client attempted to call drop ammo");

        foreach (var pool in itemPools)
        {
            if (pool.prefab is Ammo ammo && ammo.type == type)
            {
                var newAmmo = NewItem(pool, NewItemId) as Ammo;
                newAmmo.amount = amount;

                newAmmo.Drop(pos);

                

                return newAmmo;
            }
        }

        throw new Exception($"wuh oh there's no ammo of type {type.name} in the prefab list");
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
            onRemoveItem?.Invoke(item);

            chunkArray.RemoveObject(item);
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

            onRemoveItem?.Invoke(item);

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

        //public int index;

        public void MoveTo(Item item) //idk why this function exists lol
        {
            if (item == null) return;

            //item.Chunk = null;

            //Manager.chunkArray.RemoveObject(item);

            //if (!active.Contains(item)) return;

            ////Debug.Log($"Item removed: {item.name}");
            //active.Remove(item);
            pool.AddToPool(item);

        }

        public void Setup(Item prefab)
        {
            container = new GameObject(prefab ? prefab.itemName : "").transform;
            container.parent = Manager.container;

            //prefab.prefabId = index;

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
                    Manager.chunkArray.RemoveObject(item);
                },
                destroyAction: item => Destroy(item.gameObject)
            );
        }

        public Item NewItem()
        {
            var item = pool.GetFromPool();
            //active.Add(item);
            item.defaultContainer = container;
            //item.prefabId = index;
            //item.Drop(pos);
            return item;
        }
    }
}

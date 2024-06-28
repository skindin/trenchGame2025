using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using JetBrains.Annotations;

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
    public List<ItemsGroup> itemsGroups = new();
    public Transform container;

    public float itemDropRadius = 5, dropInterval = 60, dropTimer = 0;
    public int dropCount = 10;
    public bool dropOnStart = false, replaceCappedItems = false;
    public Coroutine dropRoutine;

    public float TimeToNextDrop => dropInterval - dropTimer;

    private void Awake()
    {
        foreach (var a in itemsGroups)
        {
            foreach (var b in a.itemGroups)
            {
                b.Setup(b.prefab);
            }
        }

        dropRoutine = StartCoroutine(ItemDrop());
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

    System.Collections.IEnumerator ItemDrop()
    {
        while (true)
        {
            if (spawnDrops)
            {
                var spawnPoint = ChunkManager.Manager.GetRandomPos(itemDropRadius);
                SpawnDrop(spawnPoint);
            }

            dropTimer = 0;

            while (dropTimer < dropInterval)
            {
                yield return null;
                dropTimer += Time.deltaTime;
            }
        }
    }

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

    Item NewItem(Item prefab)
    {
        ItemGroup group = null;

        foreach (var g in itemsGroups)
        {
            var itemGroup = g.itemGroups.Find(x => x.prefab == prefab);
            if (itemGroup != null)
            {
                group = itemGroup;
                break;
            }
        }

        if (group != null)
        {
            var item = group.NewItem();
            return item;
        }

        return null;
    }

    public void RemoveItem(Item item)
    {
        foreach (var a in itemsGroups)
        {
            foreach (var b in a.itemGroups)
            {
                if (b.active.Contains(item))
                {
                    //Debug.Log($"Removing item: {item.name}");
                    //Debug.Log($"{item} chunk was {(item.Chunk != null ? $"chunk {item.Chunk.adress}" : "null")} before it was removed");
                    b.Remove(item);
                    //Debug.Log($"{item} chunk was {(item.Chunk != null ? $"chunk {item.Chunk.adress}" : "null")} after it was removed");
                    //Debug.Log($"... and {(item.gameObject.activeSelf ? "was " : "wasn't")} active...");
                    return;
                }
            }
        }
    }

    public void DropAmo(AmoType type, int amount, Vector2 pos)
    {
        foreach (var itemsGroup in itemsGroups)
        {
            foreach (var itemGroup in itemsGroup.itemGroups)
            {
                if (itemGroup.prefab is not Amo amo || amo.AmoModel.type != type)
                    continue;

                var newAmo = NewItem(itemGroup.prefab) as Amo;
                newAmo.amount = amount;
                newAmo.Drop(pos);
            }
        }
    }

    public List<Item> GenerateItemList(List<Item> list, int count, bool clearList = true) //repeating the same item throughout groups causes lots of problems lol
    {
        if (clearList) list.Clear();

        var itemsGroupPairs = LogicAndMath.GetOccurancePairs(itemsGroups, count, x =>
        {
            return x.chance;
        });

        foreach (var itemsGroupPair in itemsGroupPairs)
        {
            var groups = itemsGroupPair.Item1.itemGroups;
            var groupsCount = itemsGroupPair.Item2;

            if (groupsCount == 0)
                continue;

            var itemGroupPairs = LogicAndMath.GetOccurancePairs(
                groups,
                groupsCount,
                x => {
                    return x.chance; },
                x => {
                    return x.MaxNew; },
                true,
                replaceCappedItems
            );

            foreach (var itemGroupPair in itemGroupPairs)
            {
                for (int i = 0; i < itemGroupPair.Item2; i++)
                {
                    list.Add(itemGroupPair.Item1.prefab);
                }
            }
        }

        return list;
    }

    readonly List<Item> reusableItemList = new();

    public void SpawnDrop(Vector2 spawnPos)
    {
        GenerateItemList(reusableItemList, dropCount);

        foreach (var item in reusableItemList)
        {
            var itemPos = Random.insideUnitCircle * itemDropRadius + spawnPos;
            var newItem = NewItem(item);
            //Debug.Log($"{newItem} chunk was {(newItem.Chunk != null ? $"chunk {newItem.Chunk.adress}" : "null")} before it was dropped");
            newItem.Drop(itemPos);
            //Debug.Log($"{newItem} chunk was {(newItem.Chunk != null ? $"chunk {newItem.Chunk.adress}" : "null")} after it was dropped");
        }
    }

    [System.Serializable]
    public class ItemsGroup
    {
        public List<ItemGroup> itemGroups;
        public float chance = 1;

        public float Chance
        {
            get
            {
                if (itemGroups.Find(x => x.Chance > 0) == null) return 0;
                else return chance;
            }
        }
    }

    [System.Serializable]
    public class ItemGroup
    {
        public Item prefab;
        public float chance = 1;

        public float Chance => active.Count >= spawnCap ? 0 : chance;
        public int MaxNew => spawnCap - active.Count;

        public List<Item> active = new();
        public ObjectPool<Item> pool;
        public Transform container;
        public int spawnCap;
        public bool exceedsCap = false;

        public void Remove(Item item)
        {
            if (item == null) return;

            item.Chunk = null;

            if (!active.Contains(item)) return;

            //Debug.Log($"Item removed: {item.name}");
            active.Remove(item);
            pool.AddToPool(item);

        }

        public void Setup(Item prefab)
        {
            container = new GameObject((prefab&& prefab.itemModel)?prefab.itemModel.name:"").transform;
            container.parent = Manager.container;

            pool = new ObjectPool<Item>(
                minPooled: pool?.minPooled ?? 5,
                maxPooled: pool?.maxPooled ?? 100,
                newFunc: () => Instantiate(prefab, container).GetComponent<Item>(),
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
            active.Add(item);
            item.defaultContainer = container;
            //item.Drop(pos);
            return item;
        }
    }
}

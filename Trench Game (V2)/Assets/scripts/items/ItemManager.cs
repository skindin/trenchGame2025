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

    public List<ItemsGroup> itemsGroups = new();
    //spawn groups should probably be separate from object pools. pools have much more to do with local performance,
    //but clients don't need to know anything about spawn caps etc, because only the server will use them

    public Transform container;

    public float itemDropRadius = 5, dropInterval = 60, dropTimer = 0;
    public int dropCount = 10;
    public bool dropOnStart = false, replaceCappedItems = false;

    //WaitForSeconds wait;

    private void Awake()
    {
        foreach (var a in itemsGroups)
        {
            foreach (var b in a.itemGroups)
            {
                b.Setup(b.prefab);
            }
        }
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

    Item NewItem(Item prefab, Vector3 pos)
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
            var item = group.NewItem(pos);
            item.Drop();
            return item;
        }

        return null;
    }

    public void RemoveItem (Item item)
    {
        foreach (var a in itemsGroups)
        {
            foreach (var b in a.itemGroups)

            if (b.active.Contains(item))
            {
                b.Remove(item);
                return;
            }
        }
    }

    public void DropAmo (AmoType type, int amount, Vector2 pos)
    {
        foreach (var itemsGroup in itemsGroups)
        {
            foreach (var itemGroup in itemsGroup.itemGroups)
            {
                if (itemGroup.prefab is not Amo amo || amo.AmoModel.type != type)
                    continue;

                var newAmo = NewItem(itemGroup.prefab, pos) as Amo;
                newAmo.amount = amount;
            }
        }
    }

    public void Sort ()
    {
        //foreach (var item in itemPrefabs)
        //{
        //    var group = tierGroups.Find(x => x.tier == item.model.tier);

        //    if (group == null)
        //    {
        //        group = new(new() {item}, item.model.tier);
        //        tierGroups.Add(group);
        //    }
        //    else
        //    {
        //        group.items.Add(item);
        //    }
        //}
    }


    /// <summary>
    /// Generates a list of refferences to prefabs. These prefabs must still be instantiated.
    /// </summary>
    /// <returns></returns>
    public List<Item> GenerateItemList(List<Item> list, int count, bool clearList = true)
    {
        if (clearList) list.Clear();
        //return null;//one secd

        var itemsGroupPairs = LogicAndMath.GetOccurancePairs(itemsGroups, count, x => x.Chance);

        foreach (var itemsGroupPair in itemsGroupPairs)
        {
            var groups = itemsGroupPair.Item1.itemGroups;
            var groupsCount = itemsGroupPair.Item2;
            var itemGroupPairs = LogicAndMath.GetOccurancePairs(
                groups, 
                groupsCount, 
                x => x.Chance, 
                x => x.MaxNew, 
                true, 
                replaceCappedItems);

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
        //var count = LogicAndMath.MinMaxAvgConcToInt(Random.value, minCount, maxCount, avgCount, countConc);

        GenerateItemList(reusableItemList, dropCount);

        foreach (var item in reusableItemList)
        {
            var itemPos = Random.insideUnitCircle * itemDropRadius + spawnPos;
            NewItem(item,itemPos);
        }
    }

    //public Item NewItem (Item prfab, Vector3 pos)
    //{        
    //    var item = Instantiate(prfab, pos, Quaternion.identity, container).GetComponent<Item>();
    //    item.Drop();
    //    return item;
    //}

    [System.Serializable]
    public class ItemsGroup
    {
        public List<ItemGroup> itemGroups;
        public float chance = 1;
        public float Chance
        {
            get
            {
                if (itemGroups.Find(x => x.Chance > 0) == null) //if none of them have a chance, neither does this
                    return 0;
                else return chance;
            }
        }

        //public TierGroup (List<Item> items, int tier)
        //{
        //    this.items = items;
        //    this.tier = tier;
        //}
    }

    //[System.Serializable]
    //public class ItemChance
    //{
    //    public Item item;
    //    public float chance;
    //}

    [System.Serializable]
    public class ItemGroup
    {
        public Item prefab;
        public float chance = 1;

        public float Chance
        {
            get
            {
                if (active.Count >= spawnCap) return 0;
                return chance;
            }
        }

        public int MaxNew
        {
            get
            {
                return spawnCap - active.Count;
            }
        }

        public List<Item> active = new();
        public ObjectPool<Item> pool;
        public Transform container;

        public int spawnCap;

        public bool exceedsCap = false;

        public void Remove (Item item)
        {
            if (!active.Contains(item))
                return;

            active.Remove(item);
            pool.AddToPool(item);
        }

        public void Setup (Item prefab)
        {
            container = new GameObject(prefab.model.name).transform;
            container.parent = Manager.container;

            pool.newFunc = () => Instantiate(prefab, container).GetComponent<Item>();

            pool.disableAction = item =>
            {
                item.gameObject.SetActive(false);
                item.transform.parent = container;
            };

            pool.resetAction = item =>
            {
                item.ResetItem();
                item.gameObject.SetActive(true);
                //item.transform.parent = container;
            };

            pool.removeAction = item => Destroy(item, 0.0001f);
        }

        public Item NewItem (Vector3 pos)
        {
            var item = pool.GetFromPool();
            active.Add(item);
            item.defaultContainer = container;
            item.transform.position = pos;
            return item;
        }
    }
}
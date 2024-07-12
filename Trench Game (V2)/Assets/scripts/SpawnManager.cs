using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    static SpawnManager manager;
    public static SpawnManager Manager
    {
        get
        {
            if (manager == null)
            {
                manager = FindObjectOfType<SpawnManager>();
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

    public List<SpawnItemGroup> itemGroups = new();
    public int itemsPerDrop = 5;
    public float itemDropRadius = 5, itemDropInterval = 20, itemDropTimer = 0, minSpawnDelay, maxSpawnDelay, spawnScaleDuration;
    public bool spawnItemDrops = true;
    Coroutine itemDropRoutine;
    public float TimeToNextDrop
    {
        get
        {
            return itemDropInterval - itemDropTimer;
        }
    }

    public int GetCharacterTotal
    {
        get
        {
            return spawnCharacter.currentAmount;
        }
    }

    public int GetItemTotal
    {
        get
        {
            return (int)LogicAndMath.GetListValueTotal(itemGroups.ToArray(), group => LogicAndMath.GetListValueTotal(group.spawnItems.ToArray(), spawnItem => spawnItem.currentAmount));
        }
    }

    private void Start() //this code relies on other things being set up, so it's best to put in start
    {
        if (spawnItemDrops)
            StartItemDropRoutine();

        if (spawnBots)
            FillCharacterCapWithBots();
    }

    public abstract class SpawnObject<T>
    {
        public string name;
        public T prefab;

        readonly List<T> active = new();

        //public bool capSpawning = true;
        public int currentAmount, spawnCap = 10;

        public virtual int CapDiff
        {
            get
            {
                return spawnCap - active.Count;
            }
        }

        //public abstract int Amount { get; set; }

        public T Get(Vector2 pos)
        {
            var newObj = SpawnLogic(pos);
            active.Add(newObj);
            currentAmount = active.Count;
            return newObj;
        }

        public abstract T SpawnLogic(Vector2 pos);

        //ClientRequestSpawn for when the client needs to spawn something such as it's own player character or amo
        //ServerSpawn for the server to determine if the client can spawn the object
        //ClientsSpawn for spawning the object for all clients that do not yet know about it
        //ClientSpawnDenied for when the server needs to undo unpermitted spawning

        public void Remove(T obj)
        {
            RemoveLogic(obj);
            active.Remove(obj);
            currentAmount = active.Count;
        }

        public abstract void RemoveLogic(T obj);

        public bool Contains(T obj)
        {
            return active.Contains(obj);
        }
    }

    public void StartItemDropRoutine()
    {
        itemDropRoutine ??= StartCoroutine(DropRoutine());

        IEnumerator DropRoutine()
        {
            while (true)
            {
                SpawnItemDrop();

                while (itemDropTimer < itemDropInterval)
                {
                    yield return null;
                    itemDropTimer += Time.deltaTime;
                }

                itemDropTimer = 0;
            }
        }
    }

    public List<(SpawnItem, int)> GenerateItemPairs()
    {
        var groupPairs = LogicAndMath.GetOccurancePairs(itemGroups, itemsPerDrop, group => group.chance);

        var allItemPairs = new List<(SpawnItem, int)>();

        foreach (var groupPair in groupPairs)
        {
            var itemPairs = LogicAndMath.GetOccurancePairs(
                groupPair.Item1.spawnItems,
                groupPair.Item2,
                spawnItem => spawnItem.chance,
                spawnItem => spawnItem.CapDiff
                );

            foreach (var itemPair in itemPairs)
            {
                allItemPairs.Add(itemPair);
            }
        }

        return allItemPairs;
    }
    public void SpawnItemDrop()
    {
        var dropPos = ChunkManager.Manager.GetRandomPos(itemDropRadius);
        var itemPairs = GenerateItemPairs();

        foreach (var pair in itemPairs)
        {
            for (var i = 0; i < pair.Item2; i++)
            {
                var itemPos = Random.insideUnitCircle * itemDropRadius + dropPos;
                //itemPos = Vector2.zero;
                var newItem = pair.Item1.Get(itemPos);
                //var spawnDelay = Random.Range(minSpawnDelay, maxSpawnDelay);
                newItem.Drop(itemPos);
                //newItem.Spawn(Vector2.zero, spawnDelay, spawnScaleDuration);
            }
        }
    }

    public Ammo GetAmo(AmmoType type, int amount, Vector2 pos = default)
    {
        foreach (var group in itemGroups)
        {
            foreach (var spawnItem in group.spawnItems)
            {
                if (spawnItem.prefab is Ammo amoPrefab)
                {
                    if (amoPrefab.AmoModel.type == type)
                    {
                        var newAmo = spawnItem.Get(pos) as Ammo;
                        newAmo.amount = amount;
                        return newAmo;
                    }
                }
            }
        }

        return null;
    }

    public Ammo DropAmo(AmmoType type, int amount, Vector2 pos)
    {
        //startPos = endPos = Vector2.zero;

        var newAmo = GetAmo(type, amount, pos);

        if (newAmo != null)
        {
            newAmo.Drop(pos);
            return newAmo;
        }

        return null;
    }

    public void RemoveItem(Item item)
    {
        foreach (var group in itemGroups)
        {
            foreach (var spawnItem in group.spawnItems)
            {
                if (spawnItem.Contains(item))
                {
                    spawnItem.Remove(item);
                    return;
                }
            }
        }

        throw new System.Exception($"No spawn groups contained {item}");
    }

    [System.Serializable]
    public class SpawnItem : SpawnObject<Item>
    {
        public float chance = 1f;

        public override Item SpawnLogic(Vector2 pos)
        {
            var item = ItemManager.Manager.NewItem(prefab);
            item.transform.position = pos;
            return item;
        }

        public override void RemoveLogic(Item item)
        {
            ItemManager.Manager.RemoveItem(item, prefab);
        }
    }

    [System.Serializable]
    public class SpawnItemGroup
    {
        public string Name;
        public List<SpawnItem> spawnItems = new();
        public float chance = 1f;
    }

    [System.Serializable]
    public class SpawnCharacter : SpawnObject<Character>
    {
        Character.CharacterType characterType;

        public Character SpawnWithType(Vector2 pos, Character.CharacterType type)
        {
            characterType = type;
            return Get(pos);
        }

        public override Character SpawnLogic(Vector2 pos)
        {
            return CharacterManager.Manager.NewCharacter(pos, characterType);
        }

        public override void RemoveLogic(Character character)
        {
            CharacterManager.Manager.RemoveCharacter(character);
        }
    }

    public SpawnCharacter spawnCharacter;
    public bool spawnBots = true;

    public void FillCharacterCapWithBots()
    {
        var botAmount = spawnCharacter.CapDiff;

        for (int i = 0; i < botAmount; i++)
        {
            var botPos = ChunkManager.Manager.GetRandomPos();

            var newBot = spawnCharacter.SpawnWithType(botPos, Character.CharacterType.localBot);

            newBot.name += i;
            newBot.Name = $"bot{i}";
        }
    }

    //public List<Gun> gunPrefabs = new();
    //public List<Amo> amoPrefabs = new();
}
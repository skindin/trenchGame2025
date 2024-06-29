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
    public float itemDropRadius = 5, itemDropInterval = 20, itemDropTimer = 0;
    Coroutine itemDropRoutine;

    private void Awake()
    {
        StartItemDropRoutine();
        FillCharacterCapWithBots();
    }

    public void StartItemDropRoutine()
    {
        itemDropRoutine ??= StartCoroutine(DropRoutine());

        IEnumerator DropRoutine ()
        {
            while (true)
            {
                SpawnItemDrop();

                while (itemDropTimer < itemDropInterval)
                {
                    yield return null;
                    itemDropTimer += Time.deltaTime;
                }
            }
        }
    }


    public SpawnCharacter characterSpawn;

    public void FillCharacterCapWithBots ()
    {
        var botAmount = characterSpawn.spawnCap - characterSpawn.active.Count;

        for (int i = 0; i < botAmount; i++)
        {
            var botPos = ChunkManager.Manager.GetRandomPos();

            characterSpawn.SpawnWithType(botPos, Character.CharacterType.localBot);
        }
    }

    //public List<Gun> gunPrefabs = new();
    //public List<Amo> amoPrefabs = new();

    public List<(SpawnItem,int)> GenerateItemPairs ()
    {
        var groupPairs = LogicAndMath.GetOccurancePairs(itemGroups, itemsPerDrop, group => group.chance);

        var allItemPairs = new List<(SpawnItem, int)>();

        foreach (var groupPair in groupPairs)
        {
            var itemPairs = LogicAndMath.GetOccurancePairs(groupPair.Item1.spawnItems, groupPair.Item2, spawnItem => spawnItem.chance);

            foreach (var itemPair in itemPairs)
            {
                allItemPairs.Add(itemPair);
            }
        }

        return allItemPairs;
    }
    public void SpawnItemDrop ()
    {
        var dropPos = ChunkManager.Manager.GetRandomPos(itemDropRadius);
        var itemPairs = GenerateItemPairs();

        foreach (var pair in itemPairs)
        {
            for (var i = 0; i < pair.Item2; i++)
            {
                var itemPos = Random.insideUnitCircle * itemDropRadius + dropPos;
                pair.Item1.Spawn(itemPos);
            }
        }
    }

    public abstract class SpawnObject<T>
    {
        public T prefab;

        public List<T> active = new();

        public bool capSpawning = true;
        public int spawnCap = 10;

        //public abstract int Amount { get; set; }

        public abstract T Spawn(Vector2 pos);

        //ClientRequestSpawn for when the client needs to spawn something such as it's own player character or amo
        //ServerSpawn for the server to determine if the client can spawn the object
        //ClientsSpawn for spawning the object for all clients that do not yet know about it
        //ClientSpawnDenied for when the server needs to undo unpermitted spawning

        public abstract void Remove(T obj);
    }

    [System.Serializable]
    public class SpawnItem : SpawnObject<Item>
    {
        public string Name;
        public float chance = 1f;

        public override Item Spawn(Vector2 pos)
        {
            return ItemManager.Manager.NewItem(prefab);
        }

        public override void Remove(Item item)
        {
            ItemManager.Manager.RemoveItem(item);
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
        public Character SpawnWithType (Vector2 pos, Character.CharacterType type)
        {
            return CharacterManager.Manager.NewCharacter(pos, type);
        }

        public override Character Spawn(Vector2 pos)
        {
            return SpawnWithType(pos, Character.CharacterType.none);
        }

        public override void Remove(Character character)
        {
            CharacterManager.Manager.RemoveCharacter(character);
        }
    }
}
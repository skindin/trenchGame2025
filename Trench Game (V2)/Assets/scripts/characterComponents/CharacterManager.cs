using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    public bool spawnSquads = true, spawnSquadOnStart = true;
    public ObjectPool<Character> pool;
    public List<Character> active = new();
    public Character prefab;
    public Transform container;
    public float squadRadius = 5, squadSpawnInterval = 20, respawnWait = .5f;
    public int botsPerSquad = 5, spawnCap = 10;
    float lastSquadStamp = 0;

    public float TimeToSquadSpawn
    {
        get
        {
            return squadSpawnInterval - (Time.time - lastSquadStamp);
        }
    }

    static CharacterManager manager;
    public static CharacterManager Manager
    {
        get
        {
            if (manager == null)
            {
                manager = FindObjectOfType<CharacterManager>();
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

    private void Awake()
    {
        SetupPool();
    }

    //private void Start()
    //{
    //}

    void SetupPool ()
    {
        pool.newFunc = () => Instantiate(prefab, container).GetComponent<Character>();

        pool.disableAction = character =>
        {
            character.gameObject.SetActive(false);
            //character.transform.parent = container;
        };

        pool.resetAction = character =>
        {
            character.ResetSelf();
            character.gameObject.SetActive(true);
            //item.transform.parent = container;
        };

        pool.removeAction = character => Destroy(character.gameObject, 0.0001f);
    }

    private void Update()
    {
        if (spawnSquads)
        {
            if (Time.time - lastSquadStamp > squadSpawnInterval || (Time.time == 0 && spawnSquadOnStart))
            {
                var pos = ChunkManager.Manager.GetRandomPos(squadRadius);
                SpawnBotSquad(pos);
                lastSquadStamp = Time.time;
            }
        }
    }

    public void SpawnBotSquad (Vector2 pos)
    {
        var amount = Mathf.Min(spawnCap - active.Count, botsPerSquad);

        for (int i = 0; i < amount; i++)
        {
            var botPos = Random.insideUnitCircle * squadRadius + pos;

            NewBot(botPos);
        }
    }

    public Character NewBot (Vector2 pos)
    {
        return NewCharacter(pos, Character.CharacterType.localBot);
    }

    public Character NewLocalPlayer(Vector2 pos)
    {
        return NewCharacter(pos, Character.CharacterType.localPlayer);
    }

    Character NewCharacter (Vector2 pos, Character.CharacterType type)
    {
        var newCharacter = pool.GetFromPool();

        newCharacter.transform.position = pos;
        newCharacter.UpdateChunk();

        newCharacter.Type = type;

        active.Add(newCharacter);

        return newCharacter;
    }

    public void RemoveCharacter(Character character)
    {
        active.Remove(character);

        pool.AddToPool(character);
        character.Chunk = null;
    }

    public void StartRespawn (Character character)
    {
        StartCoroutine(RespawnCharacter(character));
    }

    IEnumerator RespawnCharacter (Character character)
    {
        var type = character.Type;

        character.gameObject.SetActive(false);
        character.ResetSelf();

        yield return new WaitForSeconds(respawnWait);

        character.gameObject.SetActive(true);
        character.SetPos(ChunkManager.Manager.GetRandomPos());
        character.Type = type;

        character.UpdateChunk();
    }
}

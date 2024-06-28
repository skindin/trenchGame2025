using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    public bool spawnSquads = true;
    public ObjectPool<Character> pool;
    public List<Character> active = new();
    public Character prefab;
    public Transform container;
    public float squadRadius = 5, squadSpawnInterval = 20, respawnWait = .5f, stopWatchTime = 0;
    public int botsPerSquad = 5, spawnCap = 10;
    float squadSpawnTimer = 0;
    Coroutine squadSpawnRoutine, stopWatchRoutine;
    bool sortCharactersThisFrame = false;
    int nextBotId = 0;

    public float TimeToSquadSpawn
    {
        get
        {
            return squadSpawnInterval - squadSpawnTimer;
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
        squadSpawnRoutine = StartCoroutine(BotSpawn());

        StartStopWatch();
    }

    void StartStopWatch ()
    {
        if (stopWatchRoutine != null)
            StopCoroutine(stopWatchRoutine);
        stopWatchRoutine = StartCoroutine(StopWatch());
    }

    IEnumerator StopWatch ()
    {
        stopWatchTime = 0;

        while (true)
        {
            yield return null;
            stopWatchTime += Time.deltaTime;
        }
    }
    //private void Start()
    //{
    //}

    void SetupPool ()
    {
        pool = new ObjectPool<Character>(pool.minPooled, pool.maxPooled,
            newFunc: () => Instantiate(prefab, container).GetComponent<Character>(),
            resetAction: character =>
            {
                character.ResetSelf();
                character.gameObject.SetActive(true);
                //item.transform.parent = container;
            },
            disableAction: character =>
            {
                character.gameObject.SetActive(false);
                //character.transform.parent = container;
            },
            destroyAction: character => Destroy(character.gameObject, 0.0001f)
            );
    }

    void SortByScore ()
    {
        if (active.Count < 1)
            return;

        var prevTop = active[0];

        LogicAndMath.SortHighestToLowest(active, character => character.KillCount);
        LogicAndMath.AssignIndexes(active, (character, index) => character.rank = index + 1);

        if (active[0] != prevTop)
        {
            StartStopWatch();
        }
    }

    public void UpdateScoreBoard ()
    {
        sortCharactersThisFrame = true;
    }

    private void Update()
    { 
        if (sortCharactersThisFrame)
        {
            SortByScore();
        }
    }

    //private void Update()
    //{
    //    if (spawnSquads)
    //    {
    //        if (Time.time - lastSquadStamp > squadSpawnInterval || (Time.time == 0 && spawnSquadOnStart))
    //        {
    //            var pos = ChunkManager.Manager.GetRandomPos(squadRadius);
    //            SpawnBotSquad(pos);
    //            lastSquadStamp = Time.time;
    //        }
    //    }
    //}

    IEnumerator BotSpawn ()
    {
        while (true)
        {
            if (spawnSquads)
            {
                var pos = ChunkManager.Manager.GetRandomPos(squadRadius);
                SpawnBotSquad(pos);

                if (active.Count >= spawnCap) yield break;
            }

            squadSpawnTimer = 0;

            while (squadSpawnTimer < squadSpawnInterval)
            {
                yield return null;
                squadSpawnTimer += Time.deltaTime;
            }
        }
    }

    public void SpawnBotSquad (Vector2 pos)
    {
        var amount = Mathf.Min(spawnCap - active.Count, botsPerSquad);

        for (int i = 0; i < amount; i++)
        {
            var botPos = Random.insideUnitCircle * squadRadius + pos;

            NewBot(botPos).Name = $"bot{nextBotId}";
            nextBotId++;
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

        UpdateScoreBoard();

        return newCharacter;
    }

    public void RemoveCharacter(Character character)
    {
        active.Remove(character);

        pool.AddToPool(character);
        character.Chunk = null;

        squadSpawnRoutine = StartCoroutine(BotSpawn());

        UpdateScoreBoard();
    }

    public void KillCharacter(Character character)
    {
        if (character.controlType != Character.CharacterType.localBot || active.Count <= spawnCap)
            //if this character is not a bot, or the spawn cap is unmet...
        {
            StartRespawn(character);
        }
        else
        {
            RemoveCharacter(character);
        }

        UpdateScoreBoard();
    }

    void StartRespawn (Character character)
    {
        StartCoroutine(RespawnCharacter(character));
    }

    IEnumerator RespawnCharacter (Character character)
    {
        //active.Remove(character);

        character.gameObject.SetActive(false);
        active.Remove(character);

        character.Chunk = null;

        yield return new WaitForSeconds(respawnWait);

        var type = character.Type;
        character.ResetSelf();

        //active.Add(character);

        character.gameObject.SetActive(true);
        character.SetPos(ChunkManager.Manager.GetRandomPos());
        character.Type = type;

        character.UpdateChunk();

        active.Add(character);

        UpdateScoreBoard();
    }
}

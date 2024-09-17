using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System;

public class CharacterManager : MonoBehaviour
{
    //#if !DEDICATED_SERVER
    //    public string symbolTest = "suck a co**";
    //#endif
    public Character localPlayerCharacter;
    public bool spawnSquads = true;
    public ObjectPool<Character> pool;
    public List<Character> active = new();
    public Character prefab;
    public Transform container;
    public float scoreStopWatch = 0, highScore = 0, respawnWait = 1;
    //public int botsPerSquad = 5, spawnCap = 10;
    //float squadSpawnTimer = 0;
    Coroutine stopWatchRoutine, scoreboardRoutine;
    //bool sortCharactersThisFrame = false;

    public string playerName = "";

    //public float TimeToSquadSpawn
    //{
    //    get
    //    {
    //        return squadSpawnInterval - squadSpawnTimer;
    //    }
    //}

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
        manager = this;

        SetupPool();
        //squadSpawnRoutine = StartCoroutine(BotSpawn());

        StartStopWatch();

//#if !DEDICATED_SERVER
//        Debug.Log("This program is not a dedicated server... " + symbolTest);
//#else
//        Debug.Log("This program is a dedicated server");
//#endif
    }

    void StartStopWatch ()
    {
        if (stopWatchRoutine != null)
            StopCoroutine(stopWatchRoutine);
        stopWatchRoutine = StartCoroutine(StopWatch());

        IEnumerator StopWatch()
        {
            scoreStopWatch = 0;

            while (true)
            {
                yield return null;
                scoreStopWatch += Time.deltaTime;
            }
        }
    }
    //private void Start()
    //{
    //}

    public void SetPlayerName (string name)
    {
        playerName = name;

        if (localPlayerCharacter)
        {
            localPlayerCharacter.characterName = name;
            NetworkManager.Manager.SetName(name);
        }
    }

    public void RemoveAllCharacters ()
    {
        while (active.Count > 0)
        {
            RemoveCharacter(active[0]);
        }
    }

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


    Character prevTop;
    public void UpdateScoreBoard ()
    {
        //sortCharactersThisFrame = true;
        scoreboardRoutine ??= StartCoroutine(UpdateScoreNextFrame());

        IEnumerator UpdateScoreNextFrame ()
        {
            yield return null;

            if (active.Count < 1)
            {
                scoreboardRoutine = null;
                yield break;
            }

            LogicAndMath.SortHighestToLowest(active, character => character.KillCount);
            LogicAndMath.AssignIndexes(active, (character, index) => character.rank = index + 1);

            var currentTop = active[0];

            if (currentTop != prevTop)
            {
                if (
                    prevTop && prevTop.controlType == Character.CharacterType.localPlayer &&
                    scoreStopWatch > highScore
                    )
                {
                    highScore = scoreStopWatch;
                }

                StartStopWatch();
            }

            prevTop = currentTop;

            scoreboardRoutine = null;
        }
    }

    //private void LateUpdate()
    //{ 
    //    if (sortCharactersThisFrame)
    //    {
    //        SortByScore();
    //    }
    //}

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

    public Character NewCharacter (Vector2 pos, Character.CharacterType type, int id)
    {
        var newCharacter = pool.GetFromPool();

        newCharacter.transform.position = pos;
        newCharacter.UpdateChunk();

        newCharacter.Type = type;

        newCharacter.id = id;

        active.Add(newCharacter);

        if (type == Character.CharacterType.localPlayer)
        {
            localPlayerCharacter = newCharacter;
            newCharacter.characterName = Manager.playerName;
            CamFollow.main.AssignTarget(newCharacter.transform);
        }

        UpdateScoreBoard();

        return newCharacter;
    }

    public void RemoveCharacter(Character character)
    {
        active.Remove(character);

        pool.AddToPool(character);
        character.Chunk = null;

        //squadSpawnRoutine = StartCoroutine(BotSpawn());

        if (character == localPlayerCharacter)
        {
            localPlayerCharacter = null;
            CamFollow.main.Reset();
        }

        character.inventory.DropAllItems();

        UpdateScoreBoard();
    }

    public void KillCharacter(Character character)
    {
        if (character.controlType != Character.CharacterType.localBot || SpawnManager.Manager.spawnCharacter.CapDiff >= 0)
            //if this character is not a bot, or the spawn cap is unmet...
        {
            StartRespawn(character);
        }
        else
        {
            SpawnManager.Manager.RemoveCharacter(character); //kind of cringe that this is removing the character from spawn manager, which then removes it from this...
        }

        character.life++;
        UpdateScoreBoard();
    }

    void StartRespawn (Character character)
    {
        StartCoroutine(RespawnCharacter(character));

        IEnumerator RespawnCharacter (Character character)
        {
            //active.Remove(character);
            var type = character.Type;

            character.gameObject.SetActive(false);
            character.ResetSelf();

            active.Remove(character);

            character.Chunk = null;

            yield return new WaitForSeconds(respawnWait);

            active.Add(character);

            //active.Add(character);



            character.gameObject.SetActive(true);
            character.SetPos(ChunkManager.Manager.GetRandomPos());
            //Debug.Log($"updated pos, {NetworkManager.Manager.server.updateCharData.List.Count} character updates");

            character.SetHP(character.maxHp); //i think this is a fine place to put it shrugging emoji
            //Debug.Log($"updated hp, {NetworkManager.Manager.server.updateCharData.List.Count} character updates");
            character.Type = type;

            character.UpdateChunk();


            UpdateScoreBoard();
            //UpdateScoreBoard();
        }
    }
}

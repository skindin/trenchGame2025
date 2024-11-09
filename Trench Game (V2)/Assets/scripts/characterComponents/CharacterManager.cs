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
    public bool spawnSquads = true, challengeComplete = false;
    public ObjectPool<Character> pool;
    public List<Character> active = new();
    public Character prefab;
    public Transform container;
    public float scoreStopWatch = 0, personalRecord = 0, respawnWait = 1, challengeDuration = 24;
    public KeyValuePair<string, float> serverRecord = new();
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

        //StartChallenge();
        //squadSpawnRoutine = StartCoroutine(BotSpawn());

        //StartStopWatch();

//#if !DEDICATED_SERVER
//        Debug.Log("This program is not a dedicated server... " + symbolTest);
//#else
//        Debug.Log("This program is a dedicated server");
//#endif
    }

    public void StartChallenge ()
    {
        if (!NetworkManager.IsServer)
            return;

        StartCoroutine(RunChallenge());

        IEnumerator RunChallenge ()
        {
            float durationInSeconds;

            Debug.Log("challenge started");


            do
            {
                yield return null;

                durationInSeconds = challengeDuration * 60 * 60;
            }
            while (NetworkManager.NetTime < durationInSeconds);

            Debug.Log($"challenge ended, {serverRecord.Key} is the winner");
            challengeComplete = true;
        }
    }

    public void StartStopWatch (float startTime = 0)
    {
        if (stopWatchRoutine != null)
            StopCoroutine(stopWatchRoutine);
        stopWatchRoutine = StartCoroutine(StopWatch());

        IEnumerator StopWatch()
        {
            while (active.Count < 1 || active[0].KillCount < 1)
            {
                scoreStopWatch = 0;
                yield return null;
            }
            scoreStopWatch = startTime;

            while (true)
            {
                yield return null;

                if (active.Count < 1 || active[0].KillCount < 1)
                {
                    StartStopWatch(scoreStopWatch);
                    yield break;
                }

                scoreStopWatch += Time.deltaTime;

                if (newLeader && scoreStopWatch >= serverRecord.Value)
                {
                    UpdateScoreBoard(scoreStopWatch, true);
                    newLeader = false;
                }
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

        serverRecord = new();
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
    int prevCharCount, prevKills;
    bool newLeader = true;
    public void UpdateScoreBoard (float progress = 0, bool @override = false)
    {
        if (!NetworkManager.IsServer)
            return;

        //sortCharactersThisFrame = true;
        //scoreboardRoutine ??= StartCoroutine(UpdateScoreNextFrame());

        //IEnumerator UpdateScoreNextFrame ()
        //{
            //yield return null;

            if (active.Count < 1)
            {
                scoreboardRoutine = null;
                return;
                //yield break;
            }

            LogicAndMath.SortHighestToLowest(active, character => character.KillCount);
            LogicAndMath.AssignIndexes(active, (character, index) => character.rank = index + 1);

            var currentTop = active[0];
            var prevTop = this.prevTop;

            if ((
            prevCharCount != active.Count ||
            currentTop != prevTop || @override) && prevKills > 0)
            {
                //if ( //shit doesn't work, no clue why
                //    prevTop && prevTop.controlType == Character.CharacterType.localPlayer &&
                //    scoreStopWatch > serverRecord.Value
                //    )
                //{
                //    personalRecord = scoreStopWatch;
                //}

                if (!challengeComplete && prevTop && scoreStopWatch > serverRecord.Value)
                    serverRecord = new (prevTop.characterName,scoreStopWatch);
            }

            if (currentTop != prevTop)
            {
                StartStopWatch(progress);
                newLeader = true;
            }      

            NetworkManager.Manager.UpdateScoreboard();

            this.prevTop = currentTop;
            prevCharCount = active.Count;
            prevKills = currentTop.KillCount;

            scoreboardRoutine = null;
        //}
    }

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

        UpdateScoreBoard();

        pool.AddToPool(character);
        character.Chunk = null;

        //squadSpawnRoutine = StartCoroutine(BotSpawn());

        if (character == localPlayerCharacter)
        {
            localPlayerCharacter = null;
            CamFollow.main.Reset();
        }

        character.inventory.DropAllItems(character.deathDropRadius); //meh
    }

    public void KillCharacter(Character character)
    {
        if (true || character.controlType != Character.CharacterType.localBot || SpawnManager.Manager.spawnCharacter.CapDiff >= 0)
            //if this character is not a bot, or the spawn cap is unmet...
        {
            StartRespawn(character);
        }
        else
        {
            SpawnManager.Manager.RemoveCharacter(character); //kind of cringe that this is removing the character from spawn manager, which then removes it from this...
            UpdateScoreBoard();
        }

        character.life++;
        //UpdateScoreBoard();
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

            active.Remove(character); //just to reorder them to give newer players a chance when they tie
            active.Add(character);

            NetworkManager.Manager.SetKills(character, 0);

            character.Chunk = null;

            NetworkManager.Manager.ToggleLimbo(character, true);

            UpdateScoreBoard();

            yield return new WaitForSeconds(respawnWait);

            //active.Add(character);

            //active.Add(character);

            NetworkManager.Manager.ToggleLimbo(character, false);

            character.gameObject.SetActive(true);
            character.SetPos(ChunkManager.Manager.GetRandomPos()); //these are still sloppy af but work for now
            //Debug.Log($"updated pos, {NetworkManager.Manager.server.updateCharData.List.Count} character updates");

            character.SetHP(character.maxHp); //i think this is a fine place to put it shrugging emoji
            //Debug.Log($"updated hp, {NetworkManager.Manager.server.updateCharData.List.Count} character updates");
            character.Type = type;

            character.UpdateChunk();


            //UpdateScoreBoard();
        }
    }
}

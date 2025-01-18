using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.Events;

public class Character : MonoBehaviour
{
    //public static List<Character> all = new();//, chunkless = new();
    public int id, rank, life = 0;
    public string characterName;
    [HideInInspector]
    public Clan clan;
    public PlayerController userController;
    public BotControllerV2 botController;
    //public SpriteRenderer sprite;
    //public Color dangerColor = Color.white;
    public float baseMoveSpeed = 5, moveSpeed = 0, deathDropRadius = 1, hp = 10, maxHp = 10, jumpDuration = .2f, jumpCooldown = .1f;

    int killCount;

    public UnityEvent<Vector2> onMove, onLook;
    public UnityEvent onReset;
    public UnityEvent<Color> onAssignedClan;

    public int KillCount
    {
        get { return killCount; }

        set
        {
            killCount = value;
        }
    }

    //public float MoveSpeed
    //{
    //    get
    //    {
    //        //var speed = baseMoveSpeed;
    //        if (digging || filling) 
    //            return digMoveSpeed;
    //        else 
    //            return baseMoveSpeed;
    //    }
    //}

    public Chunk chunk;
    public TrenchCollider trenchCollider;
    public AmmoReserve reserve;
    public Inventory inventory;
    public bool constantlyUpdateChunk = false, moving = false;

    public CharacterType controlType = CharacterType.none;
    CharacterType type;
    public CharacterType Type
    {
        get
        {
            return type;
        }

        set
        {
            if (userController)
                userController.enabled = value == CharacterType.localPlayer;

            if (botController)
                botController.enabled = value == CharacterType.localBot;

            type = value;
        }
    }

    public void AssignClan(Clan clan)
    {
        this.clan = clan;

        onAssignedClan.Invoke(clan.color);
    }

    // Start is called before the first frame update
    void Awake()
    {
        Type = controlType;
        moveSpeed = baseMoveSpeed;
        //all.Add(this);
        //chunkless.Add(this);
        //startColor = sprite.color;
        //detector.onDetect.AddListener(UpdateVulnerable);
        //detector.onDetect.AddListener(collider.ToggleSafe);
    }

    private void Start()
    {
        SetPos(transform.position,false);
        //collider.onHit.AddListener(
        //delegate
        //{
        //    //transform.position = ChunkManager.Manager.GetRandomPos();
        //    //UpdateChunk();
        //    Kill();
        //    CharacterManager.Manager.RemoveCharacter(this);
        //    //Debug.Log(gameObject.name + " was hit");
        //});

        trenchCollider.onHit = bullet => Damage(bullet.damage,bullet.source, bullet.shooterLife);
    }

    //private void Update()
    //{
    //    if (Input.GetKey(KeyCode.Space))
    //    {
    //        if (Input.GetKey(KeyCode.LeftShift))
    //            DataManager.GetPrivateCharacterData(this);
    //        else
    //            DataManager.GetPrivateCharacterData(this).ToJson();
    //    }
    //}

    public void Damage (float hp, Character killer, int killerLife)
    {
        SetHP(MathF.Max(this.hp - hp,0));

        if (this.hp == 0)
        {
            if (killer.life == killerLife)
            {
                killer.killCount++;

                if (NetworkManager.IsServer)
                {
                    NetworkManager.Manager.SetKills(killer, killer.killCount);
                }
            }

            KillThis();
        }

        //SetHP(newHp);
    }

    public void Heal (float hp)
    {
        SetHP(Mathf.Min(maxHp, this.hp + hp));
    }

    public void SetHP (float hp)
    {
        //Debug.Log($"hp was set to {hp}");

        this.hp = hp;

        if (!NetworkManager.IsServer)
            return;

        NetworkManager.Manager.SetHealth(this,hp);

        //Debug.Log($"set character {id} hp to {hp}");
    }

    //private void OnEnable()
    //{
    //    //detector.DetectTrench(0);
    //    UpdateChunk();

    //}

    //private void OnDestroy()
    //{
    //    all.Remove(this);
    //    //if (chunkless.Contains(this)) chunkless.Remove(this);
    //}

    // Update is called once per frame
    void Update()
    {

        if (constantlyUpdateChunk)
        {
            UpdateChunk();
            if (type != CharacterType.remote)
                inventory.DetectItems();
        }

//#if !UNITY_SERVER || UNITY_EDITOR

//        //if (type == CharacterType.localPlayer)
//        //    SetPos(transform.position); //just for network testing

//#endif
//        //lastPos = tran
    }

    //private void LateUpdate()
    //{
    //    lastPos = transform.position;
    //}
    public void Jump ()
    {
        trenchCollider.ExitTrench(jumpDuration);
    }

    public void LookInDirection (Vector2 direction)
    {
        onLook.Invoke(direction);

        inventory.Aim(direction);
    }

    public void MoveInDirection(Vector2 direction)
    {
        Vector3 dir = moveSpeed * Time.deltaTime * direction;

        //trenchCollider.MoveToPos(transform.position + dir);

        SetPos(trenchCollider.MoveToPos(transform.position + dir));

        onMove.Invoke(direction);
    }

    public void MoveToPos (Vector2 pos) //doesn't utilize trenchCollider yet
    {
        SetPos(Vector2.MoveTowards(transform.position, pos, moveSpeed * Time.deltaTime));

        onMove.Invoke(pos - (Vector2)transform.position);

        //MoveInDirection(Vector2.ClampMagnitude(pos - (Vector2)transform.position, moveSpeed * Time.deltaTime));

        //transform.position += d * Time.deltaTime;
    }

    //bool posWasSetThisFrame = false;
    //Coroutine waitForPosRoutine;

    public void SetPos (Vector2 pos, bool sync = true) //this would be the rpc
    {
        //if ((Vector2)transform.position == pos) return;

        transform.position = pos;

        if (!constantlyUpdateChunk)
        {
            UpdateChunk();
            inventory.DetectItems();
        }

        if (sync)
            NetworkManager.Manager?.SetPos(pos,id);

        //posWasSetThisFrame = true;

        //if (waitForPosRoutine != null)
        //    StopCoroutine(waitForPosRoutine);
        
        //waitForPosRoutine = StartCoroutine(WaitForPosSet());
    }

    //IEnumerator WaitForPosSet ()
    //{
    //    yield return null;

    //    if (!posWasSetThisFrame)
    //    {
    //        moving = false;
    //        inventory.UpdateChunks();
    //    }
    //}

    public Chunk Chunk
    {
        get
        {
            return chunk;
        }

        set
        {
            if (chunk == value) return;

            if (chunk != null)
            {
                chunk.RemoveCharacter(this);
            }

            if (value != null)
            {
                value.AddCharacter(this);
            }
            chunk = value;
        }
    }

    public void UpdateChunk ()
    {
        Chunk = ChunkManager.Manager.ChunkFromPosClamped(transform);
    }

    public void KillThis ()
    {
        if (reserve)
            reserve.DropEverything(deathDropRadius);

        if (inventory)
            inventory.DropAllItems(deathDropRadius, true);

        CharacterManager.Manager.KillCharacter(this);
    }

    public void RemoveSelf ()
    {
        CharacterManager.Manager.RemoveCharacter(this);
        if (inventory)
            inventory.OnRemoved();
    }

    public void ResetSelf (bool clearItems = false) //clearItems parameter in case I need to remove character without dropping it's items
    {
        //reset code

        if (reserve)
            reserve.Clear();

        if (clearItems)
        {
            if (inventory)
                inventory.ResetInventory(true);
        }

        trenchCollider.ResetCollider();

        hp = maxHp; //shouldn't use set, because then the server sends new character data every time it resets a character object

        killCount = 0;

        moveSpeed = baseMoveSpeed;
        //CharacterManager.Manager.UpdateScoreBoard();

        onReset.Invoke();

        botController.ResetBot();

        Type = CharacterType.none;
    }

    public virtual string InfoString (string separator = " ")
    {
        return $"{characterName}\n{hp:F1}/{maxHp:F1} hp";
    }

    //public virtual DataDict<object> Data
    //{
    //    get
    //    {
    //        return new DataDict<object>(
    //        (Naming.id, id),
    //        (Naming.pos, new DataDict<float>((Naming.x, transform.position.x), (Naming.y, transform.position.y) )),
    //        (Naming.maxHp, maxHp),
    //        (Naming.hp, hp)
    //        );
    //    }
    //}

    //public virtual DataDict<object> PublicData
    //{
    //    get
    //    {
    //        var publicData = Data;

    //        if (gun)
    //            DataDict<object>.Combine(ref publicData, (Naming.gun, gun.PublicData));

    //        return publicData;
    //    }
    //}

    //public virtual DataDict<object> PrivateData
    //{
    //    get
    //    {
    //        var privateData = Data;

    //        if (gun)
    //            DataDict<object>.Combine(ref privateData, (Naming.gun, gun.PrivateData));

    //        if (reserve)
    //            DataDict<object>.Combine(ref privateData, (Naming.amoReserve, reserve.Data));

    //        return privateData;
    //    }
    //}

    public enum CharacterType
    {
        none,
        localPlayer,
        remote,
        localBot
    }
}

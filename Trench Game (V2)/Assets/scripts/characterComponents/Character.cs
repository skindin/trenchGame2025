using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.Events;

public class Character : MonoBehaviour
{
    //public static List<Character> all = new();//, chunkless = new();
    public int id, //for network purposes
        rank, //current place on kill streak scoreboard
        life = 0; //to prevent their killstreak from increasing if a bullet fired BEFORE their death killed a character AFTER their death
    public string characterName;
    [HideInInspector]
    public Clan clan;
    public PlayerController userController;//user controler behavior. allows input to control this character.
                                           //max of one is enabled per device, unless I add splitscreen later on
    public BotBrains.ObserverBot botController; //allows a bot script to control this character
    //public SpriteRenderer sprite;
    //public Color dangerColor = Color.white;
    public float baseMoveSpeed = 5, //move speed before modified by other features
        moveSpeed = 0, //current move speed, after being modified by other features
        deathDropRadius = 1, //how far items in inventory will be dropped when this character dies
        hp = 10, //hit points (health)
        maxHp = 10, //beginning and maximum hit points
        jumpDuration = .2f, //jumping doesn't really move the character right now, it just disables their trench collisions so they can leave a trench when they want
        jumpCooldown = .1f;

    int killCount; //ie kill streak: how many characters this one has killed since their last death

    //for animation and sprite color purposes.

    public UnityEvent<Vector2> onMove, onLook;
    public UnityEvent onReset;
    public UnityEvent<Color> onAssignedClan;

    //public UnityEvent<float, Character, int> onDamaged;

    public int KillCount //not sure why this is here...?
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

    //public Chunk chunk;
    public TrenchCollider trenchCollider;// specialized collider to detect bullets and conceal from bullets when within a trench, also to keep within a trench if they walk outside without jumping
    public AmmoReserve reserve;
    public Inventory inventory;
    public bool constantlyUpdateChunk = false, moving = false;

    public CharacterType controlType = CharacterType.none;
    //CharacterType type;
    public CharacterType Type
    {
        get
        {
            return controlType;
        }

        set
        {
            if (userController)
                userController.enabled = value == CharacterType.localPlayer;

            if (botController)
                botController.enabled = value == CharacterType.localBot;

            controlType = value;
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

    public void Damage (float hp, Character aggressor, int life)
    {
        SetHP(MathF.Max(this.hp - hp,0));

        //botController.OnDamaged(hp, aggressor, life);

        if (this.hp == 0)
        {
            if (aggressor.life == life)
            {
                aggressor.killCount++;

                if (NetworkManager.IsServer) //REVIEWERS: ignore network code
                {
                    NetworkManager.Manager.SetKills(aggressor, aggressor.killCount);
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
    void LateUpdate()
    {

        if (constantlyUpdateChunk)
        {
            UpdateChunk();
            if (controlType != CharacterType.remote) //as long as this character isn't being controlled by a remote computer (server side bot, or remote player), run item detection
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
    public void Jump ()//currently just allows their collider to exit the trench
    {
        trenchCollider.ExitTrench(jumpDuration);
    }

    public void LookInDirection (Vector2 direction)
    {
        onLook.Invoke(direction);

        inventory.Aim(direction);
    }

    public void MoveInDirection(Vector2 direction) //for direction based movement
    {
        Vector3 dir = moveSpeed * Time.deltaTime * direction;

        //trenchCollider.MoveToPos(transform.position + dir);

        SetPos(trenchCollider.MoveToPos(transform.position + dir));

        onMove.Invoke(direction);
    }

    public void MoveToPos (Vector2 pos) //for position based movement (for bots). doesn't utilize trenchCollider yet
    {
        SetPos(Vector2.MoveTowards(transform.position, pos, moveSpeed * Time.deltaTime));

        onMove.Invoke(pos - (Vector2)transform.position);

        //MoveInDirection(Vector2.ClampMagnitude(pos - (Vector2)transform.position, moveSpeed * Time.deltaTime));

        //transform.position += d * Time.deltaTime;
    }

    //bool posWasSetThisFrame = false;
    //Coroutine waitForPosRoutine;

    public void SetPos (Vector2 pos, bool sync = true) //to minimize code repetition, also to prevent animations from playing when their position is just set
    {
        //if ((Vector2)transform.position == pos) return;

        transform.position = pos;

        if (true || !constantlyUpdateChunk)
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

    //public Chunk Chunk
    //{
    //    get
    //    {
    //        return chunk;
    //    }

    //    set
    //    {
    //        if (chunk == value) return;

    //        if (chunk != null)
    //        {
    //            chunk.RemoveCharacter(this);
    //        }

    //        if (value != null)
    //        {
    //            value.AddCharacter(this);
    //        }
    //        chunk = value;
    //    }
    //}

    public void UpdateChunk ()
    {
        CharacterManager.Manager.chunkArray.UpdateObjectChunk(this);
    }

    public void KillThis ()
    {
        if (reserve)
            reserve.DropEverything(deathDropRadius);

        if (inventory)
            inventory.DropAllItems(deathDropRadius, true);

        CharacterManager.Manager.KillCharacter(this);
    }

    public void RemoveSelf ()//tbh i can't remember why i wrote this
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

        //botController.ResetBot();

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

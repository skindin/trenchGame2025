using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class Character : MonoBehaviour
{
    //public static List<Character> all = new();//, chunkless = new();
    public int id, rank, life = 0;
    public string characterName;
    public PlayerController userController;
    public BotController aiController;
    public SpriteRenderer sprite;
    public Color dangerColor = Color.white;
    Color startColor;
    public float baseMoveSpeed = 5, digMoveSpeed = 1, initialDigSpeed = 5, deathDropRadius = 1, hp = 10, maxHp = 10;

    int killCount;

    public int KillCount
    {
        get { return killCount; }

        set
        {
            killCount = value;
        }
    }

    public float MoveSpeed
    {
        get
        {
            //var speed = baseMoveSpeed;
            if (digging || filling) 
                return digMoveSpeed;
            else 
                return baseMoveSpeed;
        }
    }

    public Chunk chunk;
    public Collider collider;
    //public TrenchDetector detector;
    //public TrenchDigger digger; //eventually this will be attached to the shovel...?
    //public Gun gun;
    public AmmoReserve reserve;
    public Inventory inventory;
    public bool 
        digging = false, filling = false, constantDig = false, //too much trouble to comment these out atm
        constantlyUpdateChunk = false, shooting = false, moving = false; //most of these will probably be moved once i design shovels

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

            if (aiController)
                aiController.enabled = value == CharacterType.localBot;

            type = value;
        }
    }

    // Start is called before the first frame update
    void Awake()
    {
        Type = controlType;
        //all.Add(this);
        //chunkless.Add(this);
        startColor = sprite.color;
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

        collider.onHit = bullet => Damage(bullet.damage,bullet.source);
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

    public void Damage (float hp, Character killer)
    {
        this.hp -= hp;

        if (this.hp <= 0)
        {
            this.hp = 0;

            KillThis();

            killer.killCount++;
        }
    }

    public void Heal (float hp)
    {
        this.hp = Mathf.Min(maxHp, this.hp + hp);
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
            inventory.DetectItems();
        }

        //lastPos = tran
    }

    //private void LateUpdate()
    //{
    //    lastPos = transform.position;
    //}

    public void MoveInDirection(Vector2 direction)
    {
        Vector3 dir = Vector2.ClampMagnitude(direction, MoveSpeed * Time.deltaTime);

        SetPos(transform.position + dir);
    }

    public void MoveToPos (Vector2 pos)
    {
        SetPos(Vector2.MoveTowards(transform.position, pos, MoveSpeed * Time.deltaTime));

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
            NetworkManager.Manager?.SetPos(pos);

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

    public void UpdateVulnerable (bool trenchStatus)
    {
        if (trenchStatus)
        {
            sprite.color = startColor;
        }
        else
        {
            sprite.color = dangerColor;
        }
    }

    public void KillThis ()
    {
        if (reserve)
            reserve.DropEverything(deathDropRadius);

        if (inventory)
            inventory.DropAllItems();

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
        digging = filling = shooting = false;

        if (clearItems)
        {
            if (inventory)
                inventory.ResetInventory(true);

            if (reserve)
                reserve.Clear();
        }

        collider.ResetCollider();

        hp = maxHp;

        killCount = 0;
        //CharacterManager.Manager.UpdateScoreBoard();

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

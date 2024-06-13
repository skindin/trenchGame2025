using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    //public static List<Character> all = new();//, chunkless = new();
    public Controller userController;
    public AIController aiController;
    public SpriteRenderer sprite;
    public Color dangerColor = Color.white;
    Color startColor;
    public float baseMoveSpeed = 5, digMoveSpeed = 1, initialDigSpeed = 5, deathDropRadius = 1, hp = 10, maxHp = 10;
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
    public Gun gun;
    public AmoReserve reserve;
    public Inventory inventory;
    public bool digging = false, filling = false, constantDig = false, constantDetect = false, shooting = false; //most of these will probably be moved once i design shovels

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
                aiController.enabled = value == CharacterType.localNPC;

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
        UpdateChunk();
        //collider.onHit.AddListener(
        //delegate
        //{
        //    //transform.position = ChunkManager.Manager.GetRandomPos();
        //    //UpdateChunk();
        //    Kill();
        //    CharacterManager.Manager.RemoveCharacter(this);
        //    //Debug.Log(gameObject.name + " was hit");
        //});

        collider.onHit = bullet => Damage(bullet.damage);
    }



    public void Damage (float hp)
    {
        this.hp -= hp;

        if (this.hp <= 0)
        {
            this.hp = 0;

            Kill();
        }
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
    //void Update()
    //{
    //    //if (constantDetect)
    //    //    detector.DetectTrench(0);
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

    void SetPos (Vector2 pos) //this would be the rpc
    {
        transform.position = pos;

        UpdateChunk();
        inventory.DetectItems();
    }

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

    public void Kill ()
    {
        if (reserve)
            reserve.DropEverything(deathDropRadius);

        if (inventory)
            inventory.DropAllItems();

        CharacterManager.Manager.RemoveCharacter(this);
    }

    public void ResetCharacter (bool clearItems = false) //clearItems parameter in case I need to remove character without dropping it's items
    {
        //reset code
        digging = filling = constantDig = constantDetect =  shooting = false;

        if (clearItems)
        {
            if (inventory)
                inventory.ResetInventory(true);

            if (reserve)
                reserve.Clear();
        }

        collider.ResetCollider();

        hp = maxHp;

        Type = CharacterType.none;
    }

    public enum CharacterType
    {
        none,
        localPlayer,
        remote,
        localNPC
    }
}

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
    public float moveSpeed = 5, digMoveSpeed = 1, initialDigSpeed = 5, deathDropRadius = 1;
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
                aiController.enabled = value == CharacterType.npc;

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
        collider.onBulletHit.AddListener(
        delegate
        {
            //transform.position = ChunkManager.Manager.GetRandomPos();
            //UpdateChunk();
            Kill();
            CharacterManager.Manager.RemoveCharacter(this);
            //Debug.Log(gameObject.name + " was hit");
        });
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

    /// <summary>
    /// Takes vector2 of magnitude between 0 and 1
    /// </summary>
    /// <param name="input"></param>
    public void Move(Vector2 input)
    {
        Vector3 dir = input;
        var speed = moveSpeed;
        if (digging || filling) speed = digMoveSpeed;

        dir = Vector2.ClampMagnitude(dir, 1) * speed;

        transform.position += dir * Time.deltaTime;

        UpdateChunk();

        //if (!digging && !filling)
        //    detector.DetectTrench(0);
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

    public void Dig (Vector2 digPoint, bool stop = false)
    {
        //if (!stop)
        //{
        //    if (!digger.DigTrench(digPoint, Time.deltaTime))
        //    {
        //        digging = false;
        //        return;
        //    }
        //}
        //else if (!constantDig)
        //{
        //    digger.StopDigging();
        //}

        //digging = !stop;

        //if (digging)
        //{
        //    UpdateVulnerable(true);
        //    detector.SetStatus(true);
        //}
    }

    public void Fill (Vector2 fillPoint, bool stop = false)
    {
        //if (stop)
        //{
        //    digger.StopFilling();
        //    filling = false;
        //    return;
        //}

        //UpdateVulnerable(false);
        //detector.SetStatus(false);

        //digger.FillTrenches(fillPoint, Time.deltaTime);
        //filling = true;
    }

    public void Kill ()
    {
        if (reserve)
            reserve.DropEverything(deathDropRadius);

        if (inventory)
            inventory.DropAllItems();
    }

    public void ResetCharacter (bool clearItems = false) //clearItems parameter in case I need to remove character without dropping it's items
    {
        //reset code
        digging = filling = constantDig = constantDetect =  shooting = false;

        if (clearItems)
        {
            inventory.ResetInventory(true);
            reserve.Clear();
        }

        Type = CharacterType.none;
    }

    public enum CharacterType
    {
        none,
        localPlayer,
        remotePlayer,
        npc
    }
}

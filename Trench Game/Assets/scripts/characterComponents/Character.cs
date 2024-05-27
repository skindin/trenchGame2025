using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    public static List<Character> all = new();//, chunkless = new();
    public SpriteRenderer sprite;
    public Color dangerColor = Color.white;
    Color startColor;
    public float moveSpeed = 5, digMoveSpeed = 1, initialDigSpeed = 5;
    public Chunk chunk;
    public Collider collider;
    //public TrenchDetector detector;
    //public TrenchDigger digger; //eventually this will be attached to the shovel...?
    public Gun gun;
    public AmoReserve reserve;
    public Inventory inventory;
    public bool digging = false, filling = false, constantDig = false, constantDetect = false, shooting = false;

    // Start is called before the first frame update
    void Awake()
    {
        all.Add(this);
        //chunkless.Add(this);
        startColor = sprite.color;
        //detector.onDetect.AddListener(UpdateVulnerable);
        //detector.onDetect.AddListener(collider.ToggleSafe);
    }

    private void Start()
    {
        //detector.DetectTrench(0);
        UpdateChunk();
        collider.onBulletHit.AddListener(
        delegate
        {
            SpawnManager.Manager.Relocate(transform);
            UpdateChunk();
            //detector.DetectTrench(0);
        });
    }

    private void OnDestroy()
    {
        all.Remove(this);
        //if (chunkless.Contains(this)) chunkless.Remove(this);
    }

    // Update is called once per frame
    void Update()
    {
        //if (constantDetect)
        //    detector.DetectTrench(0);
    }

    public void Move(Vector2 direction)
    {
        Vector3 dir = direction;
        var speed = moveSpeed;
        if (digging || filling) speed = digMoveSpeed;
        transform.position += dir * speed * Time.deltaTime;

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
        Chunk = ChunkManager.Manager.ChunkFromPos(transform.position, true);
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
}

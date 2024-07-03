using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using static UnityEditor.Progress;
//using UnityEngine.Events;
//using static UnityEditor.Progress;

public class Item : MonoBehaviour
{
    public int id;
    public Transform defaultContainer;
    public ItemModel itemModel;
    public Character wielder;
    //public bool currentlyHeld = false;
    public static List<Item> all = new();
    Chunk chunk = null;
    public Chunk Chunk
    {
        get
        {
            return chunk;
        }

        set
        {
            if (chunk == value)
            {
                //Debug.Log($"Item {this} {gameObject.GetInstanceID()} chunk was already {(chunk == null ? "null" : $"chunk {chunk.adress}")}");
                return;
            }

            chunk?.RemoveItem(this);

            chunk = value; //you wouldn't BELIEVE how important the order of these three lines is

            chunk?.AddItem(this);

            //Debug.Log($"Item {this}{gameObject.GetInstanceID()} chunk was set to {(value == null ? "null" : $"Chunk {chunk.adress}")}");
        }
    }

    public virtual string Verb { get; } = "use";

    public bool passivePickup = false;

    private void Awake()
    {
        //Debug.Log(gameObject.name + " ran base item awake function");
        //ResetItem();
        ItemAwake();

        //currentlyHeld = wielder;
    }

    public virtual void ItemAwake ()
    {
        if (!all.Contains(this))
            all.Add(this);
    }

    private void Start()
    {
        //if (!wielder)
        //    UpdateChunk();

        ItemStart();
    }

    public virtual void ItemStart()
    {

    }

    private void OnDestroy()
    {
        all.Remove(this);

        if (chunk != null)
            chunk.RemoveItem(this);
    }

    private void Update()
    {
        ItemUpdate();
    }

    public virtual void ItemUpdate ()
    {
        //shrugging emoji
    }

    public virtual void Action()
    {

    }

    public void UpdateChunk()
    {
        Chunk = ChunkManager.Manager.ChunkFromPosClamped(transform);
    }

    public virtual Coroutine Pickup (Character character, out bool wasDispatched, out bool wasDestroyed, out bool inCharInventory
        //, bool shrinToZero = false
        )
    {
        if (wielder != character)
        {
            wielder = character;

            //transform.localPosition = Vector3.zero;
        }

        Chunk = null;
        wasDestroyed = false;
        wasDispatched = true;
        var characterLife = character.life;

        inCharInventory = true;

        return StartCoroutine(MoveToCharacter());

        IEnumerator MoveToCharacter ()
        {
            var ratio = 0f;
            Vector2 startPos = transform.position;

            while (ratio <= 1)
            {
                yield return null;
                if (characterLife != character.life)
                {
                    //DestroySelf();
                    Drop(transform.position);
                    yield break;
                }
                ratio += Time.deltaTime / ItemManager.Manager.grabDur;
                transform.position = Vector2.Lerp(startPos, character.transform.position, ratio);
            }

            transform.parent = character.transform;
        }
    }

    public void Pickup (Character character) //not overidable, becase it's just a shorthand
    {
        Pickup(character, out _, out _, out _);
    }


    public virtual void DropLogic (Vector2 pos, out bool destroyedSelf)
    {
        wielder = null;

        var worldPos = transform.position;
        var preDropScale = transform.localScale;
        transform.parent = defaultContainer;
        transform.position = worldPos;
        //transform.position = pos;
        var postDropScale = transform.localScale;
        transform.rotation = Quaternion.identity;
        //UpdateChunk();
        destroyedSelf = false;

        //Debug.Log($"Item {this} {this.gameObject.GetInstanceID()} was dropped");
    }

    public void Drop(Vector2 pos, out bool destroyedSelf)
    {
        DropLogic(pos, out destroyedSelf);

        bool localDestroyed = destroyedSelf;

        if (!destroyedSelf)
        {
            StartCoroutine(MoveToDropPos());
        }

        IEnumerator MoveToDropPos ()
        {
            //transform.SetParent(defaultContainer);

            var ratio = 0f;

            Vector2 startPos = transform.position;

            while (ratio <= 1)
            {
                yield return null;
                ratio += Time.deltaTime / ItemManager.Manager.grabDur;
                transform.position = Vector2.Lerp(startPos, pos, Mathf.Min(ratio,1));
            }

            if (!localDestroyed && gameObject.activeSelf)
                UpdateChunk();
        }
    }

    public void Drop(Vector2 pos)
    {
        Drop(pos, out _);
    }

    Coroutine spawnRoutine;

    public void Spawn(Vector2 pos, float delay, float scaleDuration)
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
        }

        spawnRoutine = StartCoroutine(DelayedSpawn());

        IEnumerator DelayedSpawn()
        {
            Vector3 endScale = transform.localScale;
            //Debug.Log($"Initial endScale: {endScale}");

            transform.localScale = Vector3.zero;

            yield return new WaitForSeconds(delay);

            var ratio = 0f;

            while (ratio < 1)
            {
                yield return null;
                ratio += Time.deltaTime / scaleDuration;
                transform.localScale = Vector3.Lerp(Vector3.zero, endScale, ratio);
                //Debug.Log($"Current ratio: {ratio}, Current scale: {transform.localScale}");
            }

            // Ensure final scale is set correctly
            transform.localScale = endScale;
            //Debug.Log($"Final scale set to: {transform.localScale}");

            Drop(pos);
        }
    }

    bool destroying = false;
    public Coroutine DestroySelf ()
    {
        if (destroying)
            return null;

        //destroy logic here shruggin emoji
        //StopAllCoroutines();
        destroying = true;

        return StartCoroutine(Shrink());

        System.Collections.IEnumerator Shrink()
        {
            var ratio = 0f;

            //transform.parent = defaultContainer;
            var prevParent = transform.parent;
            //transform.parent = null;
            Vector3 startScale = transform.lossyScale;
            //transform.parent = prevParent;

            while (ratio <= 1)
            {
                yield return null;
                ratio += Time.deltaTime / ItemManager.Manager.deleteDur;

                var newScale = Vector3.Lerp(startScale, Vector3.zero, ratio);

                if (prevParent != transform.parent)
                    throw new System.Exception($"Something changed {gameObject}'s parent!");
                //prevParent = transform.parent;
                //transform.parent = null;
                transform.localScale = newScale;
                transform.parent = prevParent;
            }

            yield return null;

            if (prevParent != transform.parent)
                throw new System.Exception($"Something changed {gameObject}'s parent!");
            //prevParent = transform.parent;
            //transform.parent = null;
            transform.localScale = startScale;
            //transform.parent = prevParent;

            SpawnManager.Manager.RemoveItem(this);
        }
    }

    public virtual void ResetItem ()
    {
        ItemAwake();
        wielder = null;
        destroying = false;
    }

    public virtual string InfoString(string separator = " ")
    {
        return itemModel.name;// { $"Tier {model.tier}" };
    }

    //public virtual DataDict<object> PublicData //to be used when observing an item that another character is holding
    //{
    //    get
    //    {
    //        return new(
    //        (Naming.id, id),
    //        (Naming.name, model.name),
    //        (Naming.pos, new DataDict<float>((Naming.x, transform.position.x), (Naming.y, transform.position.y))),
    //        (Naming.wielderId, (wielder) ? wielder.id : -1)
    //        );
    //    }
    //}

    //public virtual DataDict<object> PrivateData //to be used when observing an item not being held or being held by wielder
    //{
    //    get
    //    {
    //        return PublicData;
    //    }
    //}
}

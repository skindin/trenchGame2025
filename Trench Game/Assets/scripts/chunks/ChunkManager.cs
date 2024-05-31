using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    static ChunkManager manager;

    public static ChunkManager Manager
    {
        get
        {
            if (manager == null)
            {
                manager = FindObjectOfType<ChunkManager>();
                //if (manager == null)
                //{
                //    GameObject go = new GameObject("GameManager");
                //    manager = go.AddComponent<ChunkManager>();
                //    DontDestroyOnLoad(go);
                //}
            }
            return manager;
        }
    }

    //public float worldSize = 100;
    //public int chunkArraySize = 5, mapSize = 10;
    public float chunkSize = 10, maxWorldSize = 100;
    float worldSize;
    int chunkArraySize;

    public Chunk[,] chunks;
    //public int maxChunksPooled = 10;
    //readonly List<Chunk> chunkPool = new();
    public ObjectPool<Chunk> chunkPool;

    public bool logOutOfBounds = false, drawChunks = false;

    private void Awake()
    {
        chunkPool = new(
        newFunc: () => new Chunk(),
        resetAction: chunk => chunk.Reset(),
        disableAction: null,
        removeAction: null
        );

        InstantiateChunks();
    }

    private void Update()
    {
        if (drawChunks) DrawChunks();
    }

    List<Chunk> reusableChunkList = new();

    public void DrawChunks ()
    {
        foreach (var chunk in chunkPool.objects)
        {
            DrawChunk(chunk, Color.black);
        }

        foreach (var chunk in this.chunks)
        {
            if (chunk != null) DrawChunk(chunk, Color.green);
        }

        //var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        //GeoFuncs.MarkPoint(ClampPosToNearestChunk(mousePos), .5f, Color.red);
        //var chunks = ChunksFromLine(Vector2.zero, mousePos, reusableChunkList, true, true);
        //foreach (var chunk in chunks)
        //{
        //    if (chunk != null) DrawChunk(chunk, Color.green);
        //}
    }

    public void InstantiateChunks ()
    {
        chunkArraySize = Mathf.FloorToInt(maxWorldSize / chunkSize);
        worldSize = chunkArraySize * chunkSize;
        chunks = new Chunk[chunkArraySize, chunkArraySize];
    }

    public Vector2 GetChunkPos (Chunk chunk)
    {
        return AdressToPos(chunk.adress);
    }

    public Vector2 AdressToPos (Vector2Int adress)
    {
        var min = -new Vector2(worldSize, worldSize) / 2;
        var pos = ((Vector2)adress * chunkSize) + min;
        return pos;
    }

    public Vector2Int PosToAdress (Vector2 pos)
    {
        var min = -new Vector2(worldSize, worldSize) / 2;
        var delta = pos - min;
        var adress = Vector2Int.FloorToInt(delta / chunkSize);
        return adress;
    }

    public Vector2 GetRandomPos ()
    {
        var x = Random.Range(-worldSize, worldSize)/2;
        var y = Random.Range(-worldSize, worldSize)/2;

        return new(x, y);
    }

    public Vector2 GetRandomPosMargin (float margin)
    {
        var edge = worldSize - margin;

        var x = Random.Range(-edge, edge) / 2;
        var y = Random.Range(-edge, edge) / 2;

        return new(x, y);
    }

    /// <summary>
    /// Will still return null if outside bounds!
    /// </summary>
    /// <param name="adress"></param>
    /// <param name="newIfNone"></param>
    /// <returns></returns>
    public Chunk ChunkFromAdress (Vector2Int adress, bool newIfNone = false, bool debugLines = false)
    {
        if (debugLines) DrawAdressBox(adress, Color.magenta); 

        var min = Vector2Int.zero;

        if (adress != Vector2Int.Max(adress, min))
        {
            if (logOutOfBounds) Debug.LogError("adress was left of or below world box bounds");
            return null;
        }
        var max = Vector2Int.one * chunkArraySize - Vector2Int.one;
        if (adress != Vector2Int.Min(adress, max))
        {
            if (logOutOfBounds) Debug.LogError("adress was right of or above world box bounds");
            return null;
        }
        var chunk = chunks[adress.x, adress.y];
        if (chunk == null)
        {
            if (newIfNone)
            {
                chunk = NewChunk(adress);
            }
        }
        return chunk;
    }

    public Chunk NewChunk(Vector2Int adress)
    {
        var newChunk = chunkPool.GetFromPool();
        newChunk.adress = adress;
        //foreach (var character in Character.chunkless)
        //{
        //    if (PosToAdress(character.transform.position) == adress)
        //    {
        //        chunk.characters.Add(character);
        //        chunk.colliders.Add(character.collider);
        //    }
        //}
        chunks[adress.x, adress.y] = newChunk;

        return newChunk;
    }

    public void RemoveChunk (Chunk chunk)
    {
        chunks[chunk.adress.x, chunk.adress.y] = null;

        chunkPool.AddToPool(chunk);
    }

    public Chunk ChunkFromPos (Vector2 pos, bool newIfNone = false)
    {
        var adress = PosToAdress(pos);
        return ChunkFromAdress(adress,newIfNone);
    }

    public Chunk ChunkFromPosClamped (Transform transform, bool newIfNone = true)
    {
        var clampedPos = ClampPosToNearestChunk(transform.position, out var adress);
        transform.position = clampedPos;
        return ChunkFromAdress(adress, newIfNone);
    }

    public Vector2 ClampPosToNearestChunk (Vector2 pos, out Vector2Int closestAdress)
    {
        var adress = PosToAdress(pos);
        closestAdress = Vector2Int.Max(adress, Vector2Int.zero);
        closestAdress = Vector2Int.Min(closestAdress, (chunkArraySize-1) * Vector2Int.one);

        GetChunkBox(closestAdress, out var chunkMin, out var chunkMax);

        var closestPos = Vector2.Max(pos, chunkMin);
        closestPos = Vector2.Min(closestPos, chunkMax);

        return closestPos;
    }

    public Vector2 ClampPosToNearestChunk(Vector2 pos)
    {
        var adress = PosToAdress(pos);
        var closestAdress = Vector2Int.Max(adress, Vector2Int.zero);
        closestAdress = Vector2Int.Min(closestAdress, (chunkArraySize - 1) * Vector2Int.one);

        GetChunkBox(closestAdress, out var chunkMin, out var chunkMax);

        var closestPos = Vector2.Max(pos, chunkMin);
        closestPos = Vector2.Min(closestPos, chunkMax);

        return closestPos;
    }

    public Vector2Int[,] AdressesFromBox (Vector2 min, Vector2 max)
    {
        var minAdress = PosToAdress(min);
        var maxAdress = PosToAdress(max);// + Vector2Int.one;

        var adressDelta = maxAdress - minAdress;

        //adressDelta = Vector2Int.Max(adressDelta, Vector2Int.one);

        var output = new Vector2Int[adressDelta.x + 1, adressDelta.y + 1];

        for (int y = 0; y < adressDelta.y + 1; y++)
        {
            for (int x = 0; x < adressDelta.x + 1; x++)
            {
                output[x, y] = new(x+minAdress.x, y+minAdress.y);
            }
        }

        return output;
    }

    public Chunk[,] ChunksFromBox (Vector2 min, Vector2 max, bool newIfNone = false)
    {
        var minAdress = PosToAdress(min);
        var maxAdress = PosToAdress(max);// + Vector2Int.one;

        var adressDelta = maxAdress - minAdress;

        //adressDelta = Vector2Int.Max(adressDelta, Vector2Int.one);

        var output = new Chunk[adressDelta.x+1, adressDelta.y+1];

        for (int y = 0; y < adressDelta.y+1; y++)
        {
            for (int x = 0; x < adressDelta.x+1; x++)
            {
                var chunk = ChunkFromAdress(new(x+minAdress.x, y+minAdress.y), newIfNone);
                if (chunk != null)
                    output[x, y] = chunk;
            }
        }

        return output;
    }

    public List<Chunk> ChunksFromLine (Vector2 pointA, Vector2 pointB, List<Chunk> chunkList, bool newIfNone = false, bool debugLines = false, bool clearList = true)
    {
        if (clearList) chunkList.Clear();

        var min = Vector2.Min(pointA, pointB);
        var max = Vector2.Max(pointA, pointB);

        var adresses = AdressesFromBox(min, max);

        foreach (var adress in adresses)
        {
            GetChunkBox(adress, out var chunkMin, out var chunkMax);
            if (GeoFuncs.DoesLineIntersectBox(pointA,pointB,chunkMin,chunkMax,debugLines))
            {
                var chunk = ChunkFromAdress(adress, newIfNone);
                if (chunk != null && !chunkList.Contains(chunk))
                    chunkList.Add(chunk);
            }
        }

        return chunkList;
    }

    public void DrawChunk (Chunk chunk, Color color)
    {
        DrawAdressBox(chunk.adress,color);
    }

    public void GetChunkBox (Vector2Int adress, out Vector2 min, out Vector2 max)
    {
        min = AdressToPos(adress);
        max = AdressToPos(adress + Vector2Int.one);
    }

    public void DrawAdressBox (Vector2Int adress, Color color)
    {
        var min = AdressToPos(adress);
        var max = AdressToPos(adress + Vector2Int.one);
        GeoFuncs.DrawBox(min, max, color);
    }
}

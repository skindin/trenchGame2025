using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
//using static UnityEditor.PlayerSettings;

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
    public float worldSize = 100, minChunkSize = 10, chunkSize = 10;
    public int chunkArraySize;

    public Transform pointA, pointB;

    public Chunk[,] chunks;
    //public int maxChunksPooled = 10;
    //readonly List<Chunk> chunkPool = new();
    public ObjectPool<Chunk> chunkPool = new(
        newFunc: () => new Chunk(),
        resetAction: chunk => chunk.Reset(),
        disableAction: null,
        destroyAction: null
        );//

    public bool logOutOfBounds = false, drawChunks = false, drawObjectChunkLinks = false, drawLineTest = false;

    private void Awake()
    {

        InstantiateChunks();
    }

    private void Update()
    {
        if (drawChunks)
            DrawChunks();

        if (drawObjectChunkLinks)
            DrawObjectChunkLinks();

        if (drawLineTest)
        {
            ChunksFromLine(pointA.position, pointB.position, false, true);
        }
    }

    List<Chunk> reusableChunkList = new();

    public void DrawChunks ()
    {
        foreach (var chunk in chunkPool.objects)
        {
            DrawChunk(chunk, UnityEngine.Color.black);
        }

        foreach (var chunk in chunks)
        {
            if (chunk != null) 
                DrawChunk(chunk, UnityEngine.Color.green);
        }

        //var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        //GeoFuncs.MarkPoint(ClampPosToNearestChunk(mousePos), .5f, Color.red);
        //var chunks = ChunksFromLine(Vector2.zero, mousePos, reusableChunkList, true, true);
        //foreach (var chunk in chunks)
        //{
        //    if (chunk != null) DrawChunk(chunk, Color.green);
        //}
    }

    public void DrawObjectChunkLinks ()
    {
        foreach (var chunk in chunks)
        {
            if (chunk == null)
                continue;

            var center = AdressToPos(chunk.adress) + chunkSize / 2 * Vector2.one;

            foreach (var item in chunk.items)
            {
                Debug.DrawLine(center, item.transform.position, Color.blue);
            }

            foreach (var character in chunk.characters)
            {
                Debug.DrawLine(center, character.transform.position, Color.blue);
            }
        }
    }

    public void InstantiateChunks ()
    {
        chunkArraySize = Mathf.FloorToInt(worldSize / minChunkSize);
        chunkSize = worldSize / chunkArraySize;
        //worldSize = chunkArraySize * chunkSize;
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

    public Vector2 GetRandomPos (float margin = 0)
    {
        var edge = worldSize - margin;

        var x = UnityEngine.Random.Range(-edge, edge) / 2;
        var y = UnityEngine.Random.Range(-edge, edge) / 2;

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
        if (debugLines) DrawAdressBox(adress, UnityEngine.Color.magenta); 

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

    public Chunk ChunkFromPosClamped (Transform transform, float margin = 0, bool newIfNone = true)
    {
        var clampedPos = ClampToWorld(transform.position, out var adress, margin);
        transform.position = clampedPos;

        return ChunkFromAdress(adress, newIfNone);
    }

    public Vector2 ClampToWorld(Vector2 pos, out Vector2Int closestAdress, float margin = 0)
    {
        GetWorldBox(out var min, out var max, margin);

        pos = Vector2.Max(min, pos);
        pos = Vector2.Min(max, pos);

        closestAdress = PosToAdress(pos);

        closestAdress = Vector2Int.Min(closestAdress, (chunkArraySize - 1) * Vector2Int.one);

        return pos; //there's a reason this is so complicated! if you just clamp it to the world box, sometimes the pos doesn't convert to an adress within the world
    }

    public Vector2 ClampToWorld(Vector2 pos, float margin = 0)
    {
        return ClampToWorld(pos, out _, margin);
    }

    public bool IsPointInWorld (Vector2 point, bool debugLines = false)
    {
        //GetWorldBox(out var min, out var max);
        return GeoUtils.TestBoxPosSize(Vector2.zero, Vector2.one * worldSize, point, debugLines);
    }

    public Vector2[,] DistributePoints (Vector2 distributionBox, float margin = 0)
    {
        return GeoUtils.DistributePointsInBoxPosSize(Vector2.zero, (worldSize - margin * 2) * Vector2.one, distributionBox);
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

    public Chunk[,] ChunksFromBoxMinMax (Vector2 min, Vector2 max, bool newIfNone = false)
    {
        ClampToWorld(min, out var minAdress);
        ClampToWorld(max, out var maxAdress);// - chunkSize * .5f * Vector2.one); //fingies crossed

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

    public Chunk[,] ChunksFromBoxPosSize (Vector2 pos, Vector2 size, bool newIfNone = false)
    {
        Vector2 min = new Vector2(-size.x, -size.y) / 2 + pos;
        Vector2 max = new Vector2(size.x, size.y) / 2 + pos;
        return ChunksFromBoxMinMax(min, max,newIfNone);
    }

    public void GetWorldBox (out Vector2 min, out Vector2 max, float margin = 0)
    {
        min = -new Vector2(worldSize, worldSize) / 2 + Vector2.one * margin;
        max = -min;
    }

    public List<Chunk> ChunksFromLine(Vector2 pointA, Vector2 pointB, bool newIfNone = false, bool debugLines = false)
    {

        GetWorldBox(out var worldMin, out _);

        if (debugLines)
        {
            Debug.DrawLine(pointA, pointB, Color.green);
        }

        pointA = pointA - worldMin + (chunkSize * .5f * Vector2.one);
        pointB = pointB - worldMin + (chunkSize * .5f * Vector2.one);

        List<Chunk> chunks = new();

        var lineToCells = GeoUtils.CellsFromLine(pointA, pointB, chunkSize);

        foreach (var cell in lineToCells)
        {
            //cell -= Vector2Int.one * chunkSize;

            var adress = cell - Vector2Int.one;

            var chunk = ChunkFromAdress(adress);

            //GetChunkBox(cell, out var chunkMin, out var chunkMax);


            if (chunk != null)
            {
                chunks.Add(chunk);

                if (debugLines)
                {
                    DrawAdressBox(adress, Color.green);
                }
            }
            else if (debugLines)
            {
                //hue = Mathf.Repeat(hue, 1);
                //color = Color.HSVToRGB(hue, 1, 1);

                if (debugLines)
                {
                    DrawAdressBox(adress, Color.white);
                }
            }
        }

        return chunks;
    }

    public void DrawChunk (Chunk chunk, UnityEngine.Color color)
    {
        DrawAdressBox(chunk.adress,color);
    }

    public void GetChunkBox (Vector2Int adress, out Vector2 min, out Vector2 max)
    {
        min = AdressToPos(adress);
        max = AdressToPos(adress + Vector2Int.one);
    }

    public void DrawAdressBox (Vector2Int adress, UnityEngine.Color color)
    {
        var min = AdressToPos(adress);
        var max = AdressToPos(adress + Vector2Int.one);
        GeoUtils.DrawBoxMinMax(min, max, color);
    }

    public T FindClosestCharacterWithinBoxPosSize<T>(Vector2 pos, Vector2 size, Func<T, bool> condition = null, Chunk[,] chunks = default, bool debugLines = false) where T : Character
    {
        return FindClosestObjectWithinBoxPosSize(pos, size, chunk => chunk.GetCharacters<T>(), condition, chunks, debugLines);
    }

    public T FindClosestItemWithinBoxPosSize<T>(Vector2 pos, Vector2 size, Func<T, bool> condition = null, Chunk[,] chunks = default, bool debugLines = false) where T : Item
    {
        return FindClosestObjectWithinBoxPosSize(pos, size, chunk => chunk.GetItems<T>(), condition, chunks, debugLines);
    }

    public T FindClosestObjectWithinBoxPosSize<T>(Vector2 pos, Vector2 size, Func<Chunk, T[]> getObjList, Func<T, bool> condition = null, Chunk[,] chunks = default, bool debugLines = false) where T : MonoBehaviour
    {
        if (chunks == default)
            chunks = ChunksFromBoxPosSize(pos, size);

        var flattenedChunks = CollectionUtils.FlattenArray(chunks);

        T[][] jaggedArray = new T[flattenedChunks.Length][];

        for ( int i = 0; i < flattenedChunks.Length; i++ )
        {
            var chunk = flattenedChunks[i];

            if (chunk == null)
            {
                jaggedArray[i] = new T[0];
                continue;
            }

            if (debugLines)
                DrawChunk(chunk, UnityEngine.Color.green);

            var objects = getObjList(chunk);

            jaggedArray[i] = objects;
        }


        T[] allObjects = CollectionUtils.FlattenArray(jaggedArray);

        Func<T, bool> objCondition = obj => (condition == null || condition(obj)) && GeoUtils.TestBoxPosSize(pos, size, obj.transform.position, debugLines);

        return CollectionUtils.GetClosest(pos, allObjects.ToList(), obj => obj.transform.position, out _, objCondition, null, Mathf.Infinity, debugLines); //idc to redesign this rn but should be designed not to use arrays
        //return closestBehavior;
    }

    public List<T> GetItemsWithinChunkArray<T> (Chunk[,] chunks, Func<T, bool> condition = null) where T : Item
    {
        var output = new List<T>();

        foreach ( var chunk in chunks )
        {
            if (chunk == null)
                continue;

            var itemsOfType = chunk.GetItems<T>();

            foreach ( var item in itemsOfType )
            {
                if (condition == null || condition(item))
                {
                    output.Add(item);
                }

            }
        }

        return output;
    }

    public List<T> GetCharactersWithinChunkArray<T>(Chunk[,] chunks, Func<T, bool> condition = null) where T : Character
    {
        var output = new List<T>();

        foreach (var chunk in chunks)
        {
            if (chunk == null)
                continue;

            var itemsOfType = chunk.GetCharacters<T>();

            foreach (var character in itemsOfType)
            {
                if (condition == null || condition(character))
                {
                    output.Add(character);
                }

            }
        }

        return output;
    }

    public Vector2 GetPosRatio(Vector2 pos)
    {
        GetWorldBox(out var min, out _);

        return (pos - min) / worldSize;
    }
}

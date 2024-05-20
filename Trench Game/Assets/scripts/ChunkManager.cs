using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    public static ChunkManager manager;
    public float worldSize = 100;
    public int chunkArraySize = 5, mapSize = 10;
    public Chunk[,] chunks;
    public bool logOutOfBounds = false, drawChunkTest = false;

    private void Awake()
    {
        manager = this;
        InstantiateChunks();
    }

    private void Update()
    {
        if (drawChunkTest) Test();
    }

    public void Test ()
    {
        foreach (var chunk in chunks)
        {
            if (chunk != null) DrawChunk(chunk, Color.red);
        }

        var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        var selectedChunk = ChunkFromPos(mousePos, true);
        if (selectedChunk != null) DrawChunk(selectedChunk, Color.green);
    }

    public void InstantiateChunks ()
    {
        chunks = new Chunk[chunkArraySize, chunkArraySize];
    }

    public Vector2 GetChunkPos (Chunk chunk)
    {
        return AdressToPos(chunk.adress);
    }

    public Vector2 AdressToPos (Vector2Int adress)
    {
        var min = -new Vector2(worldSize, worldSize) / 2;
        var pos = ((Vector2)adress * worldSize / chunkArraySize) + min;
        return pos;
    }

    public Vector2Int PosToAdress (Vector2 pos)
    {
        var min = -new Vector2(worldSize, worldSize) / 2;
        var delta = pos - min;
        var adress = Vector2Int.FloorToInt(delta / (worldSize / chunkArraySize));
        return adress;
    }

    /// <summary>
    /// Will still return null if outside bounds!
    /// </summary>
    /// <param name="adress"></param>
    /// <param name="newIfNone"></param>
    /// <returns></returns>
    public Chunk ChunkFromAdress (Vector2Int adress, bool newIfNone = false)
    {
        DrawAdressBox(adress, Color.magenta);

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
                chunk = chunks[adress.x, adress.y] = new(adress,mapSize);
        }
        return chunk;
    }

    public Chunk ChunkFromPos (Vector2 pos, bool newIfNone = false)
    {
        var adress = PosToAdress(pos);
        return ChunkFromAdress(adress,newIfNone);
    }

    public void DrawChunk (Chunk chunk, Color color)
    {
        DrawAdressBox(chunk.adress,color);
    }

    public void DrawAdressBox (Vector2Int adress, Color color)
    {
        var min = AdressToPos(adress);
        var max = AdressToPos(adress + Vector2Int.one);
        GeoFuncs.DrawBox(min, max, color);
    }
}

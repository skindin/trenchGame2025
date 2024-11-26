using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
//using UnityEngine.Events;

public class Collider : MonoBehaviour
{
    //public static List<Collider> all = new();
    public Action<Bullet> onHit;
    public UnityEvent<bool> onChangeTrenchStatus;
    public Chunk[,] chunks;

    public float localSize = 1;

    public bool draw = false;
    public float WorldSize
    {
        get
        {
            return localSize * transform.lossyScale.x;
        }
    }

    public bool trenchStatus = true, drawTrenchStatus = false, drawTrenchTest = false;

    private void Update()
    {
        TestWithinTrench();
        UpdateChunk();
    }

    public void UpdateChunk()
    {

        if (chunks != null)
            foreach (var chunk in chunks)
            {
                if (chunk == null)
                    continue;

                chunk.colliders.Remove(this);
            }

        chunks = ChunkManager.Manager.ChunksFromBoxPosSize(transform.position, WorldSize / 2 * Vector2.one);

        foreach (var chunk in chunks)
        {
            if (chunk == null)
                continue;

            chunk.colliders.Add(this);
        }
    }

    public bool TestWithinTrench()
    {
        trenchStatus = !TrenchManager.Manager.TestCircleTouchesValue(transform.position, WorldSize / 2, false, drawTrenchTest);

        onChangeTrenchStatus.Invoke(trenchStatus);

        if (drawTrenchStatus)
        {
            GeoUtils.DrawCircle(transform.position, WorldSize / 2, trenchStatus ? Color.green : Color.red);
        }

        return trenchStatus;
    }

    public void HitCollider (Bullet bullet)
    {
        //transform.position = Random.insideUnitCircle * 5;
        onHit?.Invoke(bullet);
    }

    public void ToggleSafe (bool safe)
    {
        this.trenchStatus = !safe;
    }

    public void ResetCollider()
    {
        foreach (var chunk in chunks)
        {
            if (chunk!= null)
                chunk.colliders.Remove(this);
        }
        chunks = new Chunk[0,0];
        //hp = maxHp;
    }

    //private void Awake()
    //{
    //    all.Add(this);
    //}

    //private void OnDestroy()
    //{
    //    all.Remove(this);
    //}

    public Vector2 TestRay(Vector2 start, Vector2 end, bool debugLines = false)
    {
        var radius = WorldSize / 2;

        if (debugLines)
        {
            GeoUtils.DrawCircle(transform.position, WorldSize / 2, Color.green);
        }

        return GeoUtils.GetCircleLineIntersection(transform.position, radius, start, end);
    }

    private void OnDrawGizmos()
    {
        if (draw)
        {
            GeoUtils.DrawCircle(transform.position, WorldSize/2, Color.green);
        }
    }
}

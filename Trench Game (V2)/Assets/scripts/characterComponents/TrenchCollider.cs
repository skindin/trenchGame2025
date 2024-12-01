using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
//using UnityEngine.Events;

public class TrenchCollider : MonoBehaviour
{
    //public static List<Collider> all = new();
    public Action<Bullet> onHit;
    public UnityEvent<bool> onChangeTrenchStatus;
    public Chunk[,] chunks;

    public float localSize = 1, wallBuffer = .1f;

    public bool draw = false, trenchTestWithPoint = true;
    bool exitedTrenchThisFrame = false;

    public float WorldSize
    {
        get
        {
            return localSize * transform.lossyScale.x;
        }
    }

    public bool trenchStatus = true, drawTrenchStatus = false, freezeMovement = false;

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

                chunk.RemoveCollider(this);
            }

        chunks = ChunkManager.Manager.ChunksFromBoxPosSize(transform.position, WorldSize * Vector2.one,true);

        foreach (var chunk in chunks)
        {
            if (chunk == null)
                continue;

            chunk.AddCollider(this);
        }
    }

    public bool TestWithinTrench()
    {
        //if (trenchTestWithPoint)
        //{
        //    //trenchStatus =
        //    if (
        //    TrenchManager.Manager.TestPoint(transform.position)
        //        )
        //        //;
        //    trenchStatus = true;
        //}
        //else
        //{
        //    trenchStatus = !TrenchManager.Manager.TestCircleTouchesValue(transform.position, WorldSize / 2, false);
        //}

        if (exitRoutine != null || exitedTrenchThisFrame)
        {
            trenchStatus = exitedTrenchThisFrame = false;
        }
        else
            trenchStatus = !TrenchManager.Manager.TestCircleTouchesValue(transform.position, WorldSize / 2, false);

        //trenchStatus = !TrenchManager.Manager.TestCircleTouchesValue(transform.position, WorldSize / 2, false);

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
        exitedTrenchThisFrame = false;
        //hp = maxHp;

        StopAllCoroutines();
        exitRoutine = null;
    }

    //private void Awake()
    //{
    //    all.Add(this);
    //}

    //private void OnDestroy()
    //{
    //    all.Remove(this);
    //}

    public Vector2 MoveToPos (Vector2 pos)
    {
        if (trenchStatus)
        {

            pos = TrenchManager.Manager.StopAtValue(transform.position, pos, WorldSize/2 + wallBuffer, false);

            if (freezeMovement)
                return transform.position;// = pos;
            else
            {
                return transform.position = pos;
            }
        }

        return transform.position = pos;
    }

    Coroutine exitRoutine;

    public void ExitTrench (float duration)
    {
        if (exitRoutine != null)
            return;

        trenchStatus = false;
        exitedTrenchThisFrame = true;

        exitRoutine = StartCoroutine(ExitRoutine());

        IEnumerator ExitRoutine ()
        {
            yield return new WaitForSeconds(duration);

            exitRoutine = null;
        }
    }

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

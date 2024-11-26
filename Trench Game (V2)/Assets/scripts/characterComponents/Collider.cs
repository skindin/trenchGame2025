using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using UnityEngine.Events;

public class Collider : MonoBehaviour
{
    //public static List<Collider> all = new();
    public Action<Bullet> onHit;
    public UnityEvent<bool> onChangeTrenchStatus;

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

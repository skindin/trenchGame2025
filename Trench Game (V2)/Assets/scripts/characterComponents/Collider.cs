using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;

public class Collider : MonoBehaviour
{
    //public static List<Collider> all = new();
    public Action<Bullet> onHit;

    public float localSize = 1;

    public bool draw = false;
    public float WorldSize
    {
        get
        {
            return localSize * transform.lossyScale.x;
        }
    }

    public bool withinTrench = false, drawTrenchStatus = false, drawTrenchTest = false;

    private void Update()
    {
        TestWithinTrench();
    }

    public bool TestWithinTrench()
    {
        withinTrench = !TrenchManager.Manager.TestCircleTouchesValue(transform.position, WorldSize / 2, false, drawTrenchTest);

        if (drawTrenchStatus)
        {
            GeoUtils.DrawCircle(transform.position, WorldSize / 2, withinTrench ? Color.green : Color.red);
        }

        return withinTrench;
    }

    public void HitCollider (Bullet bullet)
    {
        //transform.position = Random.insideUnitCircle * 5;
        onHit?.Invoke(bullet);
    }

    public void ToggleSafe (bool safe)
    {
        this.withinTrench = !safe;
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

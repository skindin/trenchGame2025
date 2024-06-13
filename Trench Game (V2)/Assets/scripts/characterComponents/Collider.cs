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
    public float WorldSize
    {
        get
        {
            return localSize * transform.lossyScale.x;
        }
    }

    public bool vulnerable = true;

    public void HitCollider (Bullet bullet)
    {
        //transform.position = Random.insideUnitCircle * 5;
        onHit?.Invoke(bullet);
    }

    public void ToggleSafe (bool safe)
    {
        this.vulnerable = !safe;
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
}

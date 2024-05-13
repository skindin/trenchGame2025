using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public Character wielder;
    public float bulletSpeed, range, firingRate;
    float lastFireStamp = -Mathf.Infinity;

    public void Trigger (Vector2 direction)
    {
        if (Time.time > lastFireStamp + (1/firingRate))
        {
            Fire(direction);
            lastFireStamp = Time.time;
        }
    }

    public Bullet Fire (Vector2 direction)
    {
        return Bullet.manager.NewBullet(transform.position, direction.normalized * bulletSpeed, range, this);
    }
}

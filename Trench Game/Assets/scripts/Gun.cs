using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public Character wielder;
    public float bulletSpeed, range, firingRate;
    float lastFireStamp = 0;
    public int maxPerFrame = 5;
    public bool enabled = true;

    public void Trigger (Vector2 direction)
    {
        if (!enabled) return;

        var deltaTime = Time.time - lastFireStamp;

        var secsPerBullet = 1 / firingRate;

        if ((lastFireStamp == 0 && Time.time <= secsPerBullet) || deltaTime > secsPerBullet)
        {
            var bulletCount = Mathf.FloorToInt(firingRate * Time.deltaTime);

            bulletCount = Mathf.Max(bulletCount, 1);

            bulletCount = Mathf.Min(bulletCount, maxPerFrame);

            for (int i = 0; i < bulletCount; i++)
            {
                Fire(direction);
            }
            lastFireStamp = Time.time;
        }
    }

    public Bullet Fire (Vector2 direction)
    {
        return BulletManager.Manager.NewBullet(transform.position, direction.normalized * bulletSpeed, range, this);
    }
}

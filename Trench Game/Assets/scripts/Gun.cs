using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : Item
{
    public float bulletSpeed, range, firingRate, reloadTime = 2;
    float lastFireStamp = 0, reloadStartStamp = 0;
    public int maxPerFrame = 5, rounds = 10, maxRounds = 10;
    public Vector2 direction = Vector2.right, barrelPos;
    public bool enabled = true,
        reloading = false,
        drawBerrelPos = false;
    public AmoType amoType;

    public bool Trigger (Vector2 direction = default) //direction parameter to ensure the bullet fires in the right direction across all connections
    {
        if (!enabled || reloading || rounds <= 0) return false;

        if (direction != Vector2.zero) this.direction = direction;

        var fireDeltaTime = Time.time - lastFireStamp;

        var secsPerBullet = 1 / firingRate;

        if ((lastFireStamp == 0 && Time.time <= secsPerBullet) || fireDeltaTime > secsPerBullet)
        {
            var bulletCount = Mathf.FloorToInt(firingRate * Time.deltaTime);

            bulletCount = Mathf.Max(bulletCount, 1);

            bulletCount = Mathf.Min(bulletCount, maxPerFrame, rounds);

            for (int i = 0; i < bulletCount; i++)
            {
                Fire();
            }

            rounds -= bulletCount;

            lastFireStamp = Time.time;
        }

        return true;
    }

    //networking procedure
    //1. client runs fire logic, which can only spawn ghost bullets (bullets that can't actually damage anything, though they are destroyed when they hit something)
    //2. client sends rpc to server containing direction and time stamp
    //3. server runs trigger logic, spawning real bullets
    //4. server sends rpc to all other clients containing direction and time stamp
    //5. clients run trigger logic and spawn ghost bullets

    public int reloadAnimRots = 3;

    private void Update()
    {
        if (reloading)
        {
            var reloadClock = Mathf.Min(Time.time - reloadStartStamp, reloadTime);

            if (reloadClock >= reloadTime)
            {
                rounds = wielder.reserve.RemoveAmo(amoType, maxRounds - rounds);
                reloadStartStamp = Time.time;
                reloading = false;
            }
                
            var angle = ((reloadTime - reloadClock) / reloadTime) * reloadAnimRots * 360;
            transform.rotation = Quaternion.FromToRotation(Vector2.up, direction) * Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    public void StartReload ()
    {
        if (reloading) return;

        if (wielder.reserve.GetAmoAmount(amoType) > 0)
        {
            reloading = true;
            reloadStartStamp = Time.time;
        }
    }

    public void Aim (Vector2 direction)
    {
        this.direction = direction;

        if (reloading) return;

        var angle = Vector2.SignedAngle(Vector2.up, direction);
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    public Bullet Fire ()
    {
        return BulletManager.Manager.NewBullet(transform.position + transform.rotation * barrelPos, direction.normalized * bulletSpeed, range, this);
    }

    private void OnDrawGizmos()
    {
        if (drawBerrelPos)    
            GeoFuncs.MarkPoint(transform.position + transform.rotation * barrelPos,.2f,Color.blue);
    }
}

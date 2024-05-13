using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletManager : MonoBehaviour
{
    public readonly List<Bullet> activeBullets = new(), pooledBullets = new();
    public int maxPooled = 100, activeCount, pooledCount, total;
    public bool debugLines = false;

    private void Awake()
    {
        Bullet.manager = this;
    }

    public Bullet NewBullet (Vector2 pos, Vector2 velocity, float range, Gun source)
    {
        Bullet newBullet;

        if (pooledBullets.Count > 0)
        {
            newBullet = pooledBullets[0];
            pooledBullets.Remove(newBullet);
            pooledCount = pooledBullets.Count;
        }
        else
        {
            newBullet = new();
        }

        newBullet.pos = newBullet.startPos = pos;
        newBullet.velocity = velocity;
        newBullet.range = range;
        newBullet.source = source;

        activeBullets.Add(newBullet);
        activeCount = activeBullets.Count;

        total = activeCount + pooledCount;

        return newBullet;
    }

    public void DestroyBullet (Bullet bullet)
    {
        activeBullets.Remove(bullet);
        if (pooledBullets.Count < maxPooled)
        {
            pooledBullets.Add(bullet);
            pooledCount = pooledBullets.Count;
        }

        activeCount = activeBullets.Count;

        total = activeCount + pooledCount;
    }

    private void Update()
    {
        UpdateBullets(Time.deltaTime);
    }

    void UpdateBullets(float seconds)
    {
        for (int i = 0; i < activeBullets.Count; i++)
        {
            var bullet = activeBullets[i];
            var delta = bullet.pos - bullet.startPos;

            var nextDelta = delta + (bullet.velocity * seconds);
            bool destroy = nextDelta.magnitude >= bullet.range;
            var nextPos = Vector2.ClampMagnitude(nextDelta, bullet.range) + bullet.startPos;
            Collider closestCollider = null;

            foreach (var collider in Collider.all)
            {
                if (bullet.source.wielder.collider == collider) continue;

                var point = GeoFuncs.GetCircleLineIntersection(collider.transform.position, collider.size / 2, bullet.pos, nextPos);
                if (point == Vector2.positiveInfinity) continue;
                var pointDist = (point - bullet.pos).magnitude;
                var nextDist = (nextPos - bullet.pos).magnitude;
                if (nextDist > pointDist)
                {
                    nextPos = point;
                    closestCollider = collider;
                }
            }

            if (debugLines)
            {
                GeoFuncs.DrawCircle(bullet.startPos, bullet.range, Color.red,10);
                Debug.DrawLine(bullet.pos, nextPos, Color.red);
            }

            bullet.pos = nextPos;

            if (closestCollider != null)
            {
                closestCollider.OnBulletHit(bullet);
                destroy = true;
                //Debug.Log($"Hit {closestCollider.gameObject.name}");
            }

            if (destroy)
            {
                DestroyBullet(bullet);
                i--;
            }
        }
    }
}

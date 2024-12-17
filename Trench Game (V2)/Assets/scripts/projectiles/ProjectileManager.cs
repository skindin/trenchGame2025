using System.Collections;
using System.Collections.Generic;
//using System.Drawing;
//using System.Net;
using UnityEngine;

public class ProjectileManager : MonoBehaviour
{
    static ProjectileManager manager;

    public static ProjectileManager Manager
    {
        get
        {
            if (manager == null)
            {
                manager = FindObjectOfType<ProjectileManager>();
                //if (manager == null)
                //{
                //    GameObject go = new GameObject("Bullet");
                //    manager = go.AddComponent<BulletManager>();
                //    DontDestroyOnLoad(go);
                //}
            }
            return manager;
        }
    }

    public readonly List<Bullet> activeBullets = new();//, pooledBullets = new();
    //public int maxPooled = 100, activeCount, pooledCount, total;
    public bool debugLines = false, markTrenchBullets = false;
    public ObjectPool<Bullet> bulletPool;
    public float destroyDelay = .2f, updateScale = 1.0f;

    private void Awake()
    {
        bulletPool = new(
            newFunc: () => new Bullet(),
            resetAction: null,
            disableAction: null,
            destroyAction: null
            );
    }

    public Bullet NewBullet (Vector2 startPos, Vector2 velocity, float range, float damage, Character source, bool withinTrench = false)
    {
        var newBullet = bulletPool.GetFromPool();

        newBullet.pos = newBullet.startPos = startPos;
        //newBullet.pos = startPos + velocity.normalized * range * progress;
        newBullet.velocity = velocity;
        newBullet.range = range;
        newBullet.source = source;
        newBullet.hit = false;
        newBullet.shooterLife = source.life;
        newBullet.withinTrench = newBullet.startedWithinTrench = withinTrench;

        activeBullets.Add(newBullet);
        //activeCount = activeBullets.Count;

        //total = activeCount + pooledCount;

        newBullet.damage = damage;

        //newBullet.withinTrench = source.wielder.detector.withinTrench;
        newBullet.destroy = false;

        return newBullet;
    }

    public void DestroyBullet (Bullet bullet)
    {
        activeBullets.Remove(bullet);

        bulletPool.AddToPool(bullet);
        //if (pooledBullets.Count < maxPooled)
        //{
        //    pooledBullets.Add(bullet);
        //    pooledCount = pooledBullets.Count;
        //}

        //activeCount = activeBullets.Count;

        //total = activeCount + pooledCount;
    }

    private void Update()
    {
        UpdateBullets(Time.deltaTime * updateScale);
    }

    List<Chunk> chunkList = new();

    public void UpdateBullet(Bullet bullet, float seconds, out bool wasDestroyed)
    {
        //var bullet = activeBullets[i];

        if ((bullet.pos - bullet.startPos).magnitude >= bullet.range * destroyDelay)
        {
            DestroyBullet(bullet);
            wasDestroyed = true;
            return;
        }
        else
        {
            wasDestroyed = false;
        }

        //var delta = bullet.pos - bullet.startPos;

        //var nextDelta = delta + (bullet.velocity * seconds);
        //bullet.destroy = (bullet.pos - bullet.startPos).magnitude >= bullet.range * destructRangeFactor;
        var nextDirection = bullet.velocity * seconds;
        var nextPos = bullet.pos + nextDirection;
        //var closestPoint = nextPos;
        TrenchCollider closestCollider = null;
        float shortestDistance = nextDirection.magnitude;
        var ogDistance = shortestDistance;
        Vector2 closestPoint = nextPos;

        //float furthestTrenchDist = nextDelta.magnitude;
        //bool leavingTrench = false;
        //if (bullet.withinTrench)
        //{
        //    var edgePoint = Trench.manager.FindTrenchEdgeFromInside(bullet.pos, nextPos, debugLines);
        //    if (edgePoint != nextPos)
        //    {
        //        furthestTrenchDist = (bullet.pos - edgePoint).magnitude;
        //        leavingTrench = true;
        //    }
        //}

        if (!bullet.hit)
        {
            var chunks = ChunkManager.Manager.ChunksFromLine(bullet.pos, nextPos, false, debugLines);

            //float wallDist = shortestDistance;

            if (bullet.withinTrench && markTrenchBullets)
            {
                GeoUtils.MarkPoint(bullet.pos, .5f, UnityEngine.Color.red);
            }

            Vector2 trenchExit = nextPos;

            var exitedTrench = bullet.withinTrench && TrenchManager.Manager.TestRayHitsValue(bullet.pos, nextPos, false, out trenchExit);

            var wallDist = (trenchExit - bullet.pos).magnitude;

            HashSet<TrenchCollider> processedColliders = new();

            foreach (var chunk in chunks)
            {
                foreach (var collider in chunk.colliders)
                {
                    if (processedColliders.Contains(collider)) continue;
                    if (!collider.gameObject.activeInHierarchy) continue;
                    if (bullet.source && bullet.source.trenchCollider == collider) continue;
                    if (!bullet.withinTrench && collider.trenchStatus) continue;

                    var point = collider.TestRay(bullet.pos, nextPos,debugLines);

                    processedColliders.Add(collider);

                    if (point.x == Mathf.Infinity) continue;

                    var pointDist = (point - bullet.pos).magnitude;

                    if (pointDist < shortestDistance && (!collider.trenchStatus || bullet.withinTrench || (exitedTrench && wallDist < shortestDistance)))
                    {
                        closestCollider = collider;
                        bullet.range = (point - bullet.startPos).magnitude;
                        shortestDistance = pointDist;
                        closestPoint = point;
                    }
                }
            }

            bool exceededRange = ((closestPoint) - bullet.startPos).magnitude > bullet.range;

            if (exceededRange)
            {
                var fullPath = bullet.velocity.normalized * bullet.range;

                var exceedPoint = bullet.startPos + fullPath;

                var distToRange = (exceedPoint - bullet.pos).magnitude;

                if (distToRange < shortestDistance)
                {
                    closestCollider = null;
                    shortestDistance = distToRange;
                }
            }

            if (shortestDistance < ogDistance)
            {
                bullet.hit = true;
                bullet.range = Mathf.Min((closestPoint - bullet.startPos).magnitude, bullet.range);
            }

            if (exitedTrench)
            {
                bullet.trenchExit = trenchExit;
                bullet.withinTrench = false;
                //float distTraveled = ((bullet.pos + bullet.velocity.normalized * wallDist) - bullet.startPos).magnitude;
                //NewBullet(bullet.startPos, bullet.velocity, distTraveled, 0, bullet.source, true);
            }
        }

        if (!bullet.hit && !ChunkManager.Manager.IsPointInWorld(nextPos))
            bullet.hit = true;

        //if (bullet.withinTrench && leavingTrench)
        //{
        //    bullet.withinTrench = false;
        //}

        if (debugLines)
        {
            GeoUtils.DrawCircle(bullet.startPos, bullet.range, UnityEngine.Color.red, 10);
            Debug.DrawLine(bullet.pos, nextPos, UnityEngine.Color.red);
        }

        //bullet.lastPos = bullet.pos;
        bullet.pos = nextPos;


        if (!NetworkManager.IsServer)
            return;

        if (closestCollider != null) //dont want to damage characters here...
        {
            closestCollider.HitCollider(bullet);
            //bullet.destroy = true;
            //Debug.Log($"Hit {closestCollider.gameObject.name}");
        }

    }

    void UpdateBullets(float seconds)
    {
        for (int i = 0; i < activeBullets.Count; i++)
        {
            var bullet = activeBullets[i];

            UpdateBullet(bullet, seconds, out var wasDestroyed);

            if (wasDestroyed)
                i--;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
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
    public bool debugLines = false;
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

    public Bullet NewBullet (Vector2 pos, Vector2 velocity, float range, float damage, Character source)
    {
        var newBullet = bulletPool.GetFromPool();

        newBullet.pos = newBullet.startPos = pos;
        newBullet.velocity = velocity;
        newBullet.range = range;
        newBullet.source = source;
        newBullet.hit = false;

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

    void UpdateBullets(float seconds)
    {
        for (int i = 0; i < activeBullets.Count; i++)
        {
            var bullet = activeBullets[i];

            if ((bullet.pos - bullet.startPos).magnitude >= bullet.range * destroyDelay)
            {
                DestroyBullet(bullet);
                i--;
                continue;
            }

            //var delta = bullet.pos - bullet.startPos;

            //var nextDelta = delta + (bullet.velocity * seconds);
            //bullet.destroy = (bullet.pos - bullet.startPos).magnitude >= bullet.range * destructRangeFactor;
            var nextPos = bullet.pos + bullet.velocity * seconds;
            //var closestPoint = nextPos;
            Collider closestCollider = null;

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

                var chunks = ChunkManager.Manager.ChunksFromLine(bullet.pos, nextPos, chunkList, false, debugLines);

                foreach (var chunk in chunks)
                {
                    foreach (var collider in chunk.colliders)
                    {
                        if (bullet.source && bullet.source.collider == collider) continue;
                        if (!collider.vulnerable) continue;

                        var radius = collider.WorldSize / 2;
                        var point = GeoUtils.GetCircleLineIntersection(collider.transform.position, radius, bullet.pos, nextPos);
                        if (point.x == Mathf.Infinity) continue;
                        var pointDist = (point - bullet.startPos).magnitude;
                        //if (bullet.withinTrench && !collider.vulnerable)
                        //{
                        //    if (pointDist > furthestTrenchDist) continue;
                        //}
                        var startToNext = bullet.startPos - nextPos;
                        var clampedNext = Vector2.ClampMagnitude(startToNext, bullet.range);
                        //var nextDist = (nextPos - bullet.pos).magnitude;

                        if (clampedNext.magnitude > pointDist)
                        {
                            bullet.hit = true;
                            //nextPos = point;
                            bullet.range = (point - bullet.startPos).magnitude;
                            closestCollider = collider;
                            //closestPoint = point;
                        }
                    }
                }
            }

            //if (bullet.withinTrench && leavingTrench)
            //{
            //    bullet.withinTrench = false;
            //}

            if (debugLines)
            {
                GeoUtils.DrawCircle(bullet.startPos, bullet.range, Color.red,10);
                Debug.DrawLine(bullet.pos, nextPos, Color.red);
            }

            //bullet.lastPos = bullet.pos;
            bullet.pos = nextPos;

            if (closestCollider != null)
            {
                closestCollider.HitCollider(bullet);
                //bullet.destroy = true;
                //Debug.Log($"Hit {closestCollider.gameObject.name}");
            }
        }
    }
}

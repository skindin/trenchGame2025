using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletManager : MonoBehaviour
{
    static BulletManager manager;

    public static BulletManager Manager
    {
        get
        {
            if (manager == null)
            {
                manager = FindObjectOfType<BulletManager>();
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

    public readonly List<Bullet> activeBullets = new(), pooledBullets = new();
    public int maxPooled = 100, activeCount, pooledCount, total;
    public bool debugLines = false;
    public Mesh bulletMesh;
    public float meshScale = .1f;

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

        //newBullet.withinTrench = source.wielder.detector.withinTrench;
        newBullet.destroy = false;

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

    List<Chunk> chunkList = new();

    void UpdateBullets(float seconds)
    {
        for (int i = 0; i < activeBullets.Count; i++)
        {
            var bullet = activeBullets[i];

            if (bullet.destroy)
            {
                DestroyBullet(bullet);
                i--;
                continue;
            }

            var delta = bullet.pos - bullet.startPos;

            var nextDelta = delta + (bullet.velocity * seconds);
            bullet.destroy = nextDelta.magnitude >= bullet.range;
            var nextPos = Vector2.ClampMagnitude(nextDelta, bullet.range) + bullet.startPos;
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

            var chunks = ChunkManager.Manager.ChunksFromLine(bullet.pos, nextPos, chunkList, false, debugLines);

            foreach (var chunk in chunks)
            {
                foreach (var collider in chunk.colliders)
                {
                    if (bullet.source.wielder.collider == collider) continue;
                    //if (!collider.vulnerable && !bullet.withinTrench) continue;

                    var radius = collider.size * collider.transform.lossyScale.x / 2;
                    var point = GeoFuncs.GetCircleLineIntersection(collider.transform.position, radius, nextPos, bullet.pos);
                    if (point.x == Mathf.Infinity) continue;
                    var pointDist = (point - bullet.pos).magnitude;
                    //if (bullet.withinTrench && !collider.vulnerable)
                    //{
                    //    if (pointDist > furthestTrenchDist) continue;
                    //}
                    var nextDist = (nextPos - bullet.pos).magnitude;

                    if (nextDist > pointDist)
                    {
                        nextPos = point;
                        closestCollider = collider;
                    }
                }
            }

            //if (bullet.withinTrench && leavingTrench)
            //{
            //    bullet.withinTrench = false;
            //}

            if (debugLines)
            {
                GeoFuncs.DrawCircle(bullet.startPos, bullet.range, Color.red,10);
                Debug.DrawLine(bullet.pos, nextPos, Color.red);
            }

            bullet.pos = nextPos;

            if (closestCollider != null)
            {
                closestCollider.BulletHit(bullet);
                bullet.destroy = true;
                //Debug.Log($"Hit {closestCollider.gameObject.name}");
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    static SpawnManager manager;

    //public List<Gun> gunPrefabs = new();
    //public List<Amo> amoPrefabs = new();

    public static SpawnManager Manager
    {
        get
        {
            if (manager == null)
            {
                manager = FindObjectOfType<SpawnManager>();
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

    public float spawnRadius = 50;



    public void Relocate (Transform transform)
    {
        transform.position = Random.insideUnitCircle * spawnRadius;
    }
}

//public class SpawnGroup
//{
//    public List<GameObject> active = new();
//    public ObjectPool<GameObject> pool;
//}
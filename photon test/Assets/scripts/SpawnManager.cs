using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager manager;
    public float spawnRadius = 50;

    private void Awake()
    {
        manager = this;
    }

    public void Relocate (Transform transform)
    {
        transform.position = Random.insideUnitCircle * spawnRadius;
    }
}

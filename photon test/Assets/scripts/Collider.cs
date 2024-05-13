using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collider : MonoBehaviour
{
    public static List<Collider> all = new();

    public float size = 1;

    public void OnBulletHit (Bullet bullet)
    {
        transform.position = Random.insideUnitCircle * 5;
    }

    private void Awake()
    {
        all.Add(this);
    }

    private void OnDestroy()
    {
        all.Remove(this);
    }
}

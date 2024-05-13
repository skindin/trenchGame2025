using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Collider : MonoBehaviour
{
    public static List<Collider> all = new();
    public UnityEvent<Bullet> onBulletHit = new();
    public float size = 1;
    public bool vulnerable = true;

    public void BulletHit (Bullet bullet)
    {
        //transform.position = Random.insideUnitCircle * 5;
        onBulletHit.Invoke(bullet);
    }

    public void ToggleSafe (bool safe)
    {
        this.vulnerable = !safe;
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

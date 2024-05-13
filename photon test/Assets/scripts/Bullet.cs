using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet
{
    public static BulletManager manager;
    public Vector2 pos, startPos, velocity;
    public float range;
    public Gun source;
}

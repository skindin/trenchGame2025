using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile
{
    public Vector2 pos, startPos, velocity;
    public float range;
    public Character source;
    public bool
        //withinTrench, 
        destroy = false;
    public float damage = 1;
}

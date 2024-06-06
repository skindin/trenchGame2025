using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class GunModel : ItemModel
{
    public float bulletSpeed, range, firingRate, reloadTime = 2;
    public int maxPerFrame = 5, maxRounds = 10, reloadAnimRots = 3;
    public AmoType amoType;
    public bool autoFire = true, autoReload = false;
}

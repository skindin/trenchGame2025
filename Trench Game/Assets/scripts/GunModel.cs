using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class GunModel : ScriptableObject
{
    public float bulletSpeed, range, firingRate, reloadTime = 2;
    public int maxPerFrame = 5, maxRounds = 10;
    public AmoType amoType;
}

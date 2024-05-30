using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class AmoModel : ItemModel
{
    public AmoType type;
    public int maxRounds;
    public bool combineWithinRadius = true;
    public float combineRadius = .5f;
}

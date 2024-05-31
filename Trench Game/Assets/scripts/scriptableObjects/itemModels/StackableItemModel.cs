using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class StackableItemModel : ItemModel
{
    public bool limitAmount = false;
    public int maxAmount = 10;
    public float combineRadius = .5f;

    //the max amount is still used when not limited, if the item is set to stackFull
}

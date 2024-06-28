using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Weapon : Item
{
    public abstract void Attack(Vector2 direction);

    public abstract void Aim(Vector2 direction);
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Weapon : Item , IDirectionalAction
{
    public abstract void DirectionalAction(Vector2 direction);

    public abstract void Aim(Vector2 direction);
}

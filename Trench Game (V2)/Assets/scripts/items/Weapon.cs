using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public abstract class Weapon : Item , IDirectionalAction
{
    public UnityEvent<Vector2> onAim;

    public abstract void DirectionalAction(Vector2 direction);

    public abstract void Aim(Vector2 direction);
}

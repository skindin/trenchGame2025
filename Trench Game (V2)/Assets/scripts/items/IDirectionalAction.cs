using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDirectionalAction
{
    public abstract void DirectionalAction(Vector2 direction);

    public abstract void Aim(Vector2 direction);
}

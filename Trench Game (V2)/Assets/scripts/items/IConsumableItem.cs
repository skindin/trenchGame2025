using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IConsumableItem
{
    public void Consume(Character character);

    public void Pickup (Character character, out bool wasPickedUp, out bool wasDestroyed)
    {
        wasDestroyed = TestDestroy(character);

        wasPickedUp = TestPickup(character);

        if (wasPickedUp) Consume(character);
    }

    public bool TestPickup (Character character);

    public virtual bool TestDestroy (Character character)
    {
        return true;
    }
}

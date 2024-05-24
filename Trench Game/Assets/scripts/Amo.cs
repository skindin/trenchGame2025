using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Amo : Item
{
    public int rounds;
    public AmoType amoType;

    public override void Pickup (Character character)
    {
        //base.Pickup(character);

        if (character.reserve)
        {
            rounds = character.reserve.AddAmo(amoType, rounds);

            if (rounds <= 0) //shouldn't be less then, but just a percaution
                DestroyItem();
        }
    }

    public override void ItemUpdate()
    {
        base.ItemUpdate();


    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Amo : Item
{
    public int rounds;
    public bool maxRounds = false;

    AmoModel amoModel;
    public AmoModel AmoModel
    {
        get
        {
            if (amoModel == null)
            {
                amoModel = (AmoModel)model;
            }
            return amoModel;
        }
    }

    public override void ItemAwake()
    {
        base.ItemAwake();

        if (maxRounds)
            rounds = AmoModel.maxRounds;
        else
            rounds = Mathf.Clamp(rounds, 0, AmoModel.maxRounds);
    }

    public override void Pickup (Character character)
    {
        //base.Pickup(character);

        if (character.reserve)
        {
            rounds = character.reserve.AddAmo(AmoModel.type, rounds);

            if (rounds <= 0) //shouldn't be less then, but just a percaution
                DestroyItem();
        }
    }

    public override void ItemUpdate()
    {
        base.ItemUpdate();


    }

    public override string[] GetInfo()
    {
        var itemInfo = base.GetInfo();

        var rounds = "x" + this.rounds;
        var type = AmoModel.type.name;

        var amoInfo = new string[] { rounds , type};

        var result = itemInfo.Concat(amoInfo).ToArray();

        return result;
    }
}

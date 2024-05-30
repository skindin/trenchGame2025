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
        {
            rounds = AmoModel.maxRounds;
            return;
        }

        if (AmoModel.combineWithinRadius) Combine();
        rounds = Mathf.Clamp(rounds, 0, AmoModel.maxRounds);
    }

    public void Combine () //idk where to run this
    {
        var min = transform.position - Vector3.one * AmoModel.combineRadius;
        var max = transform.position + Vector3.one * AmoModel.combineRadius;

        var chunks = ChunkManager.Manager.ChunksFromBox(min, max);

        foreach (var chunk in chunks)
        {
            if (chunk == null) continue;

            foreach (var item in chunk.items)
            {
                if (item.GetType() != typeof(Amo)) continue;

                var amo = (Amo)item;

                if (amo.AmoModel.type != AmoModel.type) continue;

                var dist = Vector2.Distance(amo.transform.position, transform.position);
                var minDist = Mathf.Min(AmoModel.combineRadius, amo.AmoModel.combineRadius);
                if (dist > minDist) continue;

                Amo biggestMax;
                Amo smallest;

                if (amo.AmoModel.maxRounds > AmoModel.maxRounds)
                {
                    biggestMax = amo;
                    smallest = this;
                }
                else
                {
                    biggestMax = this;
                    smallest = amo;
                }

                if (amo.rounds + rounds > biggestMax.AmoModel.maxRounds) continue;

                biggestMax.rounds += smallest.rounds;

                smallest.DestroyItem();

                if (smallest == this) return;
            }
        }
    }

    public override bool Pickup (Character character) //WOULD BE COOL IF AMO COULD COMBINE WHEN WITHIN A GIVEN PROXIMITY
    {
        //base.Pickup(character);


        if (character.reserve)
        {
            rounds = character.reserve.AddAmo(AmoModel.type, rounds);

            if (rounds <= 0)//shouldn't be less then, but just a percaution
            {
                DestroyItem();
                return true;
            }
        }

        return false;
    }

    //public override void ItemUpdate()
    //{
    //    base.ItemUpdate();


    //}

    public override string GetInfo(string separator = " ")
    {
        var itemInfo = base.GetInfo();

        var rounds = this.rounds + " rounds";
        //var type = AmoModel.type.name;

        var array = new string[] { itemInfo, rounds };//, type};

        //var array = itemInfo.Concat(amoInfo).ToArray();

        return string.Join(separator, array);
    }
}

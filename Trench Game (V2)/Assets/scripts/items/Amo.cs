using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Amo : StackableItem
{
    AmoModel amoModel;
    public AmoModel AmoModel
    {
        get
        {
            if (amoModel == null)
            {
                amoModel = (AmoModel)itemModel;
            }
            return amoModel;
        }
    }

    //public override void ItemAwake()
    //{
    //    base.ItemAwake();
    //}


    /// <summary>
    /// Picked up means was removed from ground
    /// </summary>
    /// <param name="character"></param>
    /// <param name="wasPickedup"></param>
    /// <param name="wasDestroyed"></param>
    public override void Pickup (Character character, out bool wasPickedup, out bool wasDestroyed)
    {
        //base.Pickup(character, out wasPickedup, out wasDestroyed);

        wasPickedup = wasDestroyed = false;

        if (character.reserve)
        {
            //var prevAmount = amount;
            amount = character.reserve.AddAmo(AmoModel.type, amount);

            //if (prevAmount != amount)
            //    wasPickedup = true;

            if (amount <= 0)//shouldn't be less then, but just a percaution
            {
                DestroyItem();
                wasPickedup = wasDestroyed = true;
            }
            //else
            //{

            //    CombineAll(); //man idk why this not working
            //}
        }
    }

    //public override DataDict<object> PublicData //probably don't need to add the type, because it's already in the name...
    //{
    //    get
    //    {
    //        var data = base.PublicData;
    //        DataDict<object>.Combine(ref data, Naming.amoType, AmoModel.name);
    //        return data;
    //    }
    //}

    //public override void ItemUpdate()
    //{
    //    base.ItemUpdate();


    //}

    //public override string GetInfo(string separator = " ")
    //{
    //    var itemInfo = base.GetInfo();

    //    var rounds = this.amount + " rounds";
    //    //var type = AmoModel.type.name;

    //    var array = new string[] { itemInfo, rounds };//, type};

    //    //var array = itemInfo.Concat(amoInfo).ToArray();

    //    return string.Join(separator, array);
    //}
}

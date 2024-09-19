using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Ammo : StackableItem
{
    AmmoModel cachedAmmoModel;
    public AmmoModel AmoModel
    {
        get
        {
            if (cachedAmmoModel == null)
            {
                cachedAmmoModel = (AmmoModel)itemModel;
            }
            return cachedAmmoModel;
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
    public override void Pickup (Character character, out bool wasPickedup, out bool wasDestroyed, bool sync = false)
    {
        //base.Pickup(character, out wasPickedup, out wasDestroyed);

        wasPickedup = wasDestroyed = false;

        if (character.reserve)
        {


            //if (prevAmount != amount)
            //    wasPickedup = true;

            if (NetworkManager.IsServer)//shouldn't be less then, but just a percaution
            {
                //DestroyItem();

                amount = character.reserve.AddAmo(AmoModel.type, amount);

                if (amount <= 0)
                {
                    wasPickedup = wasDestroyed = true;
                    DestroyItem();
                }
                //amount = newAmt;
            }

            if (sync)
            {
                //var prevAmt = amount;

                //var prevAmount = amount;

                var pool = character.reserve.GetPool(AmoModel.type);

                NetworkManager.Manager.RequestAmo(this, pool.maxRounds - pool.rounds);

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

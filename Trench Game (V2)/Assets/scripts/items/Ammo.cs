using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Ammo : StackableItem
{
    public AmmoType type;

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

            var pool = character.reserve.GetPool(type);
            var amtTaken = Mathf.Min(amount, pool.maxRounds - pool.rounds);
            amount = pool.AddAmo(amount);

            if (NetworkManager.IsServer)
            {
                //DestroyItem();

                if (amount <= 0)
                {
                    wasPickedup = wasDestroyed = true;
                    DestroyItem();
                }
                else
                {
                    var ammoData = new StackData { Amount = amount }; //shouldn't be here but mehhhhh
                    var itemData = new ItemData { ItemId = id, Stack = ammoData };
                    NetworkManager.Manager.server.UpdateItemData(itemData);
                }
                //amount = newAmt;
            }
            else if (sync)
            {
                //var prevAmt = amount;

                //var prevAmount = amount;

                //Debug.Log($"ammo amount is now {amount}");

                if (amtTaken > 0)
                {
                    NetworkManager.Manager.RequestAmo(this, amtTaken);
                }
                
                if (amount <= 0)
                {
                    gameObject.SetActive(false);
                }

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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Amo : StackableItem
{
    AmoModel cachedAmoModel;
    public AmoModel AmoModel
    {
        get
        {
            if (cachedAmoModel == null)
            {
                cachedAmoModel = (AmoModel)itemModel;
            }
            return cachedAmoModel;
        }
    }

    //public override void ItemAwake()
    //{
    //    base.ItemAwake();
    //}

    //readonly List<Character> dispatchedTo = new();

    /// <summary>
    /// Picked up means accessed
    /// </summary>
    /// <param name="character"></param>
    /// <param name="wasDispatched"></param>
    /// <param name="wasDestroyed"></param>
    public override Coroutine Pickup (Character character, out bool wasDispatched, out bool wasDestroyed, out bool inCharInventory
        //, bool shrinkToZero = true
        )
    {
        inCharInventory = false;

        if (amount == 0 || character.reserve.GetAmoMax(AmoModel.type) == 0
            //|| dispatchedTo.Contains(character)
            )
        {
            wasDispatched = wasDestroyed = false;
            return null;
        }

        var spaceInReserve = character.reserve.GetAmoSpace(AmoModel.type);

        wasDestroyed = spaceInReserve >= amount;

        wasDispatched = spaceInReserve > 0;

        //if (character.reserve)
        //{
        //    //amount = character.reserve.AddAmo(AmoModel.type, amount);

        //    //if (amount <= 0)
        //    //{
        //    //    DestroyItem();
        //    //    wasPickedup = wasDestroyed = true;
        //    //}
        //}

        return StartCoroutine(PickupAmo());

        IEnumerator PickupAmo ()
        {
            //var amount = this.amount;

            if (spaceInReserve >= amount)
            {
                var charLife = character.life;

                var pickupRoutine = base.Pickup(character, out _, out _, out _
                    //, shrinkToZero
                    );

                if (pickupRoutine != null)
                    yield return pickupRoutine;

                if (charLife != character.life)
                    yield break;


                //var spaceLeft = character.reserve.GetAmoSpace(AmoModel.type);

                var surplus = character.reserve.AddAmo(AmoModel.type, amount);

                if (true || surplus <= 0) //tbh seeng them drop just looks bad
                {
                    yield return DestroySelf();
                }
                else
                {
                    Drop(transform.position);
                }
                //if (Chunk != null)
            }
            else if (spaceInReserve > 0)
            {
                var newAmo = SpawnManager.Manager.GetAmo(AmoModel.type, spaceInReserve, transform.position);
                //newAmo.gameObject.name = "created by splitting";
                this.amount -= spaceInReserve;
                //spaceInReserve = 0;
                //dispatchedTo.Add(character);
                yield return newAmo.Pickup(character, out _ , out _, out _);
                //dispatchedTo.Remove(character);
            }
            //else
            //{
            //    dispatchedTo.Remove(character);
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

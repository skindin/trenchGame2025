using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StackableItem : Item
{
    StackableItemModel cachedModel;

    public StackableItemModel StackableModel
    {
        get
        {
            if (cachedModel == null && model is StackableItemModel stackableModel)
            {
                cachedModel = stackableModel;
            }

            return cachedModel;
        }

        set
        {
            cachedModel = value;
        }
    }

    public int amount = 1;
    public bool startFull = false;

    public override void ResetItem()
    {
        base.ResetItem();

        //amount = 1; //idk why the fuck this was here
    }

    public override void ItemAwake()
    {
        base.ItemAwake();

        if (startFull) 
            amount = StackableModel.maxAmount;
        else 
            amount = Mathf.Clamp(amount, 0, StackableModel.maxAmount);
    }

    //public override void ItemStart()
    //{
    //    base.ItemStart();

    //    Combine();
    //}

    public override void Pickup(Character character, out bool wasPickedUp, out bool wasDestroyed)
    {
        wasPickedUp = true;
        CombineWithItems(character.inventory.items, out wasDestroyed, false);
        if (!wasDestroyed) base.Pickup(character, out wasPickedUp, out _);
    }

    public override void Drop() //just remember to run Drop() every time you spawn a new item!
    {
        CombineAll();

        base.Drop();
    }

    public bool CombineAll() //idk where to run this
    {
        var min = transform.position - Vector3.one * StackableModel.combineRadius;
        var max = transform.position + Vector3.one * StackableModel.combineRadius;

        var chunks = ChunkManager.Manager.ChunksFromBoxMinMax(min, max);

        foreach (var chunk in chunks)
        {
            if (chunk == null) continue;

            if (CombineWithItems(chunk.items, out _)) return true;
        }

        return false;
    }

    public bool CombineWithItems(List<Item> items, out bool wasDestroyed, bool testDist = true)
    {
        wasDestroyed = false;

        foreach (var item in items)
        {
            if (item == this) continue;

            if (item is not StackableItem stackItem) continue;

            if (StackableModel != stackItem.StackableModel) continue;

            if (testDist)
            {
                var dist = Vector2.Distance(item.transform.position, transform.position);
                if (dist > StackableModel.combineRadius) continue;
            }

            int addend;
            if (StackableModel.limitAmount)
            { 
                var spaceLeft = StackableModel.maxAmount - stackItem.amount;
                addend = Mathf.Min(amount, spaceLeft);
            }
            else
            {
                addend = amount; 
            }

            stackItem.amount += addend;
            amount -= addend;

            if (amount == 0)
            {
                DestroyItem();
                wasDestroyed = true;
            }

            return true;
        }

        return false;
    }

    public override string InfoString(string separator = " ")
    {
        var itemInfo = base.InfoString(separator);

        var stackInfo = $"x{amount}";

        return itemInfo + separator + stackInfo;
    }


    public override DataDict<object> PrivateData
    {
        get
        {
            var data = base.PrivateData;

            DataDict<object>.Combine(ref data, Naming.amount, amount);

            return data;
        }
    }
}

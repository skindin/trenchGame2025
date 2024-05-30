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

    public override void ItemAwake()
    {
        base.ItemAwake();

        if (startFull) amount = StackableModel.maxAmount;
        else amount = Mathf.Clamp(amount, 0, StackableModel.maxAmount);
    }

    //public override void ItemStart()
    //{
    //    base.ItemStart();

    //    Combine();
    //}

    public override void Drop() //just remember to run Drop() every time you spawn a new item!
    {
        base.Drop();

        Combine();
    }

    public bool Combine() //idk where to run this
    {
        var min = transform.position - Vector3.one * StackableModel.combineRadius;
        var max = transform.position + Vector3.one * StackableModel.combineRadius;

        var chunks = ChunkManager.Manager.ChunksFromBox(min, max);

        foreach (var chunk in chunks)
        {
            if (chunk == null) continue;

            foreach (var item in chunk.items)
            {
                if (!(item is StackableItem stackItem)) continue;

                if (StackableModel != stackItem.StackableModel) continue;

                var dist = Vector2.Distance(item.transform.position, transform.position);
                if (dist > StackableModel.combineRadius) continue;

                var spaceLeft = StackableModel.maxAmount - stackItem.amount;

                var addend = Mathf.Min(amount, spaceLeft);

                stackItem.amount += addend;
                amount -= addend;

                if (amount == 0) DestroyItem();

                return true;
            }
        }

        return false;
    }

    public override string GetInfo(string separator = " ")
    {
        var itemInfo = base.GetInfo(separator);

        var stackInfo = $"x{amount}";

        return itemInfo + separator + stackInfo;
    }
}

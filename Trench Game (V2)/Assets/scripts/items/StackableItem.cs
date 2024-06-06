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
        base.Pickup(character, out wasPickedUp, out wasDestroyed);
        CombineWithItems(character.inventory.items);
    }

    public override void Drop() //just remember to run Drop() every time you spawn a new item!
    {
        base.Drop();

        CombineAll();
    }

    public bool CombineAll() //idk where to run this
    {
        var min = transform.position - Vector3.one * StackableModel.combineRadius;
        var max = transform.position + Vector3.one * StackableModel.combineRadius;

        var chunks = ChunkManager.Manager.ChunksFromBox(min, max);

        foreach (var chunk in chunks)
        {
            if (chunk == null) continue;

            if (CombineWithItems(chunk.items)) return true;
        }

        return false;
    }

    public bool CombineWithItems(List<Item> items)
    {
        foreach (var item in items)
        {
            if (item is not StackableItem stackItem) continue;

            if (StackableModel != stackItem.StackableModel) continue;

            var dist = Vector2.Distance(item.transform.position, transform.position);
            if (dist > StackableModel.combineRadius) continue;

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

            if (amount == 0) DestroyItem();

            return true;
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

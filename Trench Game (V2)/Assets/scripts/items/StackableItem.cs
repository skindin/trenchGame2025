using System.Collections.Generic;
using UnityEngine;

public class StackableItem : Item
{
    public bool limitAmount = false;
    public int maxAmount = 10;
    public float combineRadius = .5f;

    public int amount = 1;
    public bool startFull = false;

    public override void ResetItem()
    {
        base.ResetItem();
        // Removed unnecessary initialization
    }

    public override void ItemAwake()
    {
        base.ItemAwake();

        if (startFull)
            amount = maxAmount;
        else
            amount = Mathf.Clamp(amount, 0, maxAmount);
    }

    public override void Pickup(Character character, out bool wasPickedUp, out bool wasDestroyed, bool sync) //had errors because items is an array now
    {
        //wasPickedUp = false;
        //CombineWithItems(character.inventory.items, out wasDestroyed, false);
        //if (!wasDestroyed)
            base.Pickup(character, out wasPickedUp, out wasDestroyed, sync);
    }

    public override void DropLogic(Vector2 pos, out bool destroyedSelf)
    {
        base.DropLogic(pos, out _);
        //if (NetworkManager.IsServer) //just gonna disable this for now because I might just completely remove it anyway and it's a pain in the a
        //    CombineAll(out destroyedSelf);
        //else
        //{
            destroyedSelf = false;
        //}
    }

    //public bool CombineAll(out bool destroyedSelf)
    //{
    //    var min = transform.position - Vector3.one * combineRadius;
    //    var max = transform.position + Vector3.one * combineRadius;

    //    var chunks = ChunkManager.Manager.ChunksFromBoxMinMax(min, max);

    //    foreach (var chunk in chunks)
    //    {
    //        if (chunk == null) continue;

    //        if (CombineWithItems(chunk.items, out destroyedSelf)) 
    //            return true;
    //    }

    //    destroyedSelf = false;
    //    return false;
    //}

    //public bool CombineWithItems(List<Item> items, out bool destroyedSelf, bool testDist = true)
    //{
    //    destroyedSelf = false;

    //    foreach (var item in items)
    //    {
    //        if (item == this) continue;

    //        if (item is not StackableItem stackItem) continue;

    //        if (prefabId != stackItem.prefabId) continue;

    //        if (testDist)
    //        {
    //            var dist = Vector2.Distance(item.transform.position, transform.position);
    //            if (dist > combineRadius) 
    //                continue;
    //        }

    //        int addend;
    //        if (limitAmount)
    //        {
    //            var spaceLeft = maxAmount - stackItem.amount;
    //            addend = Mathf.Min(amount, spaceLeft);
    //        }
    //        else
    //        {
    //            addend = amount;
    //        }

    //        stackItem.amount += addend;
    //        amount -= addend;

    //        if (amount == 0)
    //        {
    //            //Debug.Log($"Destroying item: {this.name}");
    //            DestroyItem();
    //            destroyedSelf = true;
    //        }

    //        return true;
    //    }

    //    return false;
    //}

    //void RemoveItemReferences()
    //{
    //    var chunk = ChunkManager.Manager.GetChunkContainingItem(this);
    //    if (chunk != null)
    //    {
    //        chunk.items.Remove(this);
    //    }

    //    ItemManager.Manager.RemoveItem(this);
    //}

    public override string InfoString(string separator = " ")
    {
        var itemInfo = base.InfoString(separator);
        var stackInfo = $"x{amount}";
        return itemInfo + separator + stackInfo;
    }
}

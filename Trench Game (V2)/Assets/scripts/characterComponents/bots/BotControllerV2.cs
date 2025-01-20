using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BotControllerV2 : MonoBehaviour
{
    public Character character;
    public Vector2 visionBox;
    public bool debugLines = false;
    public Transform targetObject;
    public Vector2? targetPos;
    //public float targetFollowDistance;
    //public Dictionary<int,BotCharacterProfile> profiles = new ();

    public Chunk[,] chunks;

    public Vector2 FindBulletPathToPos (Vector2 pos)
    {
        return transform.position; 
    }

    public void FollowTargetObject (float distance)
    {
        var delta = transform.position - targetObject.position;

        if (delta.magnitude <= distance)
        {
            return;
        }

        targetPos = delta.normalized * distance + targetObject.position;

        character.MoveToPos(targetPos.Value);
        character.LookInDirection(-delta);
    }

    public List<T> GetBestItems<T> 
        (int? limit = null, bool includeInventory = true, bool considerDistance = true, bool onePerPrefab = true)
        where T : Item
    {
        var visible = GetItems<T>();

        if (includeInventory)
        {
            foreach (var item in character.inventory.itemSlots)
            {
                if (item)
                {
                    visible.Insert(0, (T)item);
                }
            }
        }

        CollectionUtils.SortHighestToLowest(visible,
            considerDistance ?
            item => ScoringManager.Manager.RankItem(item) + ScoringManager.Manager.GetItemDistanceScore(transform.position, item) :
            ScoringManager.Manager.RankItem
            );

        if (onePerPrefab)
        {
            visible = CollectionUtils.ListFirstOfEveryValue(visible, item => item.prefabId, limit);
        }
        else if (limit.HasValue)
            visible.Take(limit.Value); //haven't ever tested this take function

        return visible;
    }

    public T PickupItemsInOrder<T> (IEnumerable<T> items, int? pickupLimit = null, bool sortByDistance = false) where T : Item
    {
        int pickedUp = 0;

        if (sortByDistance)
        {
            CollectionUtils.SortLowestToHighest(items.ToList(), item => Vector2.Distance(item.transform.position, transform.position));
        }

        foreach (var item in items) //gotta implement pickup limit, and implement sorting by distance
        {
            if (item.wielder == character)
            {
                //continue;
            }
            else if (Vector2.Distance(item.transform.position,transform.position) <= character.inventory.activePickupRad)
            {
                if (!pickupLimit.HasValue || pickedUp < pickupLimit.Value)
                {

                    character.LookInDirection(item.transform.position - transform.position);
                    character.inventory.PickupItem(item, item.transform.position, true);

                    if (targetObject == item.transform)
                    {
                        targetObject = null; //if we were targeting this item, stop targeting it
                    }

                    pickedUp++;
                }
            }
            else //not in inventory, and not in pickup range
            {
                targetObject = item.transform;
                return item;
            }
        }

        return null;
    }

    private void Update()
    {
        UpdateChunks();

        PickupItemsInOrder(GetBestItems<Item>(2));

        if (targetObject)
        {
            FollowTargetObject(0);
        }
    }

    public void UpdateChunks ()
    {
        chunks = ChunkManager.Manager.ChunksFromBoxPosSize(transform.position,visionBox);
    }

    //public Item PickupClosestItem<T>(Func<T, bool> condition = null) where T : Item
    //{
    //    var closestItem = CollectionUtils.GetClosest(
    //        transform.position,
    //        character.inventory.withinRadius.OfType<T>().ToList(),
    //        item => item.transform.position,
    //        out _,
    //        condition
    //    );

    //    if (closestItem)
    //    {
    //        var dropPos = UnityEngine.Random.insideUnitCircle * character.inventory.selectionRad + (Vector2)closestItem.transform.position;
    //        character.inventory.PickupItem(closestItem,dropPos,true);
    //    }

    //    return closestItem;
    //}

    //public T FindClosestCharacter<T>(Func<T,bool> condition = null) where T : Character
    //{
    //    return ChunkManager.Manager.FindClosestCharacterWithinBoxPosSize(transform.position, visionBox, condition, chunks, debugLines);
    //}

    //public T FindClosestItem<T>(Func<T, bool> condition = null) where T : Item
    //{
    //    return ChunkManager.Manager.FindClosestItemWithinBoxPosSize(transform.position, visionBox, condition, chunks, debugLines);
    //}

    public List<T> GetItems<T> (Func<T, bool> condition = null) where T : Item
    {
        return ChunkManager.Manager.GetItemsWithinChunkArray<T>(chunks, 
            item => 
            (condition == null || condition(item)) && GeoUtils.TestBoxPosSize(transform.position,visionBox,item.transform.position));
    }

    public List<T> GetCharacters<T>(Func<T, bool> condition = null) where T : Character
    {
        return ChunkManager.Manager.GetCharactersWithinChunkArray<T>(chunks, 
            character => 
            (condition == null || condition(character)) && GeoUtils.TestBoxPosSize(transform.position, visionBox, character.transform.position));
    }

    public void ResetBot ()
    {
        //nothing to reset yet
    }

    private void OnDrawGizmos()
    {
        if (debugLines)
        {
            GeoUtils.DrawBoxPosSize(transform.position, visionBox, UnityEngine.Color.magenta);
        }
    }
}

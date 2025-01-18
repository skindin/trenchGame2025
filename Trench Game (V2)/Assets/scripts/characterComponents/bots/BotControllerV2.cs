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
    public Vector2 targetPos;
    //public float targetFollowDistance;
    public Dictionary<int,BotCharacterProfile> profiles = new ();

    public Chunk[,] chunks;

    public void ExampleKillThisCharacter(Character target)
    {
        var sortedVisibleItems = CollectionUtils.SortToListDict(GetItems<Item>(), item => item.GetType());
        //this is a sorted list of all visible items

        if (sortedVisibleItems.TryGetValue(typeof(Gun), out var list))
        {
            SortItemListByStats(list);

            targetObject = list[0].transform;

            
        }

    }

    public void SortItemListByStats<T> (List<T> items) where T : Item
    {
        var type = typeof(T);

        if (type == typeof(Gun))
        {
            CollectionUtils.GetHighest(items, 
                gun => ItemManager.Manager.ranking.RankGun(gun as Gun) + BotManager.Manager.GetDistanceScore(this,gun), out _);
        }
    }

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

        character.MoveToPos(targetPos);
        character.LookInDirection(-delta);
    }

    public void GetBestGuns (int countLimit)
    {
        var visibleGuns = GetItems<Gun>();

        if (visibleGuns.Count == 0)
            return;

        foreach (var item in character.inventory.itemSlots)
        {
            if (item is Gun gun)
                visibleGuns.Insert(0,gun);//insert instead of add, to favor guns already in inventory
        }

        CollectionUtils.SortHighestToLowest(visibleGuns, gun => ItemManager.Manager.ranking.RankGun(gun));

        var sortedByPrefabId = CollectionUtils.SortToListDict(visibleGuns, gun => gun.prefabId);

        Gun targetGun = null;

        var i = 0;

        countLimit = Mathf.Min(character.inventory.itemSlots.Length, countLimit);

        foreach (var pair in sortedByPrefabId)
        {
            if (i >= countLimit)
            {
                break;
            }

            var bestOfGroup = pair.Value[0];

            if (bestOfGroup == targetGun)
            {
                targetGun = null;
            }

            if (character.inventory.GetSlotWithItem(item => item && item == bestOfGroup) != null)
            {
                continue;
            }

            if (Vector2.Distance(transform.position, bestOfGroup.transform.position) <= character.inventory.activePickupRad)
            {
                //if (character.inventory.GetSlotWithItem(item => item && item.prefabId == pair.Key) != null)
                //{
                //    character.inventory.DropActiveItem(bestOfGroup.transform.position);
                //} //tbh this part isn't necessary

                character.LookInDirection(bestOfGroup.transform.position - transform.position);
                character.inventory.PickupItem(bestOfGroup, bestOfGroup.transform.position, true);

                continue;
            }

            targetGun = bestOfGroup; //breh it just juggles the best guns around

            i++;
        }

        targetObject = targetGun ? targetGun.transform : null;
    }

    public void GetConsumables (int countLimit)
    {
    }

    public List<T> GetBestItems<T> (int? limit = null, bool includeInventory = true, bool considerDistance = true, bool onePerPrefab = true) where T : Item
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

        Func<T, float> getValue = item => ItemManager.Manager.ranking.RankItem(item);

        CollectionUtils.SortHighestToLowest(visible,
            considerDistance ?
            item => getValue(item) + BotManager.Manager.GetDistanceScore(this, item) :
            getValue
            );

        if (onePerPrefab)
        {
            visible = CollectionUtils.ListFirstOfEveryValue(visible, item => item.prefabId, limit);
        }

        if (limit.HasValue)
            visible.Take(limit.Value); //haven't ever tested this take function

        return visible; //gonna call it a day
    }

    private void Update()
    {
        UpdateChunks();

        GetBestGuns(2);

        if (targetObject)
        {
            FollowTargetObject(0);
        }
    }

    public void UpdateChunks ()
    {
        chunks = ChunkManager.Manager.ChunksFromBoxPosSize(transform.position,visionBox);
    }

    public Item PickupClosestItem<T>(Func<T, bool> condition = null) where T : Item
    {
        var closestItem = CollectionUtils.GetClosest(
            transform.position,
            character.inventory.withinRadius.OfType<T>().ToList(),
            item => item.transform.position,
            out _,
            condition
        );

        if (closestItem)
        {
            var dropPos = UnityEngine.Random.insideUnitCircle * character.inventory.selectionRad + (Vector2)closestItem.transform.position;
            character.inventory.PickupItem(closestItem,dropPos,true);
        }

        return closestItem;
    }

    public T FindClosestCharacter<T>(Func<T,bool> condition = null) where T : Character
    {
        return ChunkManager.Manager.FindClosestCharacterWithinBoxPosSize(transform.position, visionBox, condition, chunks, debugLines);
    }

    public T FindClosestItem<T>(Func<T, bool> condition = null) where T : Item
    {
        return ChunkManager.Manager.FindClosestItemWithinBoxPosSize(transform.position, visionBox, condition, chunks, debugLines);
    }

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

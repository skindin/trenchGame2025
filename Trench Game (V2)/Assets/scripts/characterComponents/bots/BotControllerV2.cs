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
            CollectionUtils.GetHighest(items, gun => ItemManager.Manager.ranking.RankGun(gun as Gun), out _);
        }
    }

    public Vector2 FindBulletPathToPos (Vector2 pos)
    {
        return transform.position; 
    }

    public void FollowTargetObject (float distance)
    {
        var delta = transform.position - targetObject.position;

        targetPos = delta.normalized * distance + targetObject.position;

        character.MoveToPos(targetPos);
    }

    public void TestGetBestGuns (int countLimit)
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

        Gun bestGun = null;

        var i = 0;

        countLimit = Mathf.Min(character.inventory.itemSlots.Length, countLimit);

        foreach (var gun in visibleGuns)
        {            
            if (i >= countLimit)
            {
                break;
            }

            if (character.inventory.SetSlotToItem(item => item == gun))
            {
                i++;
                continue;
            }

            if (Vector2.Distance(transform.position, gun.transform.position) <= character.inventory.activePickupRad)
            {
                character.inventory.PickupItem(gun, gun.transform.position, true);

                i++;
                continue;
            }

            bestGun = gun; //next i should make it sort the guns into prefab id, and then grab the best from each group
            break;
        }

        if (bestGun) //if there are no guns
        {
            targetObject = bestGun.transform;
            FollowTargetObject(0);
        }
    }

    private void Update()
    {
        UpdateChunks();

        TestGetBestGuns(3);
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
        return ChunkManager.Manager.GetItemsWithinChunkArray(chunks, condition);
    }

    public List<T> GetCharacters<T>(Func<T, bool> condition = null) where T : Character
    {
        return ChunkManager.Manager.GetCharactersWithinChunkArray(chunks, condition);
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

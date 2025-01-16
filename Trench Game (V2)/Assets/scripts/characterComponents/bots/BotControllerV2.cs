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

        var pos = delta.normalized * distance + targetObject.position;

        character.MoveToPos(pos);
    }

    public void TestGetBestGun ()
    {
        var bestGun = CollectionUtils.GetHighest(GetItems<Gun>(), gun => ItemManager.Manager.ranking.RankGun(gun), out _);
        if (!bestGun || //if it didn't find a gun
            bestGun.transform != targetObject || //or we are already moving targeting this gun
            character.inventory.SetSlotToItem(item => item == bestGun)) //or we are already holding this gun
        {
            return; //nothing to do
        }

        if (Vector2.Distance(transform.position, bestGun.transform.position) <= character.inventory.activePickupRad)
        {
            character.inventory.PickupItem(bestGun, transform.position, true);
        }
        else
        {
            FollowTargetObject(0);
        }
    }

    private void Update()
    {
        UpdateChunks();

        TestGetBestGun();
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

    private void OnDrawGizmos()
    {
        if (debugLines)
        {
            GeoUtils.DrawBoxPosSize(transform.position, visionBox, UnityEngine.Color.magenta);
        }
    }
}

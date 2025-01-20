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
    public float offenseMemory = 5;
    //public float targetFollowDistance;
    public Dictionary<int, BotCharacterProfile> profiles = new();

    public Chunk[,] chunks;

    public Vector2 FindBulletPathToPos (Vector2 pos)
    {
        return transform.position; 
    }

    public void FollowTargetObject (float distance, bool maintainDist = false)
    {
        var delta = transform.position - targetObject.position;

        if (!maintainDist && delta.magnitude <= distance)
        {
            return;
        }

        targetPos = delta.normalized * distance + targetObject.position;

        character.MoveToPos(targetPos.Value);
        character.LookInDirection(-delta);
    }

    public List<T> GetBestItems<T> 
        (Func<T,bool> condition, int? limit = null, bool includeInventory = true, bool considerDistance = true, bool onePerPrefab = true)
        where T : Item
    {
        var visible = GetVisibleItems<T>(condition);

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
            item => ScoringManager.Manager.GetItemScore(item) + ScoringManager.Manager.GetItemDistanceScore(transform.position, item) :
            ScoringManager.Manager.GetItemScore
            );

        //this should probably have a way of privatizing properties of items outside pickup range

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

    public List<T> GetStrongestCharacters<T> (Func<T,bool> condition, bool considerDistance = true) where T : Character
    {
        var visibleCharacters = GetVisibleCharacters(condition);

        Func<T, float> getValue = considerDistance ?
            character => ScoringManager.Manager.GetCharacterScore(character) + ScoringManager.Manager.GetCharacterDistanceScore(transform.position, character) :
            ScoringManager.Manager.GetCharacterScore;

        CollectionUtils.SortHighestToLowest(visibleCharacters, getValue);

        return visibleCharacters;
    }

    private void Update()
    {
        UpdateChunks();

        var strongestCharacter = CollectionUtils.GetHighest(
            GetVisibleCharacters<Character>(character => character.clan != this.character.clan),
            character => ScoringManager.Manager.GetCharacterScore(character) +
            ScoringManager.Manager.GetCharacterDistanceScore(transform.position, character),
            out _);

        if (strongestCharacter && character.inventory.ActiveWeapon)
        {
            Attack(strongestCharacter);
        }
        else
        {
            PickupItemsInOrder(GetBestItems<Weapon>(null, 2));

            if (targetObject)
                FollowTargetObject(0);
        }
    }

    public void Attack (Character victim)
    {
        var weapon = character.inventory.ActiveWeapon;

        if (weapon is Gun gun)
        {
            if (Vector2.Distance(victim.transform.position, transform.position) <= gun.range)
            {            
                gun.DirectionalAction(victim.transform.position - transform.position);
            }

            targetObject = gun.transform;

            FollowTargetObject(gun.range, true);
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

    public List<T> GetVisibleItems<T> (Func<T, bool> condition = null) where T : Item
    {
        return ChunkManager.Manager.GetItemsWithinChunkArray<T>(chunks, 
            item => 
            (condition == null || condition(item)) && GeoUtils.TestBoxPosSize(transform.position,visionBox,item.transform.position));
    }

    public List<T> GetVisibleCharacters<T>(Func<T, bool> condition = null) where T : Character
    {
        return ChunkManager.Manager.GetCharactersWithinChunkArray<T>(chunks, 
            character =>
            character != this.character && 
            (condition == null || condition(character)) && 
            GeoUtils.TestBoxPosSize(transform.position, visionBox, character.transform.position));
    }

    public void OnDamaged (float hp, Character aggressor, int life)
    {
        var profile = GetProfile(aggressor);

        profile.lastDamagedTime = Time.time;
        profile.totalDamageDealt += hp;
    }

    public BotCharacterProfile GetProfile (Character character)
    {
        if (profiles.TryGetValue(character.id, out var profile))
        {
            return profile;
        }
        else
        {
            var newProfile = new BotCharacterProfile();
            profiles.Add(character.id,newProfile);
            return newProfile;
        }
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

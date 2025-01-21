using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BotControllerV2 : MonoBehaviour
{
    public Character character;
    public Vector2 visionBox;
    public bool debugLines = false;
    //public Character targetCharacter;
    //public Transform targetObject;
    public Vector2? targetPos;
    public float offenseMemory = 5;
    //public float targetFollowDistance;
    public Dictionary<int, BotCharacterProfile> profiles = new();

    public Chunk[,] chunks;

    public Vector2 FindBulletPathToPos (Vector2 pos)
    {
        return transform.position; 
    }

    public bool TestVisionBox (Vector2 pos)
    {
        return GeoUtils.TestBoxPosSize(transform.position, visionBox, pos);
    }

    public void MoveToTargetPos (float distance, bool maintainDist = false)
    {
        var delta = (Vector2)transform.position - targetPos.Value;

        if (!maintainDist && delta.magnitude <= distance)
        {
            return;
        }

        var nextPos = delta.normalized * distance + targetPos.Value;

        character.MoveToPos(nextPos);
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

    public T PickupItemsInOrder<T> (IEnumerable<T> items, int? pickupLimit = null, bool sortByDistance = false, bool selectBest = true) where T : Item
    {
        int pickedUp = 0;

        if (sortByDistance)
        {
            CollectionUtils.SortLowestToHighest(items.ToList(), item => Vector2.Distance(item.transform.position, transform.position));
        }

        CollectionUtils.GetLowest<Item>(character.inventory.itemSlots, ScoringManager.Manager.GetItemScore, out var lowestValueSlot);

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

                    if (!character.inventory.itemSlots.Contains(null))
                    {
                        character.inventory.CurrentSlot = lowestValueSlot;
                    }

                    character.inventory.PickupItem(item, item.transform.position, true);


                    pickedUp++;
                }
            }
            else //not in inventory, and not in pickup range
            {
                targetPos = item.transform.position;
                return item;
            }
        }

        return null;
    }

    public List<T> GetStrongestCharacters<T> (Func<T,bool> condition, bool considerDistance = true) where T : Character
    {
        var visibleCharacters = GetVisibleCharacters(condition);

        Func<T, float> getValue = considerDistance ?
            character => ScoringManager.Manager.GetCharacterScore(character) +
            ScoringManager.Manager.GetCharacterDistanceScore(transform.position, character.transform.position) :
            ScoringManager.Manager.GetCharacterScore;

        CollectionUtils.SortHighestToLowest(visibleCharacters, getValue);

        return visibleCharacters;
    }

    public void UpdateProfiles<T> (IEnumerable<T> list) where T : Character
    {
        foreach (T character in list)
        {
            var profile = GetProfile(character);
            profile.lastKnownPos = character.transform.position;
            profile.lastSeenTime = Time.time;
            profile.lastKnownPower = ScoringManager.Manager.GetCharacterScore(character);
        }
    }

    private void Update()
    {
        UpdateChunks();

        var visibleEnemies = GetVisibleCharacters<Character>(character => character.clan != this.character.clan);

        UpdateProfiles(visibleEnemies);

        var strongestCharacter = CollectionUtils.GetHighest(
            visibleEnemies,
            character => ScoringManager.Manager.GetCharacterScore(character) +
            ScoringManager.Manager.GetCharacterDistanceScore(transform.position, character.transform.position),
            out _);

        if (strongestCharacter && character.inventory.ActiveWeapon)
        {
            //select best weapon

            CollectionUtils.GetHighest(character.inventory.itemSlots, ScoringManager.Manager.GetItemScore, out var slot);

            character.inventory.CurrentSlot = slot;

            Attack(strongestCharacter);
        }
        else
        {
            targetPos = null;

            PickupItemsInOrder(GetBestItems<Weapon>(null, 2));

            if (targetPos.HasValue)
                MoveToTargetPos(0);
        }

        if (character.inventory.ActiveWeapon)
        {
            if (!strongestCharacter)
                //if we don't see any characters
            {
                var profileList = CollectionUtils.DictionaryToList(profiles);

                CollectionUtils.SortHighestToLowest(profileList, profile => profile.lastKnownPower);

                foreach (var profile in profileList) //move to the most powerful in memory
                {
                    //if (TestVisionBox(profile.lastKnownPos) && profile.lastSeenTime != Time.time)
                    //{
                    //    continue; //if bot sees the last known pos, but hasn't seen it for a while
                    //}

                    targetPos = profile.lastKnownPos; //otherwise, move the last place we saw them at

                    MoveToTargetPos(0); //idk if this worked yet but it's too late gn

                    break;
                }
            }

            {
                if (character.inventory.ActiveWeapon is Gun gun)
                {
                    if (!strongestCharacter || gun.rounds <= 0) //if there is no threat, or we are out of ammo
                    {
                        gun.StartReload(); //reload
                    }
                }
            }
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

            targetPos = victim.transform.position;
            //targetCharacter = victim;

            MoveToTargetPos(gun.range, true);
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
            newProfile.character = character; //mehmehmeh ill make a constructor later
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
            if (targetPos.HasValue)
                GeoUtils.DrawCircle(targetPos.Value, 1, Color.red);
        }
    }
}

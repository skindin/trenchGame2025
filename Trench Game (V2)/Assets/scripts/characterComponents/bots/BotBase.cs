using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;


namespace BotBrains
{
    [RequireComponent(typeof(Character))]
    public abstract class BotBase : MonoBehaviour
    {
        public Character character;
        public Vector2 visionBox;
        public bool debugLines = false;
        //public Character targetCharacter;
        //public Transform targetObject;
        //public Vector2? targetPos;
        //public float offenseMemory = 5;
        //public float targetFollowDistance;
        public float reactionRate = 5;
        public int actionMax = 1;
        public readonly Dictionary<int, Dictionary<int, CharacterProfile>> clanProfiles;
        public readonly Dictionary<int, ItemProfile> itemProfiles;
        public Vector2? attackTarget, moveTarget;

        public Chunk[,] chunks;

        //private void Awake()
        //{
            //character = GetComponent<Character>();
        //}

        private void OnEnable()
        {
            //UpdateChunks(); //now implemented in reaction routine
            StartReactionRoutine();
        }

        public Vector2 FindBulletPathToPos(Vector2 pos)
        {
            return transform.position;
        }

        public bool TestVisionBox(Vector2 pos)
        {
            return GeoUtils.TestBoxPosSize(transform.position, visionBox, pos);
        }

        //public void MoveToPos(Vector2 pos, float distance, bool maintainDist = false)
        //{
        //    var delta = (Vector2)transform.position - pos;

        //    if (!maintainDist && delta.magnitude <= distance)
        //    {
        //        return;
        //    }

        //    var nextPos = delta.normalized * distance + pos;

        //    nextPos = GeoUtils.ClampToBoxPosSize(nextPos, pos, visionBox);

        //    character.MoveToPos(nextPos);
        //    character.LookInDirection(-delta);
        //}

        public List<T> GetBestItems<T>
            (Func<T, bool> condition, int? limit = null, bool includeInventory = true, bool considerDistance = true, bool onePerPrefab = true)
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

        //T PickupItemsInOrder<T>(IEnumerable<T> items, int? pickupLimit = null, bool sortByDistance = false, bool selectBest = true) where T : Item
        //{
        //    int pickedUp = 0;

        //    if (sortByDistance)
        //    {
        //        CollectionUtils.SortLowestToHighest(items.ToList(), item => Vector2.Distance(item.transform.position, transform.position));
        //    }

        //    CollectionUtils.GetLowest<Item>(character.inventory.itemSlots, ScoringManager.Manager.GetItemScore, out var lowestValueSlot);

        //    foreach (var item in items) //gotta implement pickup limit, and implement sorting by distance
        //    {
        //        if (item.wielder == character)
        //        {
        //            //continue;
        //        }
        //        else if (Vector2.Distance(item.transform.position, transform.position) <= character.inventory.activePickupRad)
        //        {
        //            if (!pickupLimit.HasValue || pickedUp < pickupLimit.Value)
        //            {
        //                character.LookInDirection(item.transform.position - transform.position);

        //                if (!character.inventory.itemSlots.Contains(null))
        //                {
        //                    character.inventory.CurrentSlot = lowestValueSlot;
        //                }

        //                character.inventory.PickupItem(item, item.transform.position, true);

        //                if (debugLines)
        //                    GeoUtils.MarkPoint(item.transform.position, 1, Color.red);

        //                pickedUp++;
        //            }
        //        }
        //        else //not in inventory, and not in pickup range
        //        {
        //            targetPos = item.transform.position;
        //            return item;
        //        }
        //    }

        //    return null;
        //}

        //List<T> GetStrongestCharacters<T>(Func<T, bool> condition, bool considerDistance = true) where T : Character
        //{
        //    var visibleCharacters = GetVisibleCharacters(condition);

        //    Func<T, float> getValue = considerDistance ?
        //        character => ScoringManager.Manager.GetCharacterScore(character) +
        //        ScoringManager.Manager.GetCharacterDistanceScore(transform.position, character.transform.position) :
        //        ScoringManager.Manager.GetCharacterScore;

        //    CollectionUtils.SortHighestToLowest(visibleCharacters, getValue);

        //    return visibleCharacters;
        //}

        CharacterProfile GetCharacterProfile(Character character)
        {
            if (!clanProfiles.TryGetValue(character.clan.id, out var clanProfile))
            {
                clanProfile = new();
                clanProfiles.Add(character.clan.id, clanProfile);
            }

            if (clanProfile.TryGetValue(character.id, out var profile))
            {
                return profile;
            }
            else
            {
                var newProfile = new CharacterProfile();
                newProfile.id = character.id;
                clanProfile.Add(character.id, newProfile);

                return newProfile;
            }
        }

        void UpdateCharacterProfiles<T>(IEnumerable<T> visibleCharacters) where T : Character //this actually only updates enemy profiles
        {
            foreach (T character in visibleCharacters)
            {
                var profile = GetCharacterProfile(character);
                profile.SetCurrentVelocity(character.transform.position, 1 / reactionRate);

                profile.lastKnownPos = character.transform.position;
                profile.lastSeenTime = Time.time;
                profile.isVisible = true;
                profile.lastKnownPower = ScoringManager.Manager.GetCharacterScore(character);
            }
        }

        TProfile GetItemProfile<TProfile,TItem> (TItem item) where TProfile : ItemProfile, new() where TItem : Item
        { //having generic item type isn't necessary right now, but might as well
            if (itemProfiles.TryGetValue(item.id, out var baseProfile))
            {
                if (baseProfile is not TProfile profile)
                {
                    Debug.LogError("wrong profile type");
                    return null;
                }

                return profile;
            }
            else
            {
                var newProfile = new TProfile();
                newProfile.prefabId = item.prefabId;
                itemProfiles.Add(item.id, newProfile);
                return newProfile;
            }
        }

        public void UpdateItemProfile<T> (T item) where T : Item
        {
            var profile = GetItemProfile<ItemProfile,Item>(item);

            profile.isVisible = true;
            profile.lastTimeSeen = Time.time;
            profile.lastKnownPower = ScoringManager.Manager.GetItemScore(item);

            if (item.wielder)
            {
                profile.lastKnownWielder = item.wielder.id;
                profile.lastKnownPos = null;
            }
            else
            {
                profile.lastKnownPos = item.transform.position;
                profile.lastKnownWielder = null;

                if (Vector2.Distance(transform.position, item.transform.position) <= character.inventory.activePickupRad)
                {
                    if (profile is GunProfile gunProfile && item is Gun gun)
                    {
                        gunProfile.lastKnownRounds = gun.rounds;
                    }
                    else if (profile is AmmoProfile ammoProfile && item is Ammo ammo)
                    {
                        ammoProfile.amount = ammo.amount;
                    }
                }
                else
                {
                    profile.ResetPrivateProperties();
                }
            }
        }

        void UpdateItemProfiles<T>(IEnumerable<T> visibleItems) where T : Item
        {
            foreach (var item in visibleItems)
            {
                if (item != null)
                    UpdateItemProfile(item);
            }
        }

        private void Update()
        {
            if (attackTarget.HasValue)
            {
                //attack target position
            }

            if (moveTarget.HasValue)
            {
                //move to targetPos
            }
        }

        void StartReactionRoutine()
        {
            StartCoroutine(ReactionRoutine());

            IEnumerator ReactionRoutine()
            {
                while (true)
                {
                    UpdateChunks();

                    foreach (var itemPair in itemProfiles)
                    {
                        itemPair.Value.isVisible = false;
                    }

                    foreach (var clanPair in clanProfiles)
                    {
                        foreach (var charPair in clanPair.Value)
                        {
                            charPair.Value.isVisible = false;
                        }
                    }

                    var visibleCharacters = GetVisibleCharacters<Character>();
                    UpdateCharacterProfiles(visibleCharacters);
                    UpdateItemProfiles(GetVisibleItems<Item>());

                    var heldItems = CollectionUtils.GetPropertyCollection(visibleCharacters, character => character.inventory.ActiveItem);

                    UpdateItemProfiles(heldItems);

                    Reaction();

                    yield return new WaitForSeconds(1 / reactionRate);
                }
            }
        }

        public abstract void Reaction();

        //void AttackTargetEnemy(Vector2 direction)
        //{
        //    if (character.inventory.ActiveWeapon is Gun gun)
        //    {
        //        gun.DirectionalAction(direction);
        //    }
        //}

        void UpdateChunks()
        {
            chunks = ChunkManager.Manager.ChunksFromBoxPosSize(transform.position, visionBox);
        }

        List<T> GetVisibleItems<T>(Func<T, bool> condition = null) where T : Item
        {
            return ChunkManager.Manager.GetItemsWithinChunkArray<T>(chunks,
                item =>
                (condition == null || condition(item)) && GeoUtils.TestBoxPosSize(transform.position, visionBox, item.transform.position));
        }

        List<T> GetVisibleCharacters<T>(Func<T, bool> condition = null) where T : Character
        {
            return ChunkManager.Manager.GetCharactersWithinChunkArray<T>(chunks,
                character =>
                character != this.character &&
                (condition == null || condition(character)) &&
                GeoUtils.TestBoxPosSize(transform.position, visionBox, character.transform.position));
        }

        //public void OnDamaged(float hp, Character aggressor, int life)
        //{
        //    var profile = GetCharacterProfile(aggressor);

        //    profile.lastDamagedTime = Time.time;
        //    profile.totalDamageDealt += hp;
        //}

        //public void ResetBot()
        //{
        //    //nothing to reset yet
        //}

        private void OnDrawGizmos()
        {
            if (debugLines)
            {
                GeoUtils.DrawBoxPosSize(transform.position, visionBox, UnityEngine.Color.magenta);
            }
        }
    }
}

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
        public bool debugLines = false, logProfiles = false;
        //public Character targetCharacter;
        //public Transform targetObject;
        //public Vector2? targetPos;
        //public float offenseMemory = 5;
        //public float targetFollowDistance;
        public float reactionRate = 5;
        //public int actionMax = 1;
        public readonly Dictionary<int, Dictionary<int, CharacterProfile>> clanProfiles = new(); //first layer clan index, second layer character index
        public readonly Dictionary<int,HashSet<ItemProfile>> itemProfiles = new(); //first layer prefab index, second layer item id
        //readonly CollectionUtils.TwoColumnTable<int, int> itemIdsLocalReal = new();
        readonly CollectionUtils.TwoColumnTable<int, ItemProfile> itemTable = new();
        public Vector2? attackTarget, moveTarget;

        //int nextLocalItemId = 0;

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

        //public List<T> GetBestItems<T>
        //    (Func<T, bool> condition, int? limit = null, bool includeInventory = true, bool considerDistance = true, bool onePerPrefab = true)
        //    where T : Item
        //{
        //    var visible = GetVisibleItems<T>(condition);

        //    if (includeInventory)
        //    {
        //        foreach (var item in character.inventory.itemSlots)
        //        {
        //            if (item)
        //            {
        //                visible.Insert(0, (T)item);
        //            }
        //        }
        //    }

        //    CollectionUtils.SortHighestToLowest(visible,
        //        considerDistance ?
        //        item => ScoringManager.Manager.GetItemScore(item) + ScoringManager.Manager.GetItemDistanceScore(transform.position, item) :
        //        ScoringManager.Manager.GetItemScore
        //        );

        //    //this should probably have a way of privatizing properties of items outside pickup range

        //    if (onePerPrefab)
        //    {
        //        visible = CollectionUtils.ListFirstOfEveryValue(visible, item => item.prefabId, limit);
        //    }
        //    else if (limit.HasValue)
        //        visible.Take(limit.Value); //haven't ever tested this take function

        //    return visible;
        //}

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

                profile.pos = character.transform.position;
                profile.lastSeenTime = Time.time;
                profile.isVisible = true;
                profile.power = ProfileManager.Manager.GetCharacterScore(profile);
                profile.hp = character.hp;

                if (character.inventory.ActiveItem)
                {
                    var itemProfile = UpdateItemProfile(character.inventory.ActiveItem); //hehe this should be way more efficient
                    profile.AddKnownItem(itemProfile);
                }
            }
        }

        TProfile GetItemProfile<TProfile,TItem> (TItem item) where TProfile : ItemProfile, new() where TItem : Item
        { //having generic item type isn't necessary right now, but might as well
            if (!itemProfiles.TryGetValue(item.prefabId, out var prefabSet))
            {
                itemProfiles.Add(item.prefabId, prefabSet = new());
            }

            //bool justDropped = false;

            //(justDropped = item.wielder == null && (foundProfile.wielder != null))

            if (itemTable.TryGet2From1(item.id, out var foundProfile))
            {
                return foundProfile as TProfile;
            }
            //else if (item.wielder && clanProfiles[item.wielder.clan.id][item.wielder.id])
            else
            {
                var profile = new TProfile();
                profile.prefabId = item.prefabId;
                prefabSet.Add(profile);
                //if (!justDropped)
                    itemTable.Add(item.id, profile);
                return profile;
            }
        }

        public ItemProfile UpdateItemProfile<T> (T item) where T : Item
        {
            var profile = GetItemProfile<ItemProfile,Item>(item);

            profile.isVisible = true;
            profile.lastTimeSeen = Time.time;

            if (item.wielder)
            {
                profile.wielder = GetCharacterProfile(item.wielder);
                profile.pos = null;
            }
            else
            {
                profile.pos = item.transform.position;
                profile.wielder = null;

                if (Vector2.Distance(transform.position, item.transform.position) <= character.inventory.activePickupRad)
                {
                    if (profile is GunProfile gunProfile && item is Gun gun)
                    {
                        gunProfile.rounds = gun.rounds;
                    }
                    else if (profile is AmmoProfile ammoProfile && item is Ammo ammo)
                    {
                        ammoProfile.amount = ammo.amount;
                    }
                }
                //else //we should follow the pattern of only correcting when we know for sure
                //{
                //    profile.ResetPrivateProperties();
                //}
            }

            return profile;
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

            if (debugLines)
            {
                foreach (var clanPair in clanProfiles)
                {
                    foreach (var charPair in clanPair.Value)
                    {
                        var charProfile = charPair.Value;

                        if (charProfile.pos.HasValue)
                        {
                            var color = ClanManager.Manager.clans[clanPair.Key].color;

                            color.a = charProfile.isVisible ? 1 : .5f;

                            GeoUtils.DrawCircle(charProfile.pos.Value, 1, color);

                            if (charProfile.items.Count > 0)
                            {
                                GeoUtils.DrawRingOfCircles(charProfile.pos.Value, 1, .5f, charProfile.items.Count, color);
                            }
                        }
                    }
                }

                foreach (var prefabSetPair in itemProfiles)
                {
                    foreach (var itemProfile in prefabSetPair.Value)
                    {
                        if (itemProfile.wielder != null)
                            continue;

                        Vector2? pos = itemProfile.pos;

                        if (pos.HasValue)
                        {
                            float radius = .5f;

                            var color = Color.green;

                            color.a = itemProfile.isVisible ? 1 : .5f;

                            GeoUtils.DrawCircle(pos.Value, radius, color);
                        }
                    }
                }
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

                    foreach (var pair in itemProfiles)
                    {
                        var prefabId = pair.Key;
                        var itemSet = pair.Value;

                        HashSet<ItemProfile> garbage = new();

                        foreach (var itemProfile in itemSet)
                        {
                            if (!itemProfile.isVisible)
                                continue;

                            //var shouldSeeOnGround = itemProfile.pos.HasValue && TestVisionBox(itemProfile.pos.Value);

                            //bool shouldSeeOnCharacter = shouldSeeOnGround ? false :
                            //    itemProfile.wielder != null &&
                            //    itemProfile.wielder.pos.HasValue &&
                            //    TestVisionBox(itemProfile.wielder.pos.Value); 
                            //so at this point, it will just remember every item this character picked up

                            bool shouldSee = false;// || shouldSeeOnCharacter;

                            bool dontSee = true;

                            itemTable.TryGet1From2(itemProfile, out var id);
                            ItemManager.Manager.active.TryGetValue(id, out var item);

                            if (itemProfile.pos.HasValue && TestVisionBox(itemProfile.pos.Value))
                            {
                                shouldSee = true;                                 

                                dontSee = 
                                    !item ||
                                    !TestVisionBox(item.transform.position);
                            }
                            else if (itemProfile.wielder != null &&
                                itemProfile.wielder.pos.HasValue &&
                                TestVisionBox(itemProfile.wielder.pos.Value) &&
                                CharacterManager.Manager.activeDictionary.TryGetValue(itemProfile.wielder.id, out var character) &&
                                TestVisionBox(character.transform.position)) //should see wielder
                            {
                                //shouldSee = true;

                                dontSee =
                                    //! ||
                                    character.inventory.ActiveItem != item;
                                    //!TestVisionBox(character.transform.position);
                            }
                            //else if (shouldSeeOnCharacter)
                            //{
                            //    dontSee =
                            //        !CharacterManager.Manager.activeDictionary.TryGetValue(itemProfile.wielder.id, out var character) ||
                            //        !TestVisionBox(character.transform.position);
                            //}

                            if (dontSee)
                            {
                                if (shouldSee)
                                {
                                    garbage.Add(itemProfile);
                                }
                                else
                                {
                                    itemProfile.isVisible = false;

                                    if (itemProfile.wielder != null)
                                        itemTable.RemoveFromColumn2(itemProfile);
                                }
                            }
                        }

                        foreach (var itemProfile in garbage)
                        {
                            itemSet.Remove(itemProfile);
                            itemTable.RemoveFromColumn2(itemProfile);
                        }
                    }

                    foreach (var clanPair in clanProfiles)
                    {
                        foreach (var charPair in clanPair.Value)
                        {
                            charPair.Value.isVisible = false;
                        }
                    }

                    //var visibleCharacters = GetVisibleCharacters<Character>();
                    UpdateCharacterProfiles(GetVisibleCharacters<Character>());
                    UpdateItemProfiles(GetVisibleItems<Item>());

                    if (logProfiles)
                        LogProfiles();

                    Reaction();

                    yield return new WaitForSeconds(1 / reactionRate);
                }
            }
        }

        public string LogProfiles ()
        {
            string log = $"Bot {character.id} Profile Log: Characters: (";

            foreach (var clanPair in clanProfiles)
            {
                foreach (var charPair in clanPair.Value)
                {
                    log += $"({charPair.Value.Print}), ";
                }
            }

            log += ") Items: ";

            foreach (var pair in itemProfiles)
            {
                foreach (var itemProfile in pair.Value)
                {
                    if (itemProfile.wielder == null)
                        log += $"({itemProfile.Print}), ";
                }
            }

            log += ")";

            Debug.Log(log);

            return log;
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

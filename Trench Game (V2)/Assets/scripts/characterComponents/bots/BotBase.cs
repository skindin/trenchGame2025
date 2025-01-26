using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;


namespace TrenchBot
{
    [RequireComponent(typeof(Character))]
    public abstract partial class BotBase : MonoBehaviour
    {
        public Character character;
        public Vector2 visionBox;
        public bool debugLines = false;
        //public Character targetCharacter;
        //public Transform targetObject;
        public Vector2? targetPos;
        //public float offenseMemory = 5;
        //public float targetFollowDistance;
        public float reactionRate = 5;
        public readonly Dictionary<Character, CharacterProfile> enemyProfiles = new(), allyProfiles = new();
        public CharacterProfile targetEnemy, targetAlly;
        public readonly Dictionary<Item, ItemProfile> itemProfiles;
        public ItemProfile targetItem;

        public Chunk[,] chunks;

        //private void Awake()
        //{
            //character = GetComponent<Character>();
        //}

        private void OnEnable()
        {
            UpdateChunks();
            StartRefreshRoutine();
        }

        public Vector2 FindBulletPathToPos(Vector2 pos)
        {
            return transform.position;
        }

        public bool TestVisionBox(Vector2 pos)
        {
            return GeoUtils.TestBoxPosSize(transform.position, visionBox, pos);
        }

        public void MoveToPos(Vector2 pos, float distance, bool maintainDist = false)
        {
            var delta = (Vector2)transform.position - pos;

            if (!maintainDist && delta.magnitude <= distance)
            {
                return;
            }

            var nextPos = delta.normalized * distance + pos;

            nextPos = GeoUtils.ClampToBoxPosSize(nextPos, pos, visionBox);

            character.MoveToPos(nextPos);
            character.LookInDirection(-delta);
        }

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

        public T PickupItemsInOrder<T>(IEnumerable<T> items, int? pickupLimit = null, bool sortByDistance = false, bool selectBest = true) where T : Item
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
                else if (Vector2.Distance(item.transform.position, transform.position) <= character.inventory.activePickupRad)
                {
                    if (!pickupLimit.HasValue || pickedUp < pickupLimit.Value)
                    {
                        character.LookInDirection(item.transform.position - transform.position);

                        if (!character.inventory.itemSlots.Contains(null))
                        {
                            character.inventory.CurrentSlot = lowestValueSlot;
                        }

                        character.inventory.PickupItem(item, item.transform.position, true);

                        if (debugLines)
                            GeoUtils.MarkPoint(item.transform.position, 1, Color.red);

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

        public List<T> GetStrongestCharacters<T>(Func<T, bool> condition, bool considerDistance = true) where T : Character
        {
            var visibleCharacters = GetVisibleCharacters(condition);

            Func<T, float> getValue = considerDistance ?
                character => ScoringManager.Manager.GetCharacterScore(character) +
                ScoringManager.Manager.GetCharacterDistanceScore(transform.position, character.transform.position) :
                ScoringManager.Manager.GetCharacterScore;

            CollectionUtils.SortHighestToLowest(visibleCharacters, getValue);

            return visibleCharacters;
        }

        public void UpdateProfiles<T>(IEnumerable<T> list) where T : Character
        {
            foreach (T character in list)
            {
                var profile = GetCharacterProfile(character);
                profile.SetCurrentVelocity(character.transform.position, 1 / reactionRate);

                profile.lastKnownPos = character.transform.position;
                profile.lastSeenTime = Time.time;
                profile.lastKnownPower = ScoringManager.Manager.GetCharacterScore(character);
            }
        }

        private void Update()
        {
            UpdateChunks();


        }

        public void StartRefreshRoutine()
        {
            StartCoroutine(Refresh());

            IEnumerator Refresh()
            {
                while (true)
                {
                    Reaction();

                    yield return new WaitForSeconds(1 / reactionRate);
                }
            }
        }

        public abstract void Reaction();

        void AttackTargetEnemy(Vector2 direction)
        {
            if (character.inventory.ActiveWeapon is Gun gun)
            {
                gun.DirectionalAction(direction);
            }
        }

        void UpdateChunks()
        {
            chunks = ChunkManager.Manager.ChunksFromBoxPosSize(transform.position, visionBox);
        }

        public List<T> GetVisibleItems<T>(Func<T, bool> condition = null) where T : Item
        {
            return ChunkManager.Manager.GetItemsWithinChunkArray<T>(chunks,
                item =>
                (condition == null || condition(item)) && GeoUtils.TestBoxPosSize(transform.position, visionBox, item.transform.position));
        }

        public List<T> GetVisibleCharacters<T>(Func<T, bool> condition = null) where T : Character
        {
            return ChunkManager.Manager.GetCharactersWithinChunkArray<T>(chunks,
                character =>
                character != this.character &&
                (condition == null || condition(character)) &&
                GeoUtils.TestBoxPosSize(transform.position, visionBox, character.transform.position));
        }

        public void OnDamaged(float hp, Character aggressor, int life)
        {
            var profile = GetCharacterProfile(aggressor);

            profile.lastDamagedTime = Time.time;
            profile.totalDamageDealt += hp;
        }

        CharacterProfile GetCharacterProfile(Character character)
        {
            if (enemyProfiles.TryGetValue(character, out var profile))
            {
                return profile;
            }
            else
            {
                var newProfile = new CharacterProfile();
                enemyProfiles.Add(character, newProfile);
                return newProfile;
            }
        }

        public void ResetBot()
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
}

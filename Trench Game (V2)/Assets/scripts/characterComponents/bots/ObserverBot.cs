using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine;
//using Chunks;

namespace BotBrains
{
    public class ObserverBot : MonoBehaviour
    {
        public Character character;
        public ObserverSubject thisSubject;
        public Vector2 visionBox;
        public SubjectChunkArray chunkArray = new();
        public bool debugLines = false;
        public readonly BotBehavior behavior = new();
        public float reactionRate = 5f;
        IEnumerable<Vector2Int> visibleChunkAddresses;
        readonly CollectionUtils.TwoColumnTable<int, ItemProfile> itemProfilePairs = new();

        private void Start()
        {
            chunkArray = BotManager.Manager.subjectChunkArray;
        }

        //now we just gotta set up chunk observation

        private void OnEnable()
        {
            StartReactionRoutine();
            StartObservationRoutine();
        }

        void StartReactionRoutine ()
        {
            StartCoroutine(ReactionRoutine());

            IEnumerator ReactionRoutine ()
            {
                while (true)
                {
                    yield return new WaitForSeconds(1 / reactionRate);

                    behavior.Action();
                }
            }
        }

        void StartObservationRoutine ()
        {
            StartCoroutine (ObservationRoutine());

            IEnumerator ObservationRoutine () //hopefully this won't miss anything
            {
                while (true)
                {
                    yield return null;

                    Observe();
                }
            }
        }

        void UpdateVisibleChunks ()
        {
            visibleChunkAddresses = Chunks.ChunkManager.AddressesFromBoxPosSize(transform.position, visionBox);
        }

        void Observe ()
        {
            UpdateVisibleChunks();

            //foreach (var subject in GetVisibleSubjects())
            //{
            //atleast this was designed in case I need it...
            //}

            foreach (var pair in itemProfilePairs)
            {
                if (!ItemManager.Manager.active.TryGetValue(pair.value1,out var item) ||
                    TestVisionBox(item.transform.position))
                {
                    var profile = pair.value2;

                    profile.isVisible = false;
                    profile.pos = null;
                }
            }

            foreach (var item in GetVisibleItems<Item>())
            {
                if (item is Gun gun)
                {
                    UpdateProfile<GunProfile>(gun);
                }
                else if (item is Consumable consumable)
                {
                    UpdateProfile<ConsumableProfile>(consumable);
                }
                else if (item is Ammo ammo)
                {
                    UpdateProfile<AmmoProfile>(ammo);
                }
                else if (item is Spade spade)
                {
                    UpdateProfile<AmmoProfile>(spade);
                }
            }

            foreach (var character in GetVisibleCharacters<Character>())
            {
                //blehhh
            }
        }

        public T UpdateProfile<T>(Item item) where T : ItemProfile, new()
        {
            var profile = GetProfile<T>(item);
            profile.UpdateWithItem(item); //gotta connect character profile here later
            return profile;
        }

        public T GetProfile<T> (Item item) where T : ItemProfile , new()
        {
            if (!itemProfilePairs.TryGet2From1(item.id,out var profile))
            {
                profile = new T { prefabId = item.prefabId };
                AddItemProfileToBehavior(profile);
                itemProfilePairs.Add(item.id,profile);
            }
            //Hashtable table = new Hashtable();

            if (profile is T t)
            {
                return t;
            }

            return null;
        }

        public void AddItemProfileToBehavior<T> (T profile) where T : ItemProfile
        {
            if (!behavior.itemsByPrefab.TryGetValue(profile.prefabId, out var set))
            {
                set = new();
            }

            set.Add(profile);
        }

        public void RemoveItemProfileFromBehavior (ItemProfile profile)
        {
            if (behavior.itemsByPrefab.TryGetValue(profile.prefabId, out var set))
            {
                set.Remove(profile);
                if (set.Count == 0)
                {
                    behavior.itemsByPrefab.Remove(profile.prefabId);
                }
            }
        }

        public bool TestVisionBox(Vector2 pos)
        {
            return GeoUtils.TestBoxPosSize(transform.position, visionBox, pos, debugLines);
        }

        public IEnumerable<T> GetVisibleItems<T> () where T : Item
        {
            foreach (var item in ItemManager.Manager.chunkArray.ObjectsFromAddresses(visibleChunkAddresses))
            {
                if (item is T t && TestVisionBox(item.transform.position))
                    yield return t;
            }
        }

        public IEnumerable<T> GetVisibleCharacters<T>() where T : Character
        {
            foreach (var character in CharacterManager.Manager.chunkArray.ObjectsFromAddresses(visibleChunkAddresses))
            {
                if (character is T t && TestVisionBox(character.transform.position))
                    yield return t;
            }
        }

        public IEnumerable<ObserverSubject> GetVisibleSubjects ()
        {
            foreach (var subject in chunkArray.ObjectsFromAddresses(visibleChunkAddresses))
            {
                if (subject == thisSubject || !TestVisionBox(subject.transform.position))
                    //if this is itself, or we can't see it, move on
                    continue;

                yield return subject;
                //observer logic here
            }
        }

        //public void Moved(Vector2 pos)
        //{
        //    chunkArray.CharacterMoved(character);
        //    UpdateChunks();
        //}

        //public void UpdateChunks ()
        //{
        //    chunkArray.SetObserverChunksBoxPosSize(this, transform.position, visionBox);
        //}

        //dropped item and picked up item will be run by inventory unity events
        //public void DroppedItem (Item item)
        //{
        //    chunkArray.DroppedItem(character, item);
        //}

        //public void PickedUpItem (Item item)
        //{
        //    chunkArray.DroppedItem(character, item);
        //}

        //public void ObserveItemDrop (Character subject, Item item)
        //{
        //    if (!TestVisionBox(subject.transform.position)) //if we can't actually see the character, we don't know where the item came from
        //        return;

        //    var canSeeItem = TestVisionBox(item.transform.position);
        //    //a character we are watching dropped an item
        //}

        //public void ObserveItemPickup (Character subject, Item item)
        //{
        //    if (!TestVisionBox(subject.transform.position)) //if we can't see the character, we shouldn't modify its profile
        //        return;

        //    var canSeeItem = TestVisionBox(item.transform.position);
        //    //a character we are watching picked up an item
        //}

        //public void ObserveItemSpawn (Item item)
        //{
        //    if (!TestVisionBox(item.transform.position)) //if we can't see the character, we shouldn't modify its profile
        //        return;
        //}

        //public void ObserveCharacterMove (Character subject)
        //{
        //}
    }
}
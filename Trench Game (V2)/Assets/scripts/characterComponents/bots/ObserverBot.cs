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
        readonly Dictionary<int, ItemProfile> itemProfilePairs = new();

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

            foreach (var item in GetVisibleItems<Item>())
            {

            }

            foreach (var character in GetVisibleCharacters<Character>())
            {

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
                if (item is T generic && TestVisionBox(item.transform.position))
                    yield return generic;
            }
        }

        public IEnumerable<T> GetVisibleCharacters<T>() where T : Character
        {
            foreach (var character in CharacterManager.Manager.chunkArray.ObjectsFromAddresses(visibleChunkAddresses))
            {
                if (character is T generic && TestVisionBox(character.transform.position))
                    yield return generic;
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
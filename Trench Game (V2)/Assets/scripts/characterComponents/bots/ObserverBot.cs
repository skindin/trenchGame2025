using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine;

namespace BotBrains
{
    public class ObserverBot : MonoBehaviour
    {
        public Character character;
        public Vector2 visionBox;
        public BotObserverChunkArray chunkArray = new();
        public bool debugLines = false;
        public readonly BotBehavior behavior;
        public float reactionRate = 5f;

        private void Start()
        {
            chunkArray = BotManager.Manager.observerChunkArray;
        }

        //now we just gotta set up chunk observation

        private void OnEnable()
        {
            StartReactionRoutine();
        }

        void StartReactionRoutine ()
        {
            StartCoroutine(ReactionRoutine());

            IEnumerator ReactionRoutine ()
            {
                behavior.Action();

                yield return new WaitForSeconds(1 / reactionRate);
            }
        }

        public void Moved(Vector2 pos)
        {
            chunkArray.CharacterMoved(character);
            UpdateChunks();
        }

        public void UpdateChunks ()
        {
            chunkArray.SetObserverChunksBoxPosSize(this, transform.position, visionBox);
        }

        //dropped item and picked up item will be run by inventory unity events
        public void DroppedItem (Item item)
        {
            chunkArray.DroppedItem(character, item);
        }

        public void PickedUpItem (Item item)
        {
            chunkArray.DroppedItem(character, item);
        }

        public bool TestVisionBox (Vector2 pos)
        {
            return GeoUtils.TestBoxPosSize(transform.position, visionBox, pos, debugLines);
        }

        public void ObserveItemDrop (Character subject, Item item)
        {
            if (!TestVisionBox(subject.transform.position)) //if we can't actually see the character, we don't know where the item came from
                return;

            var canSeeItem = TestVisionBox(item.transform.position);
            //a character we are watching dropped an item
        }

        public void ObserveItemPickup (Character subject, Item item)
        {
            if (!TestVisionBox(subject.transform.position)) //if we can't see the character, we shouldn't modify its profile
                return;

            var canSeeItem = TestVisionBox(item.transform.position);
            //a character we are watching picked up an item
        }

        public void ObserveItemSpawn (Item item)
        {
            if (!TestVisionBox(item.transform.position)) //if we can't see the character, we shouldn't modify its profile
                return;
        }

        public void ObserveCharacterMove (Character subject)
        {
        }
    }
}
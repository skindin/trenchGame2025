using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BotBrains
{
    public class ObserverBot : MonoBehaviour
    {
        public Character character;
        public Vector2 visionBox;
        public BotObserverChunkArray chunkArray = new();

        private void Start()
        {
            chunkArray = BotManager.Manager.observerChunkArray;
        }

        //now we just gotta set up chunk observation

        public void DroppedItem (Item item)
        {
            chunkArray.DroppedItem(character, item);
        }

        public void PickedUpItem (Item item)
        {
            chunkArray.DroppedItem(character, item);
        }

        public void ObserveItemDrop (Character subject, Item item)
        {
            //a character we are watching dropped an item
        }

        public void ObserveItemPickup (Character subject, Item item)
        {
            //a character we are watching picked up an item
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chunks;
using UnityEngine;

namespace BotBrains
{
    public class BotObserverChunkArray : ChunkArray<BotObserverChunk>
    {
        public override BotObserverChunk this[Vector2Int address] 
        { 
            get
            {
                if (base[address] == null)
                {
                    base[address] = new();
                }

                return base[address];
            }
            set => base[address] = value;
        }

        public BotObserverChunkArray() : base()
        {
        }

        //public void ObserveChunksBoxMinMax (ObserverBot bot)
        //{
        //    foreach (var)
        //}

        public void ItemAction (Character subject, Item item, Action<ObserverBot, Character, Item> action)
        {
            var subjectAddress = Chunks.ChunkManager.PosToAdress(subject.transform.position);
            var itemAddress = Chunks.ChunkManager.PosToAdress(item.transform.position);

            if (subjectAddress == itemAddress)
            {
                foreach (var observer in this[subjectAddress].observers)
                {
                    if (observer.character == subject)
                        continue;

                    action(observer, subject, item);
                }
            }
            else
            {
                HashSet<ObserverBot> alreadyTold = new();

                foreach (var observer in this[subjectAddress].observers)
                {
                    if (observer.character == subject)
                        continue;

                    action(observer,subject,item);
                    alreadyTold.Add(observer);
                }

                foreach (var observer in this[itemAddress].observers)
                {
                    if (observer.character == subject)
                        continue;

                    if (!alreadyTold.Contains(observer))
                    {
                        action(observer, subject, item);
                    }
                }
            }
        }

        public void PickedUpItem (Character subject, Item item)
        {
            ItemAction(subject, item, (observer, subject, item) => observer.ObserveItemPickup(subject, item));
        }

        public void DroppedItem (Character subject, Item item)
        {
            ItemAction(subject, item, (observer, subject, item) => observer.ObserveItemDrop(subject, item));
        }
    }
}

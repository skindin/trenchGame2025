using Chunks;
using System;
using System.Collections.Generic;
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

        readonly Dictionary<ObserverBot,HashSet<Vector2Int>> observerDict = new();

        public void RemoveObserver (ObserverBot observer)
        {
            if (observerDict.TryGetValue(observer, out var set))
            {
                foreach (var address in set)
                {
                    this[address].observers.Remove(observer);
                }

                observerDict.Remove(observer);
            }
        }

        public void AddObserverToChunk (ObserverBot bot, Vector2Int address)
        {
            this[address].observers.Add(bot);

            if (!observerDict.TryGetValue(bot, out var set))
            {
                set = new();
                observerDict.Add(bot, set);
            }

            set.Add(address);
        }

        public void SetObserverChunksBoxMinMax(ObserverBot bot, Vector2 min, Vector2 max)
        {
            RemoveObserver(bot);

            foreach (var pair in FromBoxMinMax(min, max))
            {
                if (pair.obj == null)
                    continue;

                AddObserverToChunk(bot, pair.address);
            }
        }

        public void SetObserverChunksBoxPosSize(ObserverBot bot, Vector2 pos, Vector2 size)
        {
            RemoveObserver(bot);

            foreach (var pair in FromBoxPosSize(pos, size))
            {
                if (pair.obj == null)
                {
                    AddObserverToChunk(bot, pair.address);
                }
            }
        }

        public void CharacterItemAction (Character subject, Item item, Action<ObserverBot, Character, Item> action)
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
        //might be a little unnecessary to notify observers about an item disappearing

        public void PickedUpItem (Character subject, Item item)
        {
            CharacterItemAction(subject, item, (observer, subject, item) => observer.ObserveItemPickup(subject, item));
        }

        public void DroppedItem (Character subject, Item item)
        {
            CharacterItemAction(subject, item, (observer, subject, item) => observer.ObserveItemDrop(subject, item));
        }

        public void SpawnedItem (Item item)
        {
            foreach (var observer in FromPos(item.transform.position).observers)
            {
                observer.ObserveItemSpawn(item);
            }
        }

        public void CharacterMoved (Character subject, Vector2 prevPos)
        {
            HashSet<ObserverBot> alreadyTold = new();

            foreach (var chunk in FromLine(prevPos,subject.transform.position, chunk => chunk != null))
            {
                foreach (var observer in chunk.obj.observers)
                {
                    if (observer.character == subject || alreadyTold.Contains(observer))
                        continue;

                    observer.ObserveCharacterMove(subject);
                    alreadyTold.Add(observer);
                }
            }
        }
    }
}

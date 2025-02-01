using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Chunks;

namespace BotBrains
{   
    public class BotManager : ManagerBase<BotManager>
    {
        public BotObserverChunkArray observerChunkArray;

        private void Awake()
        {
            Chunks.ChunkManager.Initialize();
            observerChunkArray = new ();
        }        
    }
}


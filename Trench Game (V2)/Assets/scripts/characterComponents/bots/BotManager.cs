using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Chunks;

namespace BotBrains
{   
    public class BotManager : ManagerBase<BotManager>
    {
        public SubjectChunkArray subjectChunkArray;

        private void Awake()
        {
            Chunks.ChunkManager.Initialize();
            subjectChunkArray = new ();
        }        
    }
}


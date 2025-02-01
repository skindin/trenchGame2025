using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chunks;

namespace BotBrains
{
    public class BotListenerChunk
    {
        public HashSet<BotBase> occupants = new(), listeners = new();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotBrains
{
    public class BotBehavior
    {
        public readonly Dictionary<int, Dictionary<int, CharacterProfile>> charactersByClan = new();
        public readonly Dictionary<int, HashSet<ItemProfile>> itemsByPrefab = new();
        public UnityEngine.Vector2? attackTarget, moveTarget;

        public virtual void Action ()
        {
            //where the thinking goes
        }
    }
}

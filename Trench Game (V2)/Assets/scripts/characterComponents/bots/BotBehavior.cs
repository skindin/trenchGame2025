using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BotBrains
{
    public class BotBehavior //REVIEWERS: ignore this script
    {
        public readonly CharacterProfile selfProfile = new();
        public readonly Dictionary<int, Dictionary<int, CharacterProfile>> charactersByClanId = new();
        public readonly Dictionary<int, HashSet<ItemProfile>> itemsByPrefabId = new();
        public Vector2? attackTarget, moveTarget;
        public ItemProfile targetItem;
        //there should be variables for other things, like preferred items

        public virtual void Think ()
        {
            //where the thinking goes
            
        }
    }
}

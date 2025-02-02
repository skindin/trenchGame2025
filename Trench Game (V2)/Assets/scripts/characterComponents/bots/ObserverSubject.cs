using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections;

namespace BotBrains
{
    public class ObserverSubject : MonoBehaviour
    {
        public List<Item> dropped = new(), pickedUp = new();

        private void LateUpdate()
        {
            ResetEventLists();
        }

        void ResetEventLists()
        {
            dropped.Clear();
            pickedUp.Clear();
        }

        public void UpdateChunk ()
        {
            BotManager.Manager.subjectChunkArray.UpdateSubjectsChunk(this);
        }

        public void OnDroppedItem (Item item)
        {
            //BotManager.Manager.subjectChunkArray
            dropped.Add(item);
        }

        public void OnPickedUpItem (Item item)
        {
            pickedUp.Add(item);
        }
    }
}

using BotBrains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Chunks
{
    public class MonoBehaviorChunkArray<TObject> : CollectionChunkArray<TObject,HashSet<TObject>>
        where TObject : MonoBehaviour
    {
        public readonly Dictionary<TObject, Vector2Int> objChunkDict = new();

        public override HashSet<TObject> this[Vector2Int address]
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

        public void RemoveObject(TObject obj, Vector2Int address)
        {
            objChunkDict.Remove(obj);
            this[address].Remove(obj);
        }

        public void RemoveObject (TObject obj)
        {
            if (objChunkDict.TryGetValue(obj, out var address))
            {
                RemoveObject(obj, address);
            }
        }

        public void UpdateObjectChunk(TObject obj)
        {
            var newAddress = ChunkManager.PosToAdress(obj.transform.position);

            bool hadValue = objChunkDict.TryGetValue(obj, out var prevAddress);

            if (hadValue && newAddress != prevAddress) //if had a previous address, and new address is different
            {
                RemoveObject(obj, newAddress);
                //remove refference from previous chunk
            }

            if (!hadValue || newAddress != prevAddress) //if no previous address, or the new address is a different address
            {
                this[newAddress].Add(obj); //add refference to new address chunk
            }
        }
    }
}

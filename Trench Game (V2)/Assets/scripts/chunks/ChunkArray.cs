using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace Chunks
{
    public class ChunkArray<T>
    {
        T[,] objects;

        public virtual T this[Vector2Int address]
        {
            get
            {
                return objects[address.x,address.y];
            }

            set
            {
                objects[address.x,address.y] = value;
            }
        }

        public ChunkArray ()
        {
            TrySetup();
        }

        public virtual void Setup ()
        {
            ChunkManager.Initialize();
            objects = ChunkManager.GetChunkPairArray<T>();
        }

        public bool TrySetup ()
        {
            try
            {
                Setup();
                return true;
            }
            catch
            {
                //
                return false;
            }
        }

        public T FromPos (Vector2 pos)
        {
            var address = ChunkManager.PosToAdress (pos);

            address = Vector2Int.Max(Vector2Int.zero, address);
            address = Vector2Int.Min(Vector2Int.one * (ChunkManager.ChunkArraySize.Value-1), address);

            return this[address];
        }

        public IEnumerable<ChunkAddressPair<T>> FromBoxMinMax(Vector2 min, Vector2 max, Func<T, bool> condition = null)
        {
            foreach (var address in ChunkManager.AddressesFromBoxMinMax(min,max))
            {
                var obj = this[address];

                if (condition == null || condition(obj))
                {
                    yield return new(address, obj);
                }
                //to be continued
            }
        }

        public IEnumerable<ChunkAddressPair<T>> FromBoxPosSize (Vector2 pos, Vector2 size, Func<T, bool> condition = null)
        {
            foreach (var address in ChunkManager.AddressesFromBoxPosSize(pos, size))
            {
                var obj = this[address];

                if (condition == null || condition(obj))
                {
                    yield return new(address,obj);
                }
                //to be continued
            }
        }

        public IEnumerable<ChunkAddressPair<T>> FromLine(Vector2 pointA, Vector2 pointB, Func<T, bool> condition = null)
        {
            foreach (var address in ChunkManager.AddressesFromLine (pointA, pointB))
            {
                if (condition == null || condition(this[address]))
                    yield return new(address, this[address]);
            }
        }

        public IEnumerable<ChunkAddressPair<T>> All ()
        {
            foreach (var address in ChunkManager.AllAddresses())
            {
                yield return new ChunkAddressPair<T>(address, this[address]);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chunks
{
    public class ChunkArray<T>
    {
        T[,] objects;

        public T this[Vector2Int address]
        {
            get
            {
                TrySetup();

                return objects[address.x,address.y];
            }

            set
            {
                TrySetup();

                objects[address.x,address.y] = value;
            }
        }

        public void Setup ()
        {
            objects = ChunkManager.GetChunkPairArray<T>();
        }

        public void TrySetup ()
        {
            if (objects == null)
                Setup();
        }

        public T ObjectFromPos (Vector2 pos)
        {
            TrySetup();

            var address = ChunkManager.PosToAdress (pos);

            address = Vector2Int.Max(Vector2Int.zero, address);
            address = Vector2Int.Min(Vector2Int.one * (ChunkManager.chunkArraySize.Value-1), address);

            return this[address];
        }

        public IEnumerable<T> FromBoxMinMax(Vector2 min, Vector2 max, Func<T, bool> condition)
        {
            foreach (var address in ChunkManager.AddressesFromBoxMinMax(min,max))
            {
                var obj = this[address];

                if (condition == null || condition(obj))
                {
                    yield return obj;
                }
                //to be continued
            }
        }

        public IEnumerable<T> FromBoxPosSize (Vector2 pos, Vector2 size, Func<T, bool> condition)
        {
            foreach (var address in ChunkManager.AddressesFromBoxPosSize(pos, size))
            {
                var obj = this[address];

                if (condition == null || condition(obj))
                {
                    yield return obj;
                }
                //to be continued
            }
        }

        public IEnumerable<T> FromLine(Vector2 pointA, Vector2 pointB)
        {
            foreach (var address in ChunkManager.AddressesFromLine (pointA, pointB))
            {
                yield return this[address];
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
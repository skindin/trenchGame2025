using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Chunks
{
    public class AreaChunkArray<T> : CollectionChunkArray<T,HashSet<T>>
    {
        readonly Dictionary<T, HashSet<Vector2Int>> objAddressesDict = new();

        public void RemoveObject (T obj)
        {
            if (objAddressesDict.TryGetValue(obj, out var addressSet))
            {
                foreach (var address in addressSet)
                {
                    this[address].Remove(obj);
                }

                objAddressesDict.Remove(obj);
            }
        }

        public void UpdateAreaByBoxMinMax (T obj, Vector2 min, Vector2 max)
        {
            RemoveObject(obj);

            if (!objAddressesDict.TryGetValue(obj,out var addressSet))
            {
                objAddressesDict.Add(obj, addressSet = new());
            }

            foreach (var pair in FromBoxMinMax(min, max))
            {
                this[pair.address].Add(obj);
                addressSet.Add(pair.address);
            }
        }

        public void UpdateAreaByBoxPosSize (T obj, Vector2 pos, Vector2 size)
        {
            GeoUtils.BoxPosSizeToMinMax(pos, size, out var min, out var max);
            UpdateAreaByBoxMinMax(obj, min, max);
        }

        public void UpdateAreaByCircle (T obj, Vector2 center, float radius)
        {
            GeoUtils.CircleToBoxPosSize(center, radius, out var pos, out var size);
            UpdateAreaByBoxPosSize(obj, pos, size);
        }

        public override IEnumerable<T> ObjectsFromAddresses(IEnumerable<Vector2Int> addresses)
        {
            HashSet<T> alreadyFound = new();

            foreach (var obj in base.ObjectsFromAddresses(addresses))
            {
                if (!alreadyFound.Contains(obj))
                {
                    alreadyFound.Add(obj);
                    yield return obj;
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Chunks
{
    public class CollectionChunkArray<TObject,TCollection> : ChunkArray<TCollection> where TCollection : IEnumerable<TObject>
    {

        public virtual IEnumerable<TObject> ObjectsFromAddresses(IEnumerable<Vector2Int> addresses)
        {
            foreach (var address in addresses)
            {
                var collection = this[address];

                if (collection != null)
                {
                    foreach ( var item in collection)
                    {
                        yield return item;
                    }
                }
            }
        }
    }
}

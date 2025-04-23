using System;
using System.Collections.Generic;
using UnityEngine;
//using static UnityEngine.RuleTile.TilingRuleOutput;

namespace Chunks
{
    public class ChunkArray<T>
    {
        //this is basically just a generic array type intended for different systems to use independantly.
        //basically, other scripts can create their own uses, instead of needing to add a new property every time
        //i need to use chunks for something

        T[,] objects;

        public virtual T this[Vector2Int address]
        {
            get
            {
                if (objects == null)
                    Setup();

                if (TestValidAddress(address))
                {

                    return objects[address.x, address.y];
                }

                return default;
            }

            set
            {

                if (TestValidAddress(address))
                {

                    objects[address.x, address.y] = value;
                }
                //objects[address.x,address.y] = value;
                else
                    throw new Exception("address is out of world bounds");
            }
        }

        /// <summary>
        /// tests if the address is within chunk manager array size
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public bool TestValidAddress (Vector2Int address)
        {
            return address.x < objects.GetLength(0) && address.x >= 0 &&
                    address.y < objects.GetLength(1) && address.y >= 0;
        }

        public ChunkArray ()
        {
            TrySetup();//tbh i can't remember if i should use trysetup or setup
        }

        /// <summary>
        /// initializes chunk manager before getting object array
        /// </summary>
        public virtual void Setup ()
        {
            ChunkManager.Initialize();
            objects = ChunkManager.GetObjectArray<T>();
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

        /// <summary>
        /// returns object/chunk closest to worls pos
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public T FromPos (Vector2 pos)
        {
            var address = ChunkManager.PosToAdress (pos);

            //address = Vector2Int.Max(Vector2Int.zero, address);
            //address = Vector2Int.Min(Vector2Int.one * (ChunkManager.ChunkArraySize-1), address);

            return this[address];
        }

        /// <summary>
        /// returns ienumerable of chunk-address pairs of all chunks touching box of min and max, optional condition func
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
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

        /// <summary>
        /// returns ienumerable of chunk-address pairs of all chunks touching box at pos with size, optional condition func
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="size"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
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

        /// <summary>
        /// returns ienumerable of chunk-address pairs of all chunks touching line from pointA to pointB 
        /// </summary>
        /// <param name="pointA"></param>
        /// <param name="pointB"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        public IEnumerable<ChunkAddressPair<T>> FromLine(Vector2 pointA, Vector2 pointB, Func<T, bool> condition = null)
        {
            foreach (var address in ChunkManager.AddressesFromLine (pointA, pointB))
            {
                if (condition == null || condition(this[address]))
                    yield return new(address, this[address]);
            }
        }


        /// <summary>
        /// returns all chunks paired with their addresses
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ChunkAddressPair<T>> All ()
        {
            foreach (var address in ChunkManager.AllAddresses())
            {
                yield return new ChunkAddressPair<T>(address, this[address]);
            }
        }
    }

    public struct ChunkAddressPair<T> //temporary pair object to simplify iteration
        //instead of using two for loops for x and y, just use a foreach loop and get the x and y from this
        //same as idea as a KeyValuePair
    {
        public readonly Vector2Int address;
        public T obj;

        public ChunkAddressPair(Vector2Int address, T obj)
        {
            this.address = address;
            this.obj = obj;
        }
    }
}
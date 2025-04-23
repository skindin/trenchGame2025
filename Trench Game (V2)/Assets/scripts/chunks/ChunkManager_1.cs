using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Chunks
{
    //this script provides a way to edit the world size and chunk array size from a single object in the unity editor
    //many independant systems use this chunk manager to group their object types based on location

    public class ChunkManager : ManagerBase<ChunkManager>
    {
        public float worldSize, minChunkSize;

        public static float WorldSize
        {
            get
            {
                if (!cachedWorldSize.HasValue)
                {
                    Initialize();
                }

                return cachedWorldSize.Value;
            }
        }
        static float? cachedWorldSize;

        public static float ChunkSize {
            get
            {
                if (!cachedChunkSize.HasValue)
                {
                    Initialize();
                }

                return cachedChunkSize.Value;
            }        
        }
        static float? cachedChunkSize;

        public static int ChunkArraySize
        {
            get
            {
                if (!cachedChunkArraySize.HasValue)
                {
                    Initialize();
                }

                return cachedChunkArraySize.Value;
            }
        }
        static int? cachedChunkArraySize;


        /// <summary>
        /// initializes static and input-dependant variables
        /// </summary>
        public static void Initialize ()
        {
            cachedWorldSize = Manager.worldSize;
            cachedChunkArraySize = Mathf.FloorToInt(WorldSize / Manager.minChunkSize);
            cachedChunkSize = Manager.worldSize / ChunkArraySize;
        }

        //private void Awake()
        //{
        //    Initialize();
        //}


        //returns array of generic type with chunk-world dimensions
        public static T[,] GetObjectArray<T>()
        {
            var array = new T[ChunkArraySize, ChunkArraySize];

            return array;
        }


        /// <summary>
        /// converts world position to vector2int grid cell
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static Vector2Int PosToAdress(Vector2 pos)
        {
            var min = -Vector2.one / 2 * WorldSize;
            var delta = pos - min;
            var adress = Vector2Int.FloorToInt(delta / ChunkSize);
            adress = Vector2Int.Min(Vector2Int.one * (ChunkArraySize-1), adress);
            adress = Vector2Int.Max(Vector2Int.zero, adress);
            return adress;
        }

        /// <summary>
        /// clamps world position to world boundaries
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static Vector2 ClampPosToWorld (Vector2 pos)
        {
            return GeoUtils.ClampToBoxPosSize(pos, Vector2.zero, Vector2.one * WorldSize);
        }


        /// <summary>
        /// gets world position of chunk/cell address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static Vector2 AddressToPos(Vector2Int address)
        {
            var min = -Vector2.one / 2 * WorldSize;
            var pos = ((Vector2)address * ChunkSize) + min;
            return pos;
        }

        /// <summary>
        /// returns min and max of world box, margin param to shrink the box as needed
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="margin"></param>
        public static void GetWorldBox (out Vector2 min, out Vector2 max, float margin = 0)
        {
            max = Vector2.one * (WorldSize / 2 + margin);
            min = -max;
        }



        /// <summary>
        /// returns random position within world, margin param to shrink box as needed
        /// </summary>
        /// <param name="margin"></param>
        /// <returns></returns>
        public static Vector2 GetRandomPos (float margin = 0)
        {
            return -(Vector2.one * WorldSize / 2) + 
                new Vector2(
                    UnityEngine.Random.Range(margin, WorldSize - margin),
                    UnityEngine.Random.Range(margin, WorldSize - margin)
                );
        }

        /// <summary>
        /// returns world position to world boundaries ratio (used for minimap)
        /// </summary>
        /// <param name="worldPos"></param>
        /// <returns></returns>
        public static Vector2 GetPosRatio (Vector2 worldPos)
        {
            return worldPos / WorldSize + Vector2.one * .5f;
        }

        /// <summary>
        /// tests if the world position is within the world
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static bool IsPointInWorld(Vector2 point, bool debugLines = false)
        {
            //GetWorldBox(out var min, out var max);
            return GeoUtils.TestBoxPosSize(Vector2.zero, Vector2.one * WorldSize, point, debugLines);
        }

        /// <summary>
        /// returns ienumerable of all the addresses of chunks touched by a box with min corner and max corner
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static IEnumerable<Vector2Int> AddressesFromBoxMinMax(Vector2 min, Vector2 max)
        {
            var minAdress = PosToAdress(min);
            var maxAdress = PosToAdress(max);// + Vector2Int.one;

            var adressDelta = maxAdress - minAdress;

            //adressDelta = Vector2Int.Max(adressDelta, Vector2Int.one);

            //var output = new Vector2Int[adressDelta.x + 1, adressDelta.y + 1];

            for (int y = 0; y < adressDelta.y + 1; y++)
            {
                for (int x = 0; x < adressDelta.x + 1; x++)
                {
                    //output[x, y] = 
                    yield return new(x + minAdress.x, y + minAdress.y);
                }
            }

            //return output;
        }


        /// <summary>
        /// returns ienumerable of all addresses of chunks touched by a box at pos with size
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static IEnumerable<Vector2Int> AddressesFromBoxPosSize (Vector2 pos, Vector2 size)
        {
            GeoUtils.BoxPosSizeToMinMax(pos, size, out var min, out var max);
            return AddressesFromBoxMinMax(min, max);
        }


        /// <summary>
        /// returns ienumerable of all addresses of chunks touched by a line from pointA to pointB
        /// </summary>
        /// <param name="pointA"></param>
        /// <param name="pointB"></param>
        /// <param name="debugLines"></param>
        /// <returns></returns>
        public static IEnumerable<Vector2Int> AddressesFromLine(Vector2 pointA, Vector2 pointB, bool debugLines = false)
        {

            GetWorldBox(out var worldMin, out _);

            if (debugLines)
            {
                Debug.DrawLine(pointA, pointB, Color.green);
            }

            pointA = pointA - worldMin + (ChunkSize * .5f * Vector2.one);
            pointB = pointB - worldMin + (ChunkSize * .5f * Vector2.one);

            foreach (var cell in GeoUtils.CellsFromLine(pointA,pointB,ChunkSize))
            {
                //cell -= Vector2Int.one * chunkSize;

                yield return cell - Vector2Int.one;
            }
        }


        /// <summary>
        /// returns all addresses within world
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Vector2Int> AllAddresses ()
        {
            for (var x = 0; x < ChunkArraySize; x++)
            {
                for (var y = 0; y < ChunkArraySize; y++)
                {
                    yield return new Vector2Int(x, y);
                }
            }
        }
    }
}

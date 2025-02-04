using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Chunks
{
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

        public static T[,] GetChunkPairArray<T>()
        {
            var array = new T[ChunkArraySize, ChunkArraySize];

            return array;
        }

        public static Vector2Int PosToAdress(Vector2 pos)
        {
            var min = -Vector2.one / 2 * WorldSize;
            var delta = pos - min;
            var adress = Vector2Int.FloorToInt(delta / ChunkSize);
            adress = Vector2Int.Min(Vector2Int.one * (ChunkArraySize-1), adress);
            adress = Vector2Int.Max(Vector2Int.zero, adress);
            return adress;
        }

        public static Vector2 ClampPosToWorld (Vector2 pos)
        {
            return GeoUtils.ClampToBoxPosSize(pos, Vector2.zero, Vector2.one * WorldSize);
        }

        public static Vector2 AddressToPos(Vector2Int address)
        {
            var min = -Vector2.one / 2 * WorldSize;
            var pos = ((Vector2)address * ChunkSize) + min;
            return pos;
        }

        public static void GetWorldBox (out Vector2 min, out Vector2 max, float margin = 0)
        {
            max = Vector2.one * (WorldSize / 2 + margin);
            min = -max;
        }

        public static bool TestWorldBox (Vector2 pos)
        {
            GetWorldBox(out var min, out var max);
            return GeoUtils.TestBoxMinMax(min, max, pos);
        }

        public static Vector2 GetRandomPos (float margin = 0)
        {
            return -(Vector2.one * WorldSize / 2) + 
                new Vector2(
                    UnityEngine.Random.Range(margin, WorldSize - margin),
                    UnityEngine.Random.Range(margin, WorldSize - margin)
                );
        }

        public static Vector2 GetPosRatio (Vector2 worldPos)
        {
            return worldPos / WorldSize + Vector2.one * .5f;
        }

        public static bool IsPointInWorld(Vector2 point, bool debugLines = false)
        {
            //GetWorldBox(out var min, out var max);
            return GeoUtils.TestBoxPosSize(Vector2.zero, Vector2.one * WorldSize, point, debugLines);
        }

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

        public static IEnumerable<Vector2Int> AddressesFromBoxPosSize (Vector2 pos, Vector2 size)
        {
            GeoUtils.BoxPosSizeToMinMax(pos, size, out var min, out var max);
            return AddressesFromBoxMinMax(min, max);
        }

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

    public struct ChunkAddressPair<T>
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

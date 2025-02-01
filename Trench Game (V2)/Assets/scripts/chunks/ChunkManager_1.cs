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

        static float staticWorldSize;

        static float? chunkSize;
        public static int? chunkArraySize { get; private set; }

        public static void Initialize ()
        {
            staticWorldSize = Manager.worldSize;
            chunkArraySize = Mathf.FloorToInt(staticWorldSize / Manager.minChunkSize);
            chunkSize = Manager.worldSize / chunkArraySize;
        }

        private void Awake()
        {
            Initialize();
        }

        public static T[,] GetChunkPairArray<T>()
        {
            var array = new T[chunkArraySize.Value, chunkArraySize.Value];

            return array;
        }

        public static Vector2Int PosToAdress(Vector2 pos)
        {
            var min = -Vector2.one / 2 * staticWorldSize;
            var delta = pos - min;
            var adress = Vector2Int.FloorToInt(delta / chunkSize.Value);
            return adress;
        }
        public static Vector2 AddressToPos(Vector2Int address)
        {
            var min = -Vector2.one / 2 * staticWorldSize;
            var pos = ((Vector2)address * chunkSize.Value) + min;
            return pos;
        }

        public static void GetWorldBox (out Vector2 min, out Vector2 max)
        {
            max = Vector2.one * staticWorldSize / 2;
            min = -max;
        }

        public static bool IsPointInWorld(Vector2 point, bool debugLines = false)
        {
            //GetWorldBox(out var min, out var max);
            return GeoUtils.TestBoxPosSize(Vector2.zero, Vector2.one * staticWorldSize, point, debugLines);
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

        public static IEnumerable<Vector2Int> AddressesFromLine(Vector2 pointA, Vector2 pointB, bool newIfNone = false, bool debugLines = false)
        {

            GetWorldBox(out var worldMin, out _);

            if (debugLines)
            {
                Debug.DrawLine(pointA, pointB, Color.green);
            }

            pointA = pointA - worldMin + (chunkSize.Value * .5f * Vector2.one);
            pointB = pointB - worldMin + (chunkSize.Value * .5f * Vector2.one);

            foreach (var cell in GeoUtils.CellsFromLine(pointA,pointB,chunkSize.Value))
            {
                //cell -= Vector2Int.one * chunkSize;

                yield return cell - Vector2Int.one;
            }
        }

        public static IEnumerable<Vector2Int> AllAddresses ()
        {
            for (var x = 0; x < chunkArraySize; x++)
            {
                for (var y = 0; y < chunkArraySize; y++)
                {
                    yield return new Vector2Int(x, y);
                }
            }
        }
    }

    public struct ChunkAddressPair<T>
    {
        public Vector2Int address { get; private set; }
        public T obj;

        public ChunkAddressPair(Vector2Int address, T obj)
        {
            this.address = address;
            this.obj = obj;
        }
    }
}

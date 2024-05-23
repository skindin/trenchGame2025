//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class TrenchManager : MonoBehaviour
//{
//    public List<Line> lines = new();
//    // Start is called before the first frame update
//    static TrenchManager manager;

//    public static TrenchManager Manager
//    {
//        get
//        {
//            if (manager == null)
//            {
//                manager = FindObjectOfType<TrenchManager>();
//                if (manager == null)
//                {
//                    GameObject go = new GameObject("GameManager");
//                    manager = go.AddComponent<TrenchManager>();
//                    DontDestroyOnLoad(go);
//                }
//            }
//            return manager;
//        }
//    }

//    void Start()
//    {

//    }

//    // Update is called once per frame
//    void LateUpdate()
//    {
//        //update maps and textures

//        lines.Clear();
//    }

//    public void AddLine (Vector2 pointA, float radA, Vector2 pointB, float radB, bool fill)
//    {
//        lines.Add(new(pointA, radA, pointB, radB, fill));
//    }

//    public void UpdateTrenchMaps ()
//    {

//    }

//public void DrawLine(Vector2 pointA, float radA, Vector2 pointB, float radB)
//{
//    GetBlockAddressesFromLine(pointA, radA, pointB, radB, out var minAddress, out var maxAddress);
//    minAddress /= 4;
//    maxAddress = Vector2Int.Min(maxAddress / 4, (size - 1) * Vector2Int.one);

//    for (int blockY = minAddress.y; blockY <= maxAddress.y; blockY++)
//    {
//        for (int blockX = minAddress.x; blockX <= maxAddress.x; blockX++)
//        {
//            var block = blocks[blockX, blockY];

//            if (block.IsFull()) continue;

//            for (int bitY = 0; bitY < 4; bitY++)
//            {
//                var y = blockY * 4 + bitY;

//                for (int bitX = 0; bitX < 4; bitX++)
//                {
//                    var x = blockX * 4 + bitX;

//                    if (block.GetPoint(bitX, bitY)) continue;

//                    if (IsPointWithinLine(new Vector2(x, y), pointA, radA, pointB, radB))
//                    {
//                        block.SetPoint(true, bitX, bitY);
//                        blocks[blockX, blockY] = block;
//                    }
//                }
//            }
//        }
//    }
//}

//    public void DrawPoint(Vector2 point, float radius)
//    {
//        DrawLine(point, radius, point, radius);
//    }

//    //public void EraseLine(Vector2 pointA, float radA, Vector2 pointB, float radB)
//    //{
//    //    GetBlockAddressesFromLine(pointA, radA, pointB, radB, out var minAddress, out var maxAddress);
//    //    minAddress /= 4;
//    //    maxAddress = Vector2Int.Min(maxAddress / 4, (size - 1) * Vector2Int.one);

//    //    for (int blockY = minAddress.y; blockY <= maxAddress.y; blockY++)
//    //    {
//    //        for (int blockX = minAddress.x; blockX <= maxAddress.x; blockX++)
//    //        {
//    //            //var block = blocks[blockX, blockY];

//    //            if (block.IsEmpty()) continue;

//    //            for (int bitY = 0; bitY < 4; bitY++)
//    //            {
//    //                var y = blockY * 4 + bitY;

//    //                for (int bitX = 0; bitX < 4; bitX++)
//    //                {
//    //                    var x = blockX * 4 + bitX;

//    //                    if (IsPointWithinLine(new Vector2(x, y), pointA, radA, pointB, radB))
//    //                    {
//    //                        block.SetPoint(false, bitX, bitY);
//    //                        blocks[blockX, blockY] = block;
//    //                    }
//    //                }
//    //            }
//    //        }
//    //    }
//    //}

//    //public void ErasePoint(Vector2 point, float radius)
//    //{
//    //    EraseLine(point, radius, point, radius);
//    //}

//    private void GetBlockAddressesFromLine(Vector2 pointA, float radA, Vector2 pointB, float radB, out Vector2Int minAddress, out Vector2Int maxAddress)
//    {
//        GetLineBounds(pointA, radA, pointB, radB, out var min, out var max);

//        min = Vector2.Max(min, Vector2.zero);
//        //max = Vector2.Min(max, size * 4 * Vector2.one);

//        minAddress = Vector2Int.FloorToInt(min);
//        maxAddress = Vector2Int.CeilToInt(max);

//        GeoFuncs.DrawBox(minAddress, maxAddress, Color.cyan);
//    }

//    private void GetLineBounds(Vector2 pointA, float radA, Vector2 pointB, float radB, out Vector2 min, out Vector2 max)
//    {
//        min = Vector2.Min(pointA - Vector2.one * radA, pointB - Vector2.one * radB);
//        max = Vector2.Max(pointA + Vector2.one * radA, pointB + Vector2.one * radB);

//        GeoFuncs.DrawBox(min, max, Color.magenta);
//    }

//    private bool IsPointWithinLine(Vector2 testPoint, Vector2 pointA, float radA, Vector2 pointB, float radB, bool debugLines = false)
//    {
//        if (debugLines)
//        {
//            Debug.DrawLine(pointA, pointB, Color.green);
//            GeoFuncs.DrawCircle(pointA, radA, Color.green);
//            GeoFuncs.DrawCircle(pointB, radB, Color.green);
//        }

//        var edgeDist = Vector2.Distance(pointA, pointB) - Mathf.Abs(radA - radB);
//        if (edgeDist <= 0)
//        {
//            return IsPointWithinLargestCircle(testPoint, pointA, radA, pointB, radB, debugLines);
//        }

//        var closestPoint = GeoFuncs.ClosestPointToLineSegment(testPoint, pointA, pointB, out var ratio);
//        var dist = Vector2.Distance(closestPoint, testPoint);

//        var radius = Mathf.Lerp(radA, radB, ratio);
//        var within = dist <= radius;
//        if (debugLines)
//        {
//            GeoFuncs.DrawCircle(closestPoint, radius, Color.blue);
//            GeoFuncs.MarkPoint(testPoint, 0.5f, within ? Color.green : Color.red);
//        }

//        return within;
//    }

//    private bool IsPointWithinLargestCircle(Vector2 testPoint, Vector2 pointA, float radA, Vector2 pointB, float radB, bool debugLines)
//    {
//        Vector2 largestCircleCenter = radA > radB ? pointA : pointB;
//        float largestRadius = Mathf.Max(radA, radB);

//        var withinLargestCircle = Vector2.Distance(testPoint, largestCircleCenter) <= largestRadius;

//        if (debugLines)
//        {
//            GeoFuncs.DrawCircle(largestCircleCenter, largestRadius, Color.blue);
//            GeoFuncs.MarkPoint(testPoint, 0.5f, withinLargestCircle ? Color.green : Color.red);
//        }

//        return withinLargestCircle;
//    }

//    public void GetLineBounds(Vector2 testPoint, Vector2 pointA, float radA, Vector2 pointB, float radB, out Vector2 min, out Vector2 max)
//    {
//        min = Vector2.Min(pointA - Vector2.one * radA, pointB - Vector2.one * radB);
//        max = Vector2.Max(pointA + Vector2.one * radA, pointB + Vector2.one * radB);

//        GeoFuncs.DrawBox(min, max, Color.magenta);
//    }
//}

//public struct Line
//{
//    public Vector2 pointA, pointB;
//    public float radA, radB;
//    public bool fill;

//    public Line (Vector2 pointA, float radA, Vector2 pointB, float radB, bool fill)
//    {
//        this.pointA = pointA;
//        this.radA = radA;
//        this.pointB = pointB;
//        this.radB = radB;
//        this.fill = fill;
//    }
//}

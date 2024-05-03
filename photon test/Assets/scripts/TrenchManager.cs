using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrenchManager : MonoBehaviour
{
    public static TrenchManager instance;
    public Trench trenchPrefab;
    public bool debugLines = false;
    public float digPointDist = 2, connectDist = 1, maxTrenchArea = 50;

    public List<Trench> allTrenches = new();
    public List<TrenchChunk> chunks = new();
    public float chunkSize = 50;

    private void Awake()
    {
        if (!instance) instance = this;
    }

    private void Update()
    {
        if (!instance) instance = this;
    }

    //I was gonna move this to trench script but I'd rather the debugLines property stay here so nvm
    public bool CheckWithinTrench (Vector2 pos)
    {
        if (!instance) instance = this;

        var chunk = ChunkFromPos(pos, false);

        if (chunk == null) return false;

        if (debugLines) DrawChunk(chunk, Color.green);

        foreach (var trench in chunk.trenches)
        {
            if (debugLines) trench.DrawBox();

            if (trench.TestBox(pos) && trench.line.positionCount > 0)
            {
                if (trench.line.positionCount == 1)
                {
                    var pointA = trench.line.GetPosition(0) + trench.transform.position;
                    var dist = Vector2.Distance(pointA, pos);
                    if (dist <= trench.line.widthMultiplier / 2)
                    {
                        return true;
                    }
                }

                if (trench.line.positionCount > 1)
                {
                    Vector2 closestPoint = Vector2.zero;

                    Vector2 pointA = trench.line.GetPosition(0) + trench.transform.position;
                    Vector2 pointB;

                    bool withinTrench = false;

                    for (var i = 1; i < trench.line.positionCount; i++)
                    {
                        pointB = trench.line.GetPosition(i) + trench.transform.position;

                        if (debugLines) Debug.DrawLine(pointA, pointB, Color.red);

                        var pointOnSegment = ClosestPointToLineSegment(pos, pointA, pointB);

                        var dist = Vector2.Distance(pos, pointOnSegment);

                        if (dist <= trench.line.widthMultiplier/2)
                        {
                            closestPoint = pointOnSegment;
                            withinTrench = true;
                            break;
                        }

                        if (i < trench.line.positionCount - 1)
                        {
                            pointA = pointB;
                        }
                        else
                        {
                            closestPoint = pointB;
                        }
                    }

                    var color = Color.green;

                    //if (!withinTrench) color = Color.red;

                    if (debugLines && withinTrench) Debug.DrawLine(pos, closestPoint, color);

                    if (withinTrench) return true;
                }
            }
        }

        return false;
    }

    Vector2 ClosestPointToLineSegment(Vector2 objectPos, Vector2 lineStart, Vector2 lineEnd)
    {

        if (lineStart == lineEnd) return lineStart;
        // Calculate the squared length of the line segment
        float lineLengthSquared = Mathf.Pow(lineEnd.x - lineStart.x, 2) + Mathf.Pow(lineEnd.y - lineStart.y, 2);

        // Calculate the parameter (t) of the closest point to the line segment
        float t = Mathf.Max(0, Mathf.Min(1, Vector2.Dot(objectPos - lineStart, lineEnd - lineStart) / lineLengthSquared));

        // Calculate the closest point on the line segment
        Vector2 closestPoint = lineStart + t * (lineEnd - lineStart);

        return closestPoint;
    }

    public Trench Dig (Vector2 digPos, Trench trench = null, float startWidth = 1)
    {
        //if (!trench) trench = FindTrenchEnd(digPos);

        if (!trench)
        {
            trench = NewTrench();
            trench.SetWidth(startWidth);
        }

        trench.AddPoint(digPos);
        AssignChunks(trench); //not ideal for performance but mehh

        if (trench.area > maxTrenchArea)
        {
            var splitIndex = trench.line.positionCount -2;

            SplitTrench(trench, splitIndex);
        }

        //if (debugLines) trench.DrawBox(); //this flickers, probably because it's only run every .2 units

        return trench;
    }

    public Trench NewTrench ()
    {
        var trench = Instantiate(trenchPrefab, transform).GetComponent<Trench>();
        allTrenches.Add(trench);
        return trench;
    }

    public void RemoveTrench (Trench trench)
    {
        allTrenches.Remove(trench);
        Destroy(trench.gameObject, .01f);
    }

    public Trench SplitTrench(Trench activeTrench, int index, bool removePoint = false)
    {
        if (index < 0) return null;

        Trench firstTrench = null;

        if (index != 0)
        {
            firstTrench = NewTrench();
            firstTrench.SetWidth(activeTrench.width);
            //needs to return a different trench because the current trench may be being edited

            var firstTrenchCount = index + 1;
            if (removePoint) firstTrenchCount -= 1;

            for (var i = 0; i < firstTrenchCount; i++)
            {
                var point = activeTrench.line.GetPosition(i);
                firstTrench.AddPoint(point);
            }
        }
        AssignChunks(firstTrench); //probably is ideal for instantiation code like this

        var activeTrenchStart = index;

        if (removePoint) activeTrenchStart++;

        var activeTrenchCount = activeTrench.line.positionCount - activeTrenchStart;

        for (var i = 0; i < activeTrenchCount; i++)
        {
            var point = activeTrench.line.GetPosition(i + activeTrenchStart);
            activeTrench.line.SetPosition(i, point);
        }

        activeTrench.line.positionCount = activeTrenchCount;

        activeTrench.CalculateBox();
        UnassignChunks(activeTrench);//also probably ideal
        AssignChunks(activeTrench);

        return firstTrench;
    }

    public TrenchChunk ChunkFromPos(Vector2 pos, bool newIfNone = true)
    {
        var coords = Vector2Int.RoundToInt(pos / chunkSize);
        return ChunkFromCoords(coords, newIfNone);
    }

    public TrenchChunk ChunkFromCoords(Vector2Int coords, bool newIfNone = true)
    {
        foreach (var chunk in chunks)
        {
            if (debugLines) DrawChunk(chunk, Color.red);

            if (chunk.coords == coords)
            {
                return chunk;
            }
        }

        if (newIfNone)
        {
            var newChunk = new TrenchChunk(coords);
            chunks.Add(newChunk);
            return newChunk;
        }

        return null;
    }

    public List<TrenchChunk> ChunksFromBox (Vector2 min, Vector2 max, List<TrenchChunk> chunks, bool newIfNone = true)
    {
        var intMin = Vector2Int.RoundToInt(min / chunkSize);
        var intMax = Vector2Int.RoundToInt(max / chunkSize);
        
        for (var y = intMin.y; y < intMax.y+1; y++)
        {
            for (var x = intMin.x; x < intMax.x + 1; x++)
            {
                var coords = new Vector2Int(x, y);
                var chunk = ChunkFromCoords(coords,newIfNone);
                if (chunk != null && !chunks.Contains(chunk)) chunks.Add(chunk);
            }
        }

        return chunks;
    }

    /// <summary>
    /// Calculates chunks.
    /// Should only be used after a trench has gone through significant change without acknowledging chunks
    /// </summary>
    /// <param name="trench"></param>
    public void AssignChunks (Trench trench)
    {
        var chunks = trench.chunks = ChunksFromBox(trench.boxMin, trench.boxMax, trench.chunks);

        foreach (var chunk in chunks)
        {
            chunk.trenches.Add(trench);
        }
    }

    public void UnassignChunks(Trench trench, bool removeEmptyChunks = false)
    {
        for (var i = 0; i < trench.chunks.Count; i++)
        {
            var chunk = trench.chunks[i];
            chunk.trenches.Remove(trench);

            if (chunk.trenches.Count == 0 && removeEmptyChunks)
            {
                chunks.Remove(chunk);
            }
        }

        trench.chunks.Clear();
    }

    public void DrawChunk(TrenchChunk chunk, Color color = default)
    {
        if (color == default) color = Color.white;

        var center = (Vector2)chunk.coords * chunkSize;
        var halfChunk = chunkSize / 2;
        var maxDelta = Vector2.one * halfChunk;

        var topRight = center + maxDelta;
        var bottomLeft = center - maxDelta;
        var bottomRight = new Vector2(topRight.x, bottomLeft.y);
        var topLeft = new Vector2(bottomLeft.x, topRight.y);

        Debug.DrawLine(topRight, bottomRight, color);
        Debug.DrawLine(bottomRight, bottomLeft, color);
        Debug.DrawLine(bottomLeft, topLeft, color);
        Debug.DrawLine(topLeft, topRight, color);
    }
}

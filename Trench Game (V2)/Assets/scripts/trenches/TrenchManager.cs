using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.PlayerSettings;
using UnityEngine.UIElements;

public class TrenchManager : ManagerBase<TrenchManager>
{
    public float maxPixelSize = .01f;
    int mapResolution;
    public Color32 trenchColor, groundColor;
    public Mesh imageMesh;
    public Material imageMaterial;//
    public FilterMode filter;
    public Transform pointA, pointB;
    public bool
        runTest = false,
        drawRayTests = false, drawFillLines = false, drawStatusTests = false, drawCollisionTests = false, drawAllBits = false, logWallSlides = false;//, doSpread = false;

    public float blockWidth, bitWidth, testRadius = 1, frictionAngleMod = 1, wallFriction = 0;
    public int maxSlides = 5, targetFPS = 120;

    private void Start()
    {
        //mapResolution = ChunkManager.Manager.chunkSize //determine resolution with maxPixelSize

        mapResolution = Mathf.CeilToInt(ChunkManager.Manager.chunkSize / maxPixelSize/4);

        blockWidth = ChunkManager.Manager.chunkSize / mapResolution;

        bitWidth = blockWidth / 4;
    }

    private void Update()
    {
        Application.targetFrameRate = targetFPS;

        //if (doSpread)
        //{
        //    Spread(false, 1, 1);
        //    doSpread = false;
        //}

        if (runTest)
        {
            //TestRayHitsValue(pointA.position, pointB.position, false, out _, true);

            StopAtValue(pointA.position, pointB.position, testRadius, false);
        }

        if (drawAllBits)
        {
            DrawAllBits();
        }

        foreach (var chunk in ChunkManager.Manager.chunks)
        {
            if (chunk != null && chunk.map != null)
            {
                chunk.map.Draw();
            }
        }
    }

    public void SetTaperedCapsule(Vector2 startPoint, float startRadius, Vector2 endPoint, float endRadius, bool value, float maxArea, out float areaChanged)
    {
        var startMax = Vector2.one * startRadius;
        var endMax = Vector2.one * endRadius;

        var capsuleMin = Vector2.Min(startPoint - startMax, endPoint - endMax);
        //capsuleMin = Vector2.Max(capsuleMin, mapMin);

        var capsuleMax = Vector2.Max(startPoint + startMax, endPoint + endMax);
        //capsuleMax = Vector2.Min(capsuleMax, mapMax);

        var chunks = ChunkManager.Manager.ChunksFromBoxMinMax(capsuleMin, capsuleMax, value);

        areaChanged = 0;

        foreach (var chunk in chunks)
        {
            if (chunk == null)
                continue;

            if (chunk.map == null)
            {
                if (value)
                {
                    var pos = ChunkManager.Manager.GetChunkPos(chunk) + ChunkManager.Manager.chunkSize * .5f * Vector2.one;

                    chunk.map = new(mapResolution,ChunkManager.Manager.chunkSize, trenchColor, groundColor,pos,imageMesh,imageMaterial,filter);
                }
                else
                {
                    continue;
                }
            }

            chunk.map.SetTaperedCapsule(startPoint, startRadius, endPoint, endRadius, value, maxArea, out var areaChangedMap, drawFillLines);

            areaChanged += areaChangedMap;
            maxArea -= areaChangedMap;

            //Debug.Log($"chunk {chunk.adress} has {chunk.map.totalEditedBlocks} edited blocks");

            if (chunk.map.allFull)
            {
                chunk.RemoveTrenchMap();
            }
        }
    }

    public bool TestRayHitsValue(Vector2 startPoint, Vector2 endPoint, bool value, out Vector2 hitPoint, bool logTotal = false)
    {
        if (drawRayTests)
        {
            Debug.DrawLine(startPoint, endPoint, Color.blue);
        }

        hitPoint = endPoint;

        var bitCorner = bitWidth * .5f * Vector2.one;

        startPoint -= bitCorner;
        endPoint -= bitCorner;

        //return GeoUtils.ForeachCellTouchingLine<bool>(startPoint, endPoint, bitWidth, null, returnCondition, returnCondition, out _, logTotal);

        var lineToCells = GeoUtils.CellsFromLine(startPoint, endPoint, bitWidth);

        foreach (var cell in lineToCells)
        {
            var pos = (Vector2)cell * bitWidth;

            var chunk = ChunkManager.Manager.ChunkFromPos(pos);

            if (chunk == null || chunk.map == null)
            {
                if (drawRayTests)
                    GeoUtils.DrawBoxPosSize(pos + bitCorner, bitWidth * Vector2.one,
                        !value ? Color.green : Color.white);

                if (value)
                    continue;
                else
                {
                    hitPoint = GetIntersectDistance(pos);
                    return true;
                }
            }
            else if (drawRayTests)
            {

            }

            if (chunk.map.allTrench)
            {
                if (value)
                {
                    hitPoint = GetIntersectDistance(pos);
                    return true;
                }
                else
                {
                    continue;
                }
            }

            var blockAdress = GetBlockAdressFloored(pos, chunk.map.pos);

            //if (debugLines)
            //    GeoUtils.DrawBoxPosSize(pos, bitWidth * Vector2.one, Color.green);

            var block = chunk.map.GetBlock(blockAdress);

            var blockPos = GetBlockPos(chunk.map.pos, blockAdress);

            if (block == null)
            {
                if (drawRayTests)
                {
                    var color = !value ? Color.green : Color.white;
                    GeoUtils.DrawBoxPosSize(blockPos, blockWidth * Vector2.one, color);
                    GeoUtils.DrawBoxPosSize(pos+ bitCorner, bitWidth * Vector2.one, color);
                }

                if (value)
                    continue;
                else
                {
                    hitPoint = GetIntersectDistance(pos);
                    return true;
                }
            }

            if (block.TestWhole(value))
            {
                if (drawRayTests)
                {
                    GeoUtils.DrawBoxPosSize(blockPos, blockWidth * Vector2.one, Color.green);
                    GeoUtils.DrawBoxPosSize(pos + bitCorner, bitWidth * Vector2.one, Color.green);
                }

                hitPoint = GetIntersectDistance(pos);
                return true;
            }

            if (drawRayTests)
            {
                GeoUtils.DrawBoxPosSize(blockPos, blockWidth * Vector2.one, Color.red);
            }

            var bitAdress = GetBitAdressFloored(pos+ bitCorner, blockPos);
            //bitAdress = Vector2Int.Max(bitAdress, Vector2Int.zero);

            //if (bitAdress.x < 0 || bitAdress.y < 0)
            //    continue;

            if (block[bitAdress] == value)
            {
                if (drawRayTests)
                {
                    GeoUtils.DrawBoxPosSize(pos + bitCorner, bitWidth * Vector2.one, Color.green);
                    GeoUtils.MarkPoint(pos + bitCorner, bitWidth / 2, Color.green);
                }

                hitPoint = GetIntersectDistance(pos);
                return true;
            }
            else if (drawRayTests)
            {
                GeoUtils.DrawBoxPosSize(pos + bitCorner, bitWidth * Vector2.one, Color.red);
            }
        }

        Vector2 GetIntersectDistance (Vector2 point)
        {
            var vector = (endPoint - startPoint).normalized * bitWidth;

            var perp = Vector2.Perpendicular(vector);

            point += bitCorner;

            point -= vector;

            var pointA = point - perp;

            var pointB = point + perp;

            return GeoUtils.FindIntersection(startPoint - vector*2 + bitCorner, point + vector*2 + bitCorner, pointA, pointB, drawRayTests);
        }

        return false;
    }

    public bool TestCircleTouchesValue(Vector2 circlePos, float circleRadius, bool value)
    {
        var min = circlePos - circleRadius * Vector2.one;
        var max = circlePos + circleRadius * Vector2.one;

        var chunks = ChunkManager.Manager.ChunksFromBoxMinMax(min,max, false);

        foreach (var chunk in chunks)
        {
            if (chunk == null || chunk.map == null)
            {
                if (value)
                {
                    continue;
                }
                else
                {
                    return true;
                }
            }

            if (chunk.map.TestCircleTouchesValue(circlePos, circleRadius, value, drawStatusTests))
                return true;
        }

        return false;
    }

    public Vector2 StopAtValue(Vector2 start, Vector2 end, float radius, bool value)
    {
        // Tracks the number of slides (iterations) during the calculation
        var slideCount = 0;

        // The current start and end points for each iteration
        var currentStart = start;
        var currentEnd = end;

        // Original delta (difference) between start and end points
        var ogDelta = currentEnd - currentStart;

        // Total magnitude of movement processed so far
        float totalMagnitude = 0;

        // Initial movement direction, normalized to a unit vector
        var direction = (end - start).normalized;

        // Loop to simulate sliding along obstacles until maxSlides is reached
        while (slideCount < maxSlides)
        {
            // Variables to track the closest collision point and related information
            float smallestDist = Mathf.Infinity;
            Vector2 closestCirclePos = currentEnd;
            Vector2 closestCollisionPoint = currentEnd;

            // Iterate through chunks to find potential collision points
            foreach (var chunk in ChunkManager.Manager.chunks)
            {
                if (chunk == null || chunk.map == null)
                {
                    continue; // Skip null or invalid chunks
                }

                // Get points in the chunk that obstruct the capsule-shaped area
                var points = chunk.map.GetBitsObstructingTaperedCapsule(currentStart, radius, currentEnd, radius, value);

                // Process each obstruction point
                foreach (var point in points)
                {
                    var pointDelta = point - currentStart;

                    // Calculate the dot product to ensure the point is in the forward direction
                    var collisionPointDot = Vector2.Dot(direction.normalized, pointDelta);
                    if (collisionPointDot <= 0)
                        continue; // Skip points behind the starting position

                    // Calculate the perpendicular distance to the collision point
                    var b = radius;
                    var c = Mathf.Abs(Vector2.Dot(Vector2.Perpendicular(direction), point - currentStart));
                    var a = Mathf.Sqrt((b * b) - (c * c));

                    // Determine the circle's collision position
                    var circleStartDelta = (collisionPointDot - a) * direction;
                    var circlePos = circleStartDelta + currentStart;

                    // Update the closest collision point if this is the nearest so far
                    var distance = circleStartDelta.magnitude;
                    if (distance < smallestDist)
                    {
                        if (drawCollisionTests)
                        {
                            GeoUtils.MarkPoint(point, bitWidth * 0.5f, new Color(0, 1, 0, 0.2f));
                        }

                        closestCirclePos = circlePos;
                        smallestDist = distance;
                        closestCollisionPoint = point;
                        continue;
                    }

                    // Optionally visualize points that are further away
                    if (drawCollisionTests)
                    {
                        GeoUtils.MarkPoint(point, bitWidth * 0.5f, new Color(1, 0, 0, 0.2f));
                    }
                }
            }

            // Debug visuals for collision testing
            if (drawCollisionTests)
            {
                var transparentGreen = new Color(0, 1, 0, 1);
                Debug.DrawRay(currentStart, direction, Color.blue);
                GeoUtils.DrawCircle(currentStart, radius, Color.blue);
                GeoUtils.DrawCircle(closestCirclePos, radius, transparentGreen);
                GeoUtils.MarkPoint(closestCollisionPoint, bitWidth / 2, transparentGreen);
            }

            // If no collision was found, return the current end position
            if (smallestDist == Mathf.Infinity)
            {
                if (logWallSlides)
                    Debug.Log($"{slideCount} slides");

                return currentEnd;
            }

            // Calculate the new direction based on the collision point
            totalMagnitude += (closestCirclePos - currentStart).magnitude;
            var collisionPointDelta = (closestCollisionPoint - closestCirclePos);
            var collisionDot = Vector2.Dot(Vector2.Perpendicular(direction), collisionPointDelta);

            direction = Vector2.Perpendicular(collisionPointDelta).normalized;
            if (-collisionDot < 0)
            {
                direction = -direction;
            }

            // Stop sliding if the direction is opposite to the original movement
            if (Vector2.Dot(ogDelta, direction) < 0)
            {
                if (logWallSlides)
                    Debug.Log($"{slideCount} slides");

                return closestCirclePos;
            }

            // Apply friction and calculate the deflection direction
            var angleRatio = (1 - frictionAngleMod * (Vector2.Angle(ogDelta, direction) / 90)) * (1 - wallFriction);
            var magnitude = Mathf.Min(radius, (ogDelta.magnitude - totalMagnitude) * angleRatio);

            var deflectionDir = direction * magnitude;

            // Update the current start and end positions for the next iteration
            currentStart = closestCirclePos;
            currentEnd = currentStart + deflectionDir;

            // Increment the slide count
            slideCount++;
        }

        // Log the total number of slides if necessary
        if (logWallSlides)
            Debug.Log($"{slideCount} slides");

        return currentEnd;
    }

    //public void Spread(bool value, float minAmount, float maxAmount)
    //{
    //    var chunkManager = ChunkManager.Manager;

    //    int arraySize = chunkManager.chunkArraySize;
    //    var minBits = minAmount / bitWidth;
    //    var maxBits = maxAmount / bitWidth;

    //    var chunkArray = chunkManager.chunks;

    //    bool somethingChanged = false;

    //    for (var chunkY = 0; chunkY < arraySize; chunkY++)
    //    {
    //        for (var chunkX = 0; chunkX < arraySize; chunkX++)
    //        {
    //            var chunk = chunkArray[chunkX,chunkY];

    //            var chunkBelow = chunkY - 1 >= 0 ? chunkManager.ChunkFromPos(new(chunkX,chunkY-1),value) : null;
    //            var chunkToLeft = chunkX - 1 >= 0 ? chunkManager.ChunkFromPos(new(chunkX - 1, chunkY), value) : null;

    //            //if (chunk != null) //if we're going to edit this eventually...
    //            //{
    //            //    if (chunk.map == null) //if there is no map data (completely full)...
    //            //    {
    //            //        if (value) //if we are trying to spread trenches...
    //            //        {

    //            //        }
    //            //        else
    //            //        {
    //            //            continue;
    //            //        }
    //            //    }
    //            //}
    //            //continue;

    //            //if (chunk == null || chunk.map == null)
    //            //    continue; //for now...

    //            //var blockArray = chunk.map.blocks;
    //            var map = chunk.map;

    //            for (var blockY = 0; blockY < mapResolution; blockY++)
    //            {
    //                for (var blockX = 0; blockX < mapResolution; blockX++)
    //                {
    //                    var block = map.blocks[blockX, blockY];
    //                    var blockBelow = blockY - 1 >= 0
    //                        ? map.blocks[blockX, blockY - 1] //if in this chunk...
    //                        : chunkBelow != null
    //                            ? chunkBelow.map.blocks[blockX, mapResolution - 1]
    //                            : null;

    //                    var blockToLeft = blockX - 1 >= 0
    //                        ? map.blocks[blockX-1, blockY] //if in this chunk...
    //                        : chunkToLeft != null
    //                            ? chunkToLeft.map.blocks[mapResolution - 1, blockY]
    //                            : null;

    //                    if (block == null)
    //                        continue; //for now...

    //                    for (var bitY = 0; bitY < 4; bitY++)
    //                    {
    //                        for (var bitX = 0; bitX < 4; bitX++)
    //                        {
    //                            var bitValue = block[bitX, bitY];

    //                            if (bitValue == value)
    //                            {

    //                                continue;
    //                            }

    //                            var bitBelow = bitY - 1 >= 0
    //                                ? block[bitX, bitY - 1]
    //                                : blockBelow != null
    //                                    ? blockBelow[bitX, 3]
    //                                    : !value;

    //                            var bitToLeft = bitX - 1 >= 0
    //                                ? block[bitX - 1, bitY]
    //                                : blockToLeft != null
    //                                    ? blockToLeft[3, bitY]
    //                                    : !value;

    //                            if (bitBelow == value || bitToLeft == value)
    //                            {
    //                                map.SetBit(new(blockX, blockY), new(bitX, bitY), value);
    //                                somethingChanged = true;
    //                            }
    //                        }
    //                    }
    //                }
    //            }

    //            if (somethingChanged)
    //            {
    //                map.ApplyPixels();
    //            }
    //        }
    //    }
    //}

    public bool TestPoint (Vector2 point)
    {
        var chunk = ChunkManager.Manager.ChunkFromPos(point);

        if (chunk == null || chunk.map == null)
            return false;

        return chunk.map.TestPoint(point);
    }

    public void DrawAllBits ()
    {
        var trenchBits = GetBits(true);

        foreach (var bit in trenchBits)
        {
            GeoUtils.DrawBoxPosSize(bit, bitWidth * Vector2.one,Color.green);
        }
    }

    public IEnumerable<Vector2> GetBits (bool value)
    {
        foreach (var chunk in ChunkManager.Manager.chunks)
        {
            if (chunk == null || chunk.map == null)
                continue;

            for (var blockY = 0; blockY < mapResolution; blockY++)
            {
                for (var blockX = 0; blockX < mapResolution; blockX++)
                {
                    var block = chunk.map.blocks[blockX, blockY];

                    var blockPos = GetBlockPos(chunk.map.pos, new Vector2Int(blockX, blockY));

                    if (block == null)
                        continue;

                    for (var bitY = 0; bitY < 4; bitY++)
                    {
                        for (var bitX = 0; bitX < 4; bitX++)
                        {
                            if (block[bitX,bitY] == value)
                            {
                                yield return GetBitPos(blockPos, new(bitX, bitY));
                            }
                        }
                    }
                }
            }
        }
    }

    public Vector2 GetBlockPos(Vector2 mapPos, Vector2Int blockAdress)
    {
        return (new Vector2(blockAdress.x + 0.5f, blockAdress.y + 0.5f) - mapResolution * .5f * Vector2.one) * blockWidth
                    + mapPos;
    }

    public Vector2 GetBitPos(Vector2 blockPos, Vector2Int bitAdress)
    {
        return blockPos + new Vector2((bitAdress.x - 1.5f) * bitWidth, (bitAdress.y - 1.5f) * bitWidth);
    }

    public Vector2 GetBlockAdressPoint(Vector2 pos, Vector2 mapPos)
    {
        return ((pos - mapPos) / blockWidth) + (mapResolution * .5f * Vector2.one);
    }

    public Vector2Int GetBlockAdressFloored(Vector2 pos, Vector2 mapPos)
    {
        return Vector2Int.FloorToInt(GetBlockAdressPoint(pos, mapPos));
    }

    public Vector2Int GetBlockAdressCield(Vector2 pos, Vector2 mapPos)
    {
        return Vector2Int.CeilToInt(GetBlockAdressPoint(pos, mapPos));
    }

    public Vector2Int GetBlockAdressRounded(Vector2 pos, Vector2 mapPos)
    {
        return Vector2Int.RoundToInt(GetBlockAdressPoint(pos, mapPos));
    }

    public Vector2 GetBitAdressPoint(Vector2 pos, Vector2 blockPos)
    {
        return (pos - blockPos + .5f * blockWidth * Vector2.one) / bitWidth;
    }

    public Vector2Int GetBitAdressFloored (Vector2 pos, Vector2 blockPos)
    {
        var point = GetBitAdressPoint(pos, blockPos);
        var floored = Vector2Int.Min(Vector2Int.FloorToInt(point),Vector2Int.one * 3);

        return floored;
    }
}

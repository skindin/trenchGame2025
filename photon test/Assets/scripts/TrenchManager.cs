using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class TrenchManager : MonoBehaviourPunCallbacks
{
    public static TrenchManager instance;
    public Trench trenchPrefab;
    public bool debugLines = false;
    public float digPointDist = 2, connectDist = 1;

    public List<Trench> trenches;

    private void Awake()
    {
        if (!instance) instance = this;
    }

    private void Update()
    {
        if (!instance) instance = this;
    }

    public bool CheckWithinTrench (Vector2 pos)
    {
        if (!instance) instance = this;

        foreach (var trench in trenches)
        {
            if (trench.line.positionCount == 0)
            {
                break;
            }

            if (trench.line.positionCount == 1)
            {
                var pointA = trench.line.GetPosition(0)+trench.transform.position;
                var dist = Vector2.Distance(pointA, pos);
                if (dist <= trench.line.widthMultiplier/2)
                {
                    return true;
                }
            }

            if (trench.line.positionCount > 1)
            {
                var closestDist = Mathf.Infinity;
                Vector2 closestPoint = Vector2.zero;

                Vector2 pointA;
                Vector2 pointB = trench.line.GetPosition(1)+trench.transform.position;

                for (var i = 0; i < trench.line.positionCount-1; i++)
                {
                    pointA = pointB;
                    pointB = trench.line.GetPosition(i+1)+trench.transform.position;

                    if (debugLines) Debug.DrawLine(pointA, pointB);

                    var pointOnSegment = ClosestPointToLineSegment(pos, pointA, pointB);

                    var dist = Vector2.Distance(pos, pointOnSegment);

                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestPoint = pointOnSegment;
                    }
                }

                var color = Color.green;
                var withinTrench = closestDist <= trench.line.widthMultiplier / 2;

                if (!withinTrench) color = Color.red;

                if (debugLines) Debug.DrawLine(pos, closestPoint, color);

                if (withinTrench) return true;
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
            trench.line.widthMultiplier = startWidth;
        }

        int currentPointCount = trench.line.positionCount;

        // Increase the position count by one
        trench.line.positionCount = currentPointCount + 1;

        // Set the position of the new point
        trench.line.SetPosition(currentPointCount, digPos);

        return trench;
    }

    public Trench FindTrenchEnd (Vector2 pos)
    {
        foreach (var trench in trenches)
        {
            var endIndex = trench.line.positionCount - 1;
            var endPos = trench.line.GetPosition(endIndex);
            var dist = Vector2.Distance(pos, endPos);
            if (dist <= connectDist)
            {
                return trench;
            }
        }

        return null;
    }

    public Trench NewTrench ()
    {
        var trench = PhotonNetwork.Instantiate(trenchPrefab.name, Vector3.forward, Quaternion.identity).GetComponent<Trench>();
        //trenches.Add(trench);
        //var trenchId = trench.photonView.ViewID;
        //trench.photonView.RPC("SyncNewTrench", RpcTarget.Others, trench.photonView, trenchId);
        return trench;
    }

    //[PunRPC]
    //public void SyncNewTrench (PhotonView view)
    //{
    //    Trench trench = view.GetComponent<Trench>();
    //    trenches.Add(trench);
    //}

    public void RemoveTrench (Trench trench)
    {
        trenches.Remove(trench);
        Destroy(trench.gameObject, .01f);
    }

    public void Fill (Vector2 fillPoint)
    {
        for (var l = 0; l < trenches.Count; l++)
        {
            var trench = trenches[l];

            for (var i = 0; i < trench.line.positionCount; i++)
            {
                var trenchPoint = trench.line.GetPosition(i);
                var dist = Vector2.Distance(trenchPoint, fillPoint);
                if (dist <= trench.line.widthMultiplier/2)
                {
                    if (i == 0)
                    {
                        RemoveFirstPoints(trench, 1);
                        i--;
                    }
                    else if (i == trench.line.positionCount - 1)
                    {
                        trench.line.positionCount--;
                    }
                    else
                    {
                        SplitTrench(trench, i);
                    }
                }
            }

            if (trench.line.positionCount == 0)
            {
                RemoveTrench(trench);
                l--;
            }
        }
    }

    public void RemoveFirstPoints(Trench trench, int count)
    {
        for (var i = 0; i < trench.line.positionCount-count; i++)
        {
            var pos = trench.line.GetPosition(i + count);
            trench.line.SetPosition(i, pos);
        }

        trench.line.positionCount -= count;
    }

    public void SplitTrench(Trench trench, int midIndex)
    {
        var secondHalf = NewTrench();

        var posCount = trench.line.positionCount;

        var secondHalfPosCount = posCount - midIndex - 1;

        secondHalf.line.positionCount = secondHalfPosCount;

        for (var i = 0; i < secondHalfPosCount; i++)
        {
            var pos = trench.line.GetPosition(i + midIndex);
            secondHalf.line.SetPosition(i, pos);
        }

        trench.line.positionCount = midIndex;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrenchDigger : MonoBehaviour
{
    Vector2 lastDigPoint, lastDigDir;
    public Trench trench;
    public float startWidth = 1, maxWidth = 2, digSpeed = 2, minPointDist = .2f;
    float fillRadius;

    private void Awake()
    {
        trench = null;
        fillRadius = startWidth;
    }

    public void DigTrench (Vector2 point, float widthIncrement)
    {
        float prevWidth = 0;
        float width;

        if (trench == null)
        {
            width = startWidth;
        }
        else
        {
            width = Mathf.Min(trench.lineMesh.width + widthIncrement * digSpeed, maxWidth);
            prevWidth = trench.lineMesh.width;
        }

        var distFromLast = Vector2.Distance(point, lastDigPoint);
        float moveDist = 1;
        var dir = Vector2.zero;
        if (trench != null && trench.lineMesh.points.Count > 0)
        {
            moveDist = Vector2.Distance(trench.lineMesh.points[^1], point);
            dir = (point - trench.lineMesh.points[^1]).normalized;
        }

        Vector2 prevBoxMin = Vector2.zero;
        Vector2 prevBoxMax = Vector2.zero;

        if (trench != null)
        {
            prevBoxMin = trench.lineMesh.boxMin;
            prevBoxMax = trench.lineMesh.boxMax;
        }

        //string equalSign;
        //if (dir == lastDigDir) equalSign = "=";
        //else equalSign = "!=";
        //Debug.Log($"{dir} {equalSign} {lastDigDir}");

        if (trench == null ||
            Trench.manager.TrenchExceedsMaxArea(trench) ||
            dir != lastDigDir &&
            distFromLast >= minPointDist)
        {
            trench = Trench.manager.DigTrench(point, width, trench, this);
            if (trench.lineMesh.points.Count == 1)
                trench = Trench.manager.DigTrench(point, width, trench, this);
            //Debug.Log(dir);
            lastDigPoint = point;
            lastDigDir = dir;
        }
        else
        {
            trench.lineMesh.points[^1] = point;
            trench.lineMesh.SetWidth(width);
            //if (distFromLast > 0) //commented this out just to make box show up
                trench.lineMesh.ExtendBox(point);
            //in multiplayer, this'll have to be set to the digger's position on local computer
        }

        if (trench.lineMesh.points.Count > 1)
        {
            if (Chunk.manager.TestDifferentChunks(prevBoxMin, trench.lineMesh.boxMin))
            {
                Chunk.manager.AutoAssignChunks(trench);
            }
            else if (Chunk.manager.TestDifferentChunks(prevBoxMax, trench.lineMesh.boxMax))
            {
                Chunk.manager.AutoAssignChunks(trench);
            }
        }


        if (moveDist > 0 || width != prevWidth) //it wouldn't redraw when they were just staying there lol
            Trench.manager.RegenerateMesh(trench);
    }

    public void FillTrenches (Vector2 point, float widthIncrement)
    {
        Trench.manager.FillTrenches(point, fillRadius);

        fillRadius = Mathf.Min(widthIncrement * digSpeed + fillRadius, maxWidth);
    }

    public void StopDigging ()
    {
        trench.lineMesh.PurgePoints();
        trench = null;
    }

    public void StopFilling ()
    {
        fillRadius = startWidth;
    }
}

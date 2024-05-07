using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrenchDigger : MonoBehaviour
{
    Vector3 lastDigPoint;
    public Trench trench;
    public float startWidth = 1, maxWidth = 2, digSpeed = 2, pointFreq = 2;
    float maxPointDist;

    private void Awake()
    {
        trench = null;
        maxPointDist = 1 / pointFreq;
    }

    public void DigTrench (Vector2 point, float widthIncrement)
    {
        float width;

        if (trench == null)
        {
            width = startWidth;
        }
        else
        {
            width = Mathf.Min(trench.lineMesh.width + widthIncrement * digSpeed, maxWidth);
        }

        var distFromLast = Vector2.Distance(point, lastDigPoint);

        Vector2 prevBoxMin = Vector2.zero;
        Vector2 prevBoxMax = Vector2.zero;

        if (trench != null)
        {
            prevBoxMin = trench.lineMesh.boxMin;
            prevBoxMax = trench.lineMesh.boxMax;
        }

        if (trench == null || distFromLast >= maxPointDist)
        {
            trench = Trench.manager.DigTrench(point, width, trench, this);
            if (trench.lineMesh.points.Count == 1)
                trench = Trench.manager.DigTrench(point, width, trench, this);
            lastDigPoint = point;
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


        //if (distFromLast > 0) //it wouldn't redraw when they were just staying there lol
            Trench.manager.RegenerateMesh(trench);
    }

    public void StopDigging ()
    {
        trench = null;
    }
}

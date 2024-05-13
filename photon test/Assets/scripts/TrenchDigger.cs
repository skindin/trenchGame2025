using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrenchDigger : MonoBehaviour
{
    Vector2 lastDigPoint, lastFillPoint;
    public float lastAngle;
    public Trench trench;
    public float startWidth = 1, maxWidth = 2, digSpeed = 2, minPointDist = .2f;
    float fillRadius;

    private void Awake()
    {
        trench = null;
        fillRadius = startWidth;
    }

    private void Start()
    {
        //DigTrench(transform.position, 10);
        //transform.position += Vector3.right * 10;
        //DigTrench(transform.position, 10);
        //FillTrenches(transform.position, 10);
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
        float angle = 0;
        if (trench != null && trench.lineMesh.points.Count > 0)
        {
            moveDist = Vector2.Distance(trench.lineMesh.points[^1], point);
            var dir = (point - trench.lineMesh.points[^1]).normalized;
            angle = Vector3.SignedAngle(Vector2.up, dir, Vector3.forward);
            angle = Mathf.Floor(angle);
        }

        Vector2 prevBoxMin = Vector2.zero;
        Vector2 prevBoxMax = Vector2.zero;

        if (trench != null)
        {
            prevBoxMin = trench.lineMesh.mesh.bounds.min;
            prevBoxMax = trench.lineMesh.mesh.bounds.max;
        }

        //string equalSign;
        //if (dir == lastDigDir) equalSign = "=";
        //else equalSign = "!=";
        //Debug.Log($"{dir} {equalSign} {lastDigDir}");

        if (trench == null ||
            Trench.manager.TrenchExceedsMaxArea(trench) ||
            angle != lastAngle &&
            distFromLast >= minPointDist)
        {
            trench = Trench.manager.DigTrench(point, width, trench, this);
            if (trench.lineMesh.points.Count == 1)
                trench = Trench.manager.DigTrench(point, width, trench, this);
            //Debug.Log(dir);
            lastDigPoint = point;
            lastAngle = angle;
            //Debug.Log(angle);
        }
        else
        {
            trench.lineMesh.points[^1] = point;
            trench.lineMesh.SetWidth(width);
            //if (distFromLast > 0) //commented this out just to make box show up
                //trench.lineMesh.ExtendBox(point, Trench.manager.debugLines);
            //in multiplayer, this'll have to be set to the digger's position on local computer
        }


        if (moveDist > 0 || width != prevWidth)
            Trench.manager.RegenerateMesh(trench);

        if (trench.lineMesh.points.Count > 1)
        {
            if (Chunk.manager.TestDifferentChunks(prevBoxMin, trench.lineMesh.mesh.bounds.min))
            {
                Chunk.manager.AutoAssignChunks(trench);
            }
            else if (Chunk.manager.TestDifferentChunks(prevBoxMax, trench.lineMesh.mesh.bounds.max))
            {
                Chunk.manager.AutoAssignChunks(trench);
            }
        }

        foreach (var chunk in trench.chunks)
        {

            for (int i = 0; i < chunk.detectors.Count; i++)
            {
                var detector = chunk.detectors[i];
                if (trench.lineMesh.TestBoxWithPoint(detector.transform.position))
                {
                    detector.DetectTrench(0);//if I ever find how to make radii work, i gotta fix this part
                    var newIndex = chunk.detectors.IndexOf(detector);
                    if (newIndex < i) i--;
                }
            }
        }
    }

    public void FillTrenches (Vector2 point, float widthIncrement)
    {
        //if (lastFillPoint == point && fillRadius >= maxWidth*2) return;

        Trench.manager.FillTrenches(point, fillRadius);

        fillRadius = Mathf.Min(widthIncrement * digSpeed + fillRadius, maxWidth*2);

        lastFillPoint = point;
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrenchAgent : MonoBehaviour
{
    public bool withinTrench = false;
    Vector3 lastDigPoint;
    public Trench trench;
    public float startWidth = 1, maxWidth = 2, digSpeed = 2;

    public bool UpdateStatus()
    {
        withinTrench = TrenchManager.instance.CheckWithinTrench(transform.position);
        return withinTrench;
    }

    public void Dig (Vector2 digPos)
    {
        var dist = Vector2.Distance(digPos, lastDigPoint);
        var exceededPointDist = dist > TrenchManager.instance.digPointDist;

        if (trench)
        {
            var width = trench.line.widthMultiplier;
            trench.line.widthMultiplier = Mathf.MoveTowards(width, maxWidth, digSpeed * Time.deltaTime);
        }

        if ((trench && exceededPointDist) || !trench)
        {
            trench = TrenchManager.instance.Dig(digPos, trench, startWidth);
            if (trench.line.positionCount < 2) TrenchManager.instance.Dig(digPos, trench);
            lastDigPoint = digPos;
        }
        else
        {
            var lastIndex = trench.line.positionCount - 1;
            trench.line.SetPosition(lastIndex, digPos);
        }
    }

    public void StopDig ()
    {
        trench = null;
    }

    public void FillTrench (Vector2 point)
    {
        TrenchManager.instance.Fill(point);
    }
}

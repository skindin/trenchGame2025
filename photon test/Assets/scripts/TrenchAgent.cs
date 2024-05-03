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

    public void SetStatus (bool status)
    {
        withinTrench = status;
    }

    public void Dig (Vector2 digPos)
    {
        var dist = Vector2.Distance(digPos, lastDigPoint);
        var exceededPointDist = dist > TrenchManager.instance.digPointDist;

        var addPoint = exceededPointDist && trench;

        if (trench)
        {
            //var endPoint = trench.line.GetPosition(trench.line.positionCount - 1);
            //var endDir = (Vector2)(lastDigPoint - endPoint).normalized;
            //var digDir = ((Vector2)endPoint - digPos).normalized;
            //var sameDirection = endDir == digDir;
            //if (sameDirection) addPoint = false;
            
            //it saves memory but makes the trenches look weird... shrugging emoji

            var width = trench.width;
            var newWidth = Mathf.MoveTowards(width, maxWidth, digSpeed * Time.deltaTime);
            trench.SetWidth(newWidth);
        }

        if (addPoint || !trench)
        {
            trench = TrenchManager.instance.Dig(digPos, trench, startWidth);
            lastDigPoint = digPos;

            if (!trench.agent) trench.agent = this; //this could be run once ever but shrugging emoji
        }
        else
        {
            trench.MoveEnd(digPos);
            trench.ExtendBox(digPos);
        }
    }

    public void StopDig ()
    {
        trench = null;
    }
}

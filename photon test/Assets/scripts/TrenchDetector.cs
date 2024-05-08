using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrenchDetector : MonoBehaviour
{
    public bool withinTrench = false;
    public Trench currentTrench;

    public void SetStatus (bool status)
    {
        withinTrench = status;
    }

    /// <summary>
    /// only to be used when status has surely changed. Otherwise, use 'withinTrench'
    /// </summary>
    /// <returns></returns>
    public bool DetectTrench ()
    {
        if (currentTrench != null && Trench.manager.TestTrench(transform.position, currentTrench))
        {
            return true;
        }

        currentTrench = Trench.manager.TestAllTrenches(transform.position);
        withinTrench = currentTrench != null;
        return withinTrench;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrenchDetector : MonoBehaviour
{
    public bool withinTrench = false;

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
        withinTrench = Trench.manager.TestWithinTrench(transform.position);
        return withinTrench;
    }
}

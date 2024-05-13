using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TrenchDetector : MonoBehaviour
{
    public bool withinTrench = false;
    public Trench currentTrench;
    public Chunk currentChunk;
    public UnityEvent<bool> onDetect = new();

    private void Update()
    {
        if (currentChunk == null)
        {
            DetectTrench(0);
        }
    }

    public void SetStatus (bool status)
    {
        withinTrench = status;
    }

    /// <summary>
    /// only to be used when status has surely changed. Otherwise, use 'withinTrench'
    /// </summary>
    /// <returns></returns>
    public bool DetectTrench (float radius)
    {
        if (currentTrench != null && Trench.manager.TestTrench(transform.position, radius, currentTrench))
        {
            withinTrench = true;
        }
        else
        {
            var newChunk = Chunk.manager.ChunkFromPos(transform.position, false, Trench.manager.debugLines);

            if (newChunk != currentChunk)
            {
                if (newChunk != null)
                {
                    newChunk.detectors.Add(this);
                }
                if (currentChunk != null)
                {
                    currentChunk.detectors.Remove(this);
                }

                currentChunk = newChunk;
            }

            if (newChunk == null)
            {
                withinTrench = false;
            }
            else
            {
                currentTrench = Trench.manager.TestChunkTrenches(transform.position, radius, currentChunk);
                withinTrench = currentTrench != null;
            }
        }

        onDetect.Invoke(withinTrench);
        return withinTrench;
    }
}

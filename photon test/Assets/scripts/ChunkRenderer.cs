using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkRenderer : MonoBehaviour
{
    public Material lineMaterial;
    public Vector2 renderBox = Vector2.one * 10;
    public bool debugLines = false;
    List<Chunk> chunks = new();

    public void LateUpdate()
    {
        RenderTrenches();
    }

    public void RenderTrenches ()
    {
        Vector3 boxDelta = renderBox / 2; //vector3 just to prevent conversion complications
        Vector2 boxMin = transform.position - boxDelta;
        Vector2 boxMax = transform.position + boxDelta;

        chunks.Clear();
        chunks = Chunk.manager.ChunksFromBox(boxMin, boxMax, chunks, false, debugLines);
        
        if (debugLines) GeoFuncs.DrawBox(boxMin, boxMax, Color.magenta);

        foreach (var chunk in chunks)
        {
            foreach (var trench in chunk.trenches)
            {
                Graphics.DrawMesh(trench.lineMesh.mesh, Vector3.forward, Quaternion.identity, lineMaterial, 0);
            }
        }
        

        //foreach (var trench in Trench.manager.trenches)
        //{
        //    Graphics.DrawMesh(trench.lineMesh.mesh, Vector3.forward, Quaternion.identity, lineMaterial, 0);
        //}
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkRenderer : MonoBehaviour
{
    public Material lineMaterial;
    List<Chunk> chunks = new();

    public void Update()
    {
        RenderTrenches();
    }

    public void RenderTrenches ()
    {
        chunks.Clear();
        chunks = Chunk.manager.GetAdjacenChunks(transform.position, this.chunks);

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

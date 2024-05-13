using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstanceRenderer : MonoBehaviour
{
    public Material lineMaterial, bulletMaterial;
    public Vector2 renderBox = Vector2.one * 10;
    public bool debugLines = false;
    List<Chunk> chunks = new();

    public void LateUpdate()
    {
        RenderTrenches();
        RenderBullets();
    }

    public void RenderBullets ()
    {
        foreach (var bullet in Bullet.manager.activeBullets)
        {
            var rot = Quaternion.LookRotation(Vector3.forward, bullet.velocity);
            Matrix4x4 matrix = Matrix4x4.TRS(bullet.pos, rot, Vector3.one * Bullet.manager.meshScale);
            Graphics.DrawMesh(Bullet.manager.bulletMesh, matrix, bulletMaterial, 0);
        }
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
                trench.lineMesh.mesh.RecalculateBounds();
                //should probably utilize this function other ways too lol
                Graphics.DrawMesh(trench.lineMesh.mesh, Vector3.forward, Quaternion.identity, lineMaterial, 0);
            }
        }
        

        //foreach (var trench in Trench.manager.trenches)
        //{
        //    Graphics.DrawMesh(trench.lineMesh.mesh, Vector3.forward, Quaternion.identity, lineMaterial, 0);
        //}
    }
}

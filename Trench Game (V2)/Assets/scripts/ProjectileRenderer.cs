using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileRenderer : MonoBehaviour
{
    //public Material lineMaterial;
    //public Vector2 renderBox = Vector2.one * 10;
    //public bool debugLines = false;
    //public float bulletVelocityScaleFactor = 1;
    //List<Chunk> chunks = new();
    //List<Trench> trenches = new();

    public Mesh bulletMesh;
    public Color bulletColor = Color.white;
    public Material bulletMaterial;
    public float headSize = .1f, trailEndSpeed = 10;//, ppu = 10;
    //Texture2D texture;


    //head resolution
    //int HeadRes
    //{
    //    get
    //    {
    //        return Mathf.CeilToInt(headSize * ppu);
    //    }
    //}

    private void Awake()
    {
        //var headRes = Mathf.CeilToInt(headSize * ppu);

        bulletMaterial = new Material(bulletMaterial);

        //if (!NetworkManager.IsServer)
        //    Debug.Log("this is server dedicated server");
    }

#if !UNITY_SERVER || UNITY_EDITOR

    public void LateUpdate()
    {
        RenderTrenches();
        RenderBullets();
    }
    public void RenderBullets ()
    {
        bulletMaterial.enableInstancing = true;

        var bullets = ProjectileManager.Manager.activeBullets;
        Matrix4x4[] transforms = new Matrix4x4[bullets.Count];

        bulletMaterial.color = bulletColor;

        for (int i = 0; i < bullets.Count; i++)
        {
            var bullet = bullets[i];
            var startToPos = bullet.pos - bullet.startPos;
            var clampedStartToPos = Vector2.ClampMagnitude(startToPos, bullet.range);
            var maxLength = startToPos.magnitude * (1-(trailEndSpeed / bullet.velocity.magnitude));
            //var end = bullet.pos - lengthVector;
            var clampedPos = clampedStartToPos + bullet.startPos;
            var totalDist = startToPos.magnitude;
            var startToEnd = Mathf.Clamp(totalDist - maxLength, 0, bullet.range);
            var end = bullet.startPos + startToPos.normalized * startToEnd;
            var length = Vector2.Distance(clampedPos, end);
            var center = (end + clampedPos) /2;
            Vector3 scale = new Vector2(headSize,length); // Adjust as needed
            var rot = Quaternion.LookRotation(Vector3.forward, bullet.velocity);
            Matrix4x4 matrix = Matrix4x4.TRS(center, rot, scale);
            transforms[i] = matrix;

            //Graphics.DrawMesh(bulletMesh, matrix, bulletMaterial, 0);
        }

        Graphics.DrawMeshInstanced(bulletMesh, 0, bulletMaterial, transforms);
    }

#endif

    public void RenderTrenches ()
    {
        //Vector3 boxDelta = renderBox / 2; //vector3 just to prevent conversion complications
        //Vector2 boxMin = transform.position - boxDelta;
        //Vector2 boxMax = transform.position + boxDelta;

        //chunks.Clear();
        //chunks = Chunk.manager.ChunksFromBox(boxMin, boxMax, chunks, false, debugLines);
        
        //if (debugLines) GeoFuncs.DrawBox(boxMin, boxMax, Color.magenta);

        //Mesh mesh = new();

        //Trench.manager.GetTrenchesFromChunks(chunks, trenches);

        //CombineInstance[] combine = new CombineInstance[trenches.Count];

        //for (int i = 0; i < trenches.Count; i++)
        //{
        //    var trench = trenches[i];

        //    combine[i].mesh = trench.lineMesh.mesh;
        //    combine[i].transform = Matrix4x4.identity;
        //}

        //mesh.CombineMeshes(combine);
        //Graphics.DrawMesh(mesh, Vector3.forward, Quaternion.identity, lineMaterial, 0);
    }
}

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
    public Color bulletColor = Color.white, dangerColor = Color.red;
    public Material bulletMaterial;
    Material dangerMaterial;
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

        dangerMaterial = new Material(bulletMaterial);

        //if (!NetworkManager.IsServer)
        //    Debug.Log("this is server dedicated server");
    }

#if !UNITY_SERVER || UNITY_EDITOR

    public void LateUpdate()
    {
        RenderBullets();
    }
    public void RenderBullets ()
    {
        //bulletMaterial.enableInstancing = true;

        var bullets = ProjectileManager.Manager.activeBullets;
        List<Matrix4x4> transforms = new();
        List<Matrix4x4> dangerousTransforms = new();

        bulletMaterial.color = bulletColor;
        dangerMaterial.color = dangerColor;

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

            if (bullet.withinTrench)
            {
                var matrix = GetMatrix(clampedPos, end);

                dangerousTransforms.Add(matrix);
            }
            else
            {                //transforms[i] = matrix;

                if (bullet.startedWithinTrench)
                {
                    if ((bullet.trenchExit - bullet.startPos).magnitude > (end - bullet.startPos).magnitude)
                    {
                        dangerousTransforms.Add(GetMatrix(bullet.trenchExit, end));
                    }

                    end = Vector2.ClampMagnitude(bullet.trenchExit - bullet.pos, maxLength) + bullet.pos;

                    transforms.Add(GetMatrix(clampedPos,end));
                }
                else
                {
                    var matrix = GetMatrix(clampedPos, end);
                    transforms.Add(matrix);
                }
            }
            //Graphics.DrawMesh(bulletMesh, matrix, bulletMaterial, 0);
        }

        Graphics.DrawMeshInstanced(bulletMesh, 0, dangerMaterial, dangerousTransforms);
        Graphics.DrawMeshInstanced(bulletMesh, 0, bulletMaterial, transforms);
    }

    public Matrix4x4 GetMatrix (Vector2 pointA, Vector2 pointB)
    {
        var length = Vector2.Distance(pointA, pointB);
        var center = (Vector3)(pointB + pointA) /2 + Vector3.back;
        Vector3 scale = new Vector2(headSize,length); // Adjust as needed
        var rot = Quaternion.LookRotation(Vector3.forward, pointA - pointB);
        return Matrix4x4.TRS(center, rot, scale);
    }

    //private void OnDrawGizmos()
    //{
    //    if (!Application.isPlaying)
    //    {
    //        RenderBullets();
    //    }
    //}

#endif
}

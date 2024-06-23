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
    }

    public void LateUpdate()
    {
        RenderTrenches();
        RenderBullets();
    }

    public void RenderBullets ()
    {
        //var bulletManager = ProjectileManager.Manager;
        bulletMaterial.enableInstancing = true;
        //var motionBlurSamples = bulletManager.motionBlurSamples;
        //var ogColor = material.color;

        var bullets = ProjectileManager.Manager.activeBullets;
        //Matrix4x4[] transforms = new Matrix4x4[bullets.Count];

        bulletMaterial.color = bulletColor;

        for (int i = 0; i < bullets.Count; i++)
        {


            var bullet = bullets[i];

            //for (int l = 0; l < motionBlurSamples; l++)
            //{
            //float t = (float)l / (motionBlurSamples - 1);
            //Vector3 samplePosition = bullet.pos - bullet.velocity * t * Time.deltaTime;
            var startToPos = bullet.pos - bullet.startPos;
            var clampedStartToPos = Vector2.ClampMagnitude(startToPos, bullet.range);
            //var maxLength = Mathf.Min(maxLengthPerMPS * bullet.velocity.magnitude, startToPos.magnitude);
            //var lengthVector = Vector2.ClampMagnitude(clampedStartToPos, maxLength);
            //var lengthVector = bullet.velocity.normalized * Mathf.Min(startToPos.magnitude, bullet.velocity.magnitude);
            //var clampedPos = clampedStartToPos + bullet.startPos;
            var maxLength = startToPos.magnitude * (1-(trailEndSpeed / bullet.velocity.magnitude));
            //var end = bullet.pos - lengthVector;
            var clampedPos = clampedStartToPos + bullet.startPos;
            var totalDist = startToPos.magnitude;
            var startToEnd = Mathf.Clamp(totalDist - maxLength, 0, bullet.range);
            var end = bullet.startPos + startToPos.normalized * startToEnd;
            //var clampedPos = clampedStartToPos + bullet.startPos;
            var length = Vector2.Distance(clampedPos, end);
            var center = (end + clampedPos) /2;
            //GeoFuncs.MarkPoint(end, 1, Color.white); GeoFuncs.MarkPoint(center,1, Color.white); GeoFuncs.MarkPoint(bullet.pos, 1, Color.white);
            //var posToCenter = center - end;
            Vector3 scale = new Vector2(headSize,length); // Adjust as needed
            var rot = Quaternion.LookRotation(Vector3.forward, bullet.velocity);
            Matrix4x4 matrix = Matrix4x4.TRS(center, rot, scale);
            //transforms[i] = matrix;

            Graphics.DrawMesh(bulletMesh, matrix, bulletMaterial, 0);
        }

        //foreach (var bullet in BulletManager.Manager.activeBullets)
        //{
        //    var rot = Quaternion.LookRotation(Vector3.forward, bullet.velocity);
        //    Matrix4x4 matrix = Matrix4x4.TRS(bullet.pos, rot, Vector3.one * BulletManager.Manager.meshScale);
        //    Graphics.DrawMesh(BulletManager.Manager.bulletMesh, matrix, bulletMaterial, 0);
        //}

        //Graphics.DrawMeshInstanced(bulletMesh, 0, bulletMaterial, transforms);
    }

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

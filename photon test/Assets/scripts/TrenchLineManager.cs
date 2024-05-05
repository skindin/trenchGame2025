using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TrenchLineManager : MonoBehaviour
{
    public Material lineMaterial;

    public List<TrenchLine> trenchLines = new();

    public int endRes = 1, cornerRes = 1;

    private void Start()
    {
        foreach (var line in trenchLines)
        {
            line.NewMesh(endRes, cornerRes);
        }
    }

    private void Update()
    {
        Start();
        RenderLines();
    }

    public void RenderLines()
    {
        foreach (var line in trenchLines)
        {
            Graphics.DrawMesh(line.mesh,transform.position,Quaternion.identity,lineMaterial,0);
        }
    }
}
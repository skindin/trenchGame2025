using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrenchGroup
{
    public List<Trench> trenches = new();

    public Vector2 boxMin, boxMax;
    public float area;

    public void AddTrench (Trench newTrench)
    {
        GetBox(newTrench, out boxMin, out boxMax, out area);
    }

    public void GetBox(Trench newTrench, out Vector2 boxMin, out Vector2 boxMax, out float area)
    {
        boxMin = this.boxMin;
        boxMax = this.boxMax;
        var bounds = newTrench.lineMesh.mesh.bounds;

        if (bounds.min.x < this.boxMin.x) boxMin.x = bounds.min.x;
        if (bounds.min.y < this.boxMin.y) boxMin.y = bounds.min.y;
        if (bounds.max.x > this.boxMax.x) boxMax.x = bounds.max.x;
        if (bounds.max.y > this.boxMax.y) boxMax.y = bounds.max.y;

        var dimensions = boxMax - boxMin;
        area = dimensions.x * dimensions.y;
    }

    public void RecalculateBox ()
    {
        boxMin = Vector2.one * Mathf.Infinity;
        boxMax = Vector2.one * -Mathf.Infinity;

        foreach (var trench in trenches)
        {
            GetBox(trench, out boxMin, out boxMax, out area);
        }
    }

    //this class hasn't been utilized yet, but i'll come back to it if I need to or feel like it

    //when trenches are split, they should stay in the same trench group
}

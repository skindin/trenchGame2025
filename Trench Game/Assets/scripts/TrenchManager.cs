using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrenchManager : MonoBehaviour
{
    public List<Line> lines = new();
    // Start is called before the first frame update
    static TrenchManager manager;

    public static TrenchManager Manager
    {
        get
        {
            if (manager == null)
            {
                manager = FindObjectOfType<TrenchManager>();
                if (manager == null)
                {
                    GameObject go = new GameObject("GameManager");
                    manager = go.AddComponent<TrenchManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return manager;
        }
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        //update maps and textures

        lines.Clear();
    }

    public void AddLine (Vector2 pointA, float radA, Vector2 pointB, float radB, bool fill)
    {
        lines.Add(new(pointA, radA, pointB, radB, fill));
    }

    public void UpdateTrenchMaps ()
    {

    }
}

public struct Line
{
    public Vector2 pointA, pointB;
    public float radA, radB;
    public bool fill;

    public void GetLineBounds(out Vector2 min, out Vector2 max)
    {
        min = Vector2.Min(pointA - Vector2.one * radA, pointB - Vector2.one * radB);
        max = Vector2.Max(pointA + Vector2.one * radA, pointB + Vector2.one * radB);

        GeoFuncs.DrawBox(min, max, Color.magenta);
    }

    public Line (Vector2 pointA, float radA, Vector2 pointB, float radB, bool fill)
    {
        this.pointA = pointA;
        this.radA = radA;
        this.pointB = pointB;
        this.radB = radB;
        this.fill = fill;
    }
}

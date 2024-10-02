using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SpriteOrderTool : MonoBehaviour
{
    public Vector2 groundCenterPoint = Vector2.zero;
    public bool drawPoint = false;
    static readonly List<SpriteOrderTool> all = new();
    static SpriteOrderTool main;
    public SpriteRenderer sprite;

    private void OnEnable()
    {
        all.Add(this);
    }

    private void OnDisable()
    {
        all.Remove(this);
    }

    private void OnDestroy()
    {
        all.Remove(this);
    }

    private void Update()
    {
        if (main == null || !main.enabled || !main.gameObject.activeInHierarchy || main.gameObject.IsDestroyed())
        {
            main = this;
        }

        if (main == this)
        {
            LogicAndMath.SortHighestToLowest(all, tool => tool.transform.TransformPoint(tool.groundCenterPoint).y);
            LogicAndMath.AssignIndexes(all, (tool, index) => tool.sprite.sortingOrder = index);
        }
    }

    private void OnDrawGizmos()
    {
        if (drawPoint)
        {
            var worldPoint = transform.TransformPoint(groundCenterPoint);
            GeoUtils.MarkPoint(worldPoint, .2f, Color.green);
        }
    }
}

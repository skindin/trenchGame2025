using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SpriteOrderTool : MonoBehaviour
{
    public Vector2 groundCenterPoint = Vector2.zero;
    public bool drawPoint = false, includeSelf = true;
    static readonly List<SpriteOrderTool> all = new();
    static SpriteOrderTool main;
    public SpriteRenderer sprite;
    public int localOrder = 0;
    public SpriteOrderTool parent;
    public List<SpriteOrderTool> children = new();
    Transform lastParent;
    //public Transform container;

    public int LocalOrder {  get { return localOrder; } set { localOrder = value; } }

    private void Awake()
    {
        //Debug.Log($"{gameObject.name} added itself to sprite ordering list");

        FindSpriteParent();
        //if (includeSelf)
        if (!parent)
            all.Add(this);

        if (includeSelf && !children.Contains(this))
        {
            children.Add(this);
        }
    }

    private void Update()
    {
        //if (container.parent != lastParent)
        //{
        //    FindSpriteParent();

        //    lastParent = container.parent;
        //}

        //for (var i = 0; i < container.childre)

        if (main == null || !main.enabled || !main.gameObject.activeInHierarchy || main.gameObject.IsDestroyed())
        {
            main = this;
        }

        if (main == this)
        {
            CollectionUtils.SortHighestToLowest(all, tool => tool.transform.TransformPoint(tool.groundCenterPoint).y);

            var order = 0;

            foreach (var spriteTool in all)
            {
                AssignChildrenOrder(spriteTool, ref order);
            }

            //LogicAndMath.AssignIndexes(all, (tool, index) => tool.sprite.sortingOrder = index);
        }
    }

    void AssignChildrenOrder (SpriteOrderTool sprite, ref int currentOrder)
    {
        //currentOrder++;

        for (int i = 0; i < sprite.children.Count; i++)
        {
            var child = sprite.children[i];

            if (child.includeSelf)
            {
                currentOrder++;
                child.sprite.sortingOrder = currentOrder;
            }

            if (child != sprite)
                AssignChildrenOrder(child, ref currentOrder);
        }
    }

    public void AddChild (SpriteOrderTool newChild)
    {
        bool added = false;

        for (int i = 0; i < children.Count; i++)
        {
            var child = children[i];
                
            if ((child != this && newChild.localOrder < child.localOrder) || (child == this && newChild.localOrder < 0))
            {
                children.Insert(i, newChild);
                added = true;
                break;
            }
        }

        if (!added)
        {
            children.Add(newChild);
        }
    }

    public void RemoveChild (SpriteOrderTool child)
    {
        children.Remove(child);
    }


    private void OnTransformParentChanged()
    {
        FindSpriteParent();
    }

    void FindSpriteParent()
    {
        bool hadParent = false;

        if (parent)
        {
            parent.RemoveChild(this);
            hadParent = true;
        }

        var currentParent = transform.parent;

        while (currentParent)
        {
            var foundTool = currentParent.GetComponent<SpriteOrderTool>();

            if (foundTool)
            {
                parent = foundTool;
                parent.AddChild(this);
                break;
            }
            else
            {
                currentParent = currentParent.parent;
            }
        }

        if (!currentParent)
        {
            parent = null;
        }

        if (!currentParent && hadParent)
        {
            all.Add(this);
        }
        else if (currentParent && !hadParent)
        {
            all.Remove(this);
        }
    }

    public void SetLocalOrder (int order)
    {
        localOrder = order;

        if (parent)
        {
            parent.RemoveChild (this);
            parent.AddChild (this);
        }
    }

    private void OnEnable()
    {
        if (!parent && !all.Contains(this))
            all.Add(this);
    }

    private void OnDisable()
    {
        //if (parent)
            all.Remove(this);
    }

    private void OnDestroy()
    {
        if (parent)
        {
            parent.children.Remove(this);
        }

        all.Remove(this);
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

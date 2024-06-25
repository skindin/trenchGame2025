using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using UnityEngine.Windows;

public class MultiuseJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public List<Use> uses = new ();
    public UseType currentUseType;
    Use currentUse;
    public RectTransform background;
    public bool dynamic = true;
    public float maxDynamicRadius = 2;
    public bool holding = false, tapped = false, released = false, dragged = false;

    Vector2 position;
    public Vector2 Position
    {
        get
        {
            return position;
        }

        private set
        {
            position = value;
        }
    }

    Canvas canvas;
    Canvas Canvas
    {
        get
        {
            if (!canvas)
            {
                canvas = GetComponentInParent<Canvas> ();
            }

            return canvas;
        }
    }

    public float WorldSpaceRadius
    {
        get
        {
            return background.sizeDelta.x / 2;
        }
    }

    private void Start()
    {
        background.gameObject.SetActive(false);
    }

    public void OnPointerDown (PointerEventData data)
    {
        holding = true;

        bool found = false;
        var selectedObject = data.pointerCurrentRaycast.gameObject;
        foreach (var use in uses)
        {
            if (use.handle == selectedObject)
            {
                currentUse = use;
                currentUseType = use.type;
                //currentUse.handle.transform.position = data.position;
                use.prevPos = use.handle.transform.position;

                background.position = data.position - Position * WorldSpaceRadius;

                found = true;
            }
            else
            {
                use.handle.SetActive(false);
            }
        }

        if (!found) return;

        background.gameObject.SetActive(true);
    }

    public void OnDrag(PointerEventData data)
    {
        var worldSpaceRadius = WorldSpaceRadius;
        var localDelta = data.delta / WorldSpaceRadius;

        if (dynamic)
        {
            //Vector2 position = RectTransformUtility.WorldToScreenPoint(Camera.main, background.position);
            //var input = (data.position - position) / (radius * canvas.scaleFactor);
            //Position = Vector2.ClampMagnitude(Position + localDelta, maxDynamicRadius);
            Position += localDelta;

            Vector2 worldHandleDelta = Position * worldSpaceRadius;

            //currentUse.handle.transform.position = data.position;

            var worldHandlePos = currentUse.handle.transform.position = worldHandleDelta + currentUse.prevPos;

            var backgroundPos = worldHandlePos - (Vector3)Vector2.ClampMagnitude(worldHandleDelta, worldSpaceRadius);

            background.position = backgroundPos;
        }
        else
        {
            Position = Vector2.ClampMagnitude(Position + localDelta, 1);

            currentUse.handle.transform.position = worldSpaceRadius * Position + (Vector2)background.position;
        }

        dragged = true;
    }

    public void OnPointerUp(PointerEventData data)
    {
        currentUse.handle.transform.position = currentUse.prevPos;

        //currentUse = null;
        //currentUseType = UseType.none;

        foreach (var use in uses)
        {
            use.handle.SetActive(true);
        }

        tapped = !dragged;

        holding = false;

        released = true;

        //holding = false;

        StartCoroutine(ResetTappedAfterFrame());

        background.gameObject.SetActive(false);
    }

    IEnumerator ResetTappedAfterFrame ()
    {
        yield return null;
        tapped = released = dragged = false;
    }

    [System.Serializable]
    public class Use
    {
        public GameObject handle;
        public UseType type;
        public Vector2 prevPos { get; set; }
    }

    public enum UseType
    {
        none,
        itemAbility,
        interactWithObject,
        placeObject
    }
}

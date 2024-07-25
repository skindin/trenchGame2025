using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using UnityEngine.Windows;
using UnityEngine.UI;
using TMPro;

public class MultiuseTouchCursor : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IBeginDragHandler
{
    public List<Use> uses = new ();
    //public Use defaultUse;
    public UseType defaultUseType;
    public UseType currentUseType;
    Use currentUse;
    public GameObject cursor;
    //public RectTransform background;
    //public bool dynamic = true;
    //public float maxDynamicRadius = 2;
    public Color defaultButtonColor = Color.white, useButtonColor = Color.white;
    public float sensitivity = 2, clampRadius = .5f;
    public bool holding = false, tapped = false, released = false, dragged = false, clampOnRelease = true;

    Vector2 position;
    public Vector2 CursorPosition
    {
        get
        {
            return position;
        }

        private set
        {
            cursor.transform.position = position = value;
 //= value;
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

    //public float WorldSpaceRadius
    //{
    //    get
    //    {
    //        return background.sizeDelta.x / 2;
    //    }
    //}

    //private void Update()
    //{
    //    foreach (var use in uses)
    //    {
    //        if (use.type == UseType.itemAbility)
    //        {
    //            use.button.GetComponentInChildren<Text>().text = ;
    //        }
    //    }
    //}

    private void Awake()
    {
        position = cursor.transform.position;
    }

    public void OnPointerDown (PointerEventData data)
    {
        ResetStatus();

        holding = true;

        bool found = false;
        var selectedObject = data.pointerCurrentRaycast.gameObject;
        foreach (var use in uses)
        {
            if (use.button.gameObject == selectedObject)
            {
                currentUse = use;
                currentUseType = use.type;
                //currentUse.handle.transform.position = data.position;
                //use.prevPos = use.button.transform.position;
                currentUse.button.color = useButtonColor;

                found = true;
            }
            //else
            //{
            //    use.button.SetActive(false);
            //}
        }

        if (!found)
        {
            currentUseType = defaultUseType;
        }

        if (resetRoutine != null) StopCoroutine(resetRoutine);

        //background.gameObject.SetActive(true);
    }

    public void OnBeginDrag (PointerEventData data)
    {
        //foreach (var use in uses)
        //{
        //    use.button.SetActive(false);
        //}
    }

    public void OnDrag(PointerEventData data)
    {
        var screenVector = new Vector2(Screen.width, Screen.height);
        CursorPosition = GeoUtils.ClampToBoxMinMax(CursorPosition + data.delta * sensitivity, Vector2.zero, screenVector);

        dragged = true;
    }

    public void OnPointerUp(PointerEventData data)
    {
        //currentUse.button.transform.position = currentUse.prevPos;

        //currentUse = null;
        //currentUseType = UseType.none;

        foreach (var use in uses)
        {
            //use.button.SetActive(true);
            use.button.color = defaultButtonColor;
        }

        tapped = !dragged;

        holding = false;

        released = true;

        //holding = false;

        resetRoutine = StartCoroutine(ResetTappedAfterFrame());

        if (clampOnRelease)
        {
            var center =
                //CursorPosition =
                new Vector2(Screen.width, Screen.height)/2;

            var cursorDelta = CursorPosition - center;

            var clampedDelta = Vector2.ClampMagnitude(cursorDelta, clampRadius * Screen.height);

            CursorPosition = clampedDelta + center;

            //Debug.Log("clamped cursor");
        }

        //background.gameObject.SetActive(false);
    }

    Coroutine resetRoutine;

    IEnumerator ResetTappedAfterFrame ()
    {
        yield return new WaitForEndOfFrame();
        ResetStatus();
    }

    void ResetStatus ()
    {
        tapped = released = dragged = false;
        currentUse = null;
        currentUseType = UseType.none;
    }

    public void SetUseLabel (UseType type, string text)
    {
        foreach (var use in uses)
        {
            if (use.type == type)
            {
                use.ChangeText(text);
                return;
            }
        }
    }

    public void ToggleButtons (bool toggle)
    {
        foreach (var use in uses)
        {
            use.button.gameObject.SetActive(toggle);
        }
    }

    [System.Serializable]
    public class Use
    {
        public UnityEngine.UI.Image button;
        public UseType type;
        //public System.Action<Use> action;
        //public Vector2 prevPos { get; set; }

        public void ChangeText (string newText)
        {
            var textObj = button.GetComponentInChildren<TextMeshProUGUI>();
            if (textObj) 
                textObj.text = newText;
        }
    }

    public enum UseType
    {
        none,
        itemAbility,
        interactWithObject,
        placeObject
    }
}

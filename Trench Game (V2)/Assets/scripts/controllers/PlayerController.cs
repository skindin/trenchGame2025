using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour
{
    public Character character;
    public static PlayerController active;
    public bool enableFill = false, drawEdgeDetection = false;
    public ControllerType type = ControllerType.keyboard;
    //public GameObject touchCursor;
    public bool overrideToTouchControls = false, autoReloadGuns = true;
    public int scrollSensitivity = 1;
    //public TouchController touchController;

    // Start is called before the first frame update
    void Awake()
    {
        active = this; //this definitely needs improvement for multiplayer but idc rn
    }

    //bool step = false;

    // Update is called once per frame
    void Update()
    {
        if (!Application.isFocused)
            return;

        DetermineController();

        if (type == ControllerType.keyboard)
        {
            KeyboardControls();
        }
        else if (type == ControllerType.touchscreen)
        {
            //KeyboardControls();
            TouchControls();
        }
    }

    //private void LateUpdate()
    //{
    //    //var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    //    //var mouseDir = mousePos - transform.position;
    //    //if (character.gun) character.gun.Aim(mouseDir);
    //}

    public ControllerType DetermineController ()
    {
        if (
            IsAnyKeyDown())
        {
            type = ControllerType.keyboard;
        }
        else if (Input.touchCount > 0)
        {
            type = ControllerType.touchscreen;
        }

        if (overrideToTouchControls)
            type = ControllerType.touchscreen;

        //type = ControllerType.touchscreen;

        TouchController.Main?.gameObject.SetActive(type == ControllerType.touchscreen);

        return type;
    }

    public void KeyboardControls ()
    {
        if (
            EventSystem.current.currentSelectedGameObject &&
            EventSystem.current.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>()
            )
            return;

            Vector2 moveDir;

        moveDir.x = Input.GetAxisRaw("Horizontal");
        moveDir.y = Input.GetAxisRaw("Vertical");

        moveDir.Normalize();

        if (moveDir.magnitude > 0)
        {
            character.MoveInDirection(moveDir);
        }

        var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mouseDir = mousePos - transform.position;

        character.inventory.SelectClosest(mousePos);

        if (Input.GetMouseButton(1))
        {
            if (Input.GetMouseButtonDown(1))
            {
                character.inventory.PickupClosest(mousePos);
            }
            else
            {

            }
        }

        if (character.inventory && Input.mouseScrollDelta.y != 0)
        {
            character.inventory.CurrentSlot -= (int)(Input.mouseScrollDelta.y * scrollSensitivity);
        }

        character.LookInDirection(mouseDir);

        //Debug.DrawRay(transform.position, mouseDir);

        //if (character.inventory.ActiveWeapon)
        //    character.inventory.ActiveWeapon.Aim(mouseDir); //this had been in late update...

        if (Input.GetMouseButton(0))
        {
            if (Input.GetMouseButtonDown(0))
            {

            }
            else
            {
                if (character.inventory.ActiveWeapon is Gun gun)
                    gun.DirectionalAction(mouseDir);
                else if (character.inventory.ActiveItem)
                    character.inventory.ActiveItem.Action();
            }
        }
        //else if (Input.GetMouseButtonUp(0))
        //{
        //    if (reloadGunsOnRelease && character.inventory.ActiveWeapon is Gun gun)
        //    {
        //        gun.StartReload(0,true);
        //    }
        //}

        else 
        { //new scope for sake of gun name
            if (character.inventory.ActiveWeapon is Gun gun)
            {
                gun.StartReload();
            }
        }

        if (Input.GetKey(KeyCode.E)) //temporary!!
        {

            if (Input.GetKeyDown(KeyCode.E)) //temporary!! //idk why i said this was temporary
            {
                if (character.inventory.ActiveWeapon is Gun gun)
                {
                    gun.Action();
                }
                else if (character.inventory.ActiveItem is Spade spade)
                    spade.SecondaryAction();
            }
            else 
            {

            }
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            character.inventory.DropActiveItem(mousePos);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            character.Jump();
        }

        //var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //var mouseDir = mousePos - transform.position;
    }

    public void TouchControls()
    {
        var touchController = TouchController.Main;

        var moveInput = touchController.MovementInput;

        if (moveInput.magnitude > 0)
        {
            character.MoveInDirection(moveInput);
        }

        //if (character.gun)
        //{
        //    var abilityInput = touchController.AbilityInput;

        //    character.gun.Trigger(abilityInput);
        //}

        var touchCursor = touchController.multiuseJoystick;

        var cursorWorldPos =
        //touchCursor.transform.position = 
        Camera.main.ScreenToWorldPoint(touchCursor.CursorPosition);

        //touchCursor.SetActive(true);
        var inputType = touchCursor.currentUseType;

        var directionToCursor = cursorWorldPos - transform.position;

        if (touchCursor.holding || touchCursor.released)
        {
            if (character.inventory.ActiveItem && inputType == MultiuseTouchCursor.UseType.itemAbility)
            {
                if (character.inventory.ActiveWeapon)
                {
                    if (touchCursor.tapped)
                    {
                        character.inventory.ActiveWeapon.Action();
                    }
                    else if (touchCursor.dragged)
                    {
                        character.inventory.ActiveWeapon.DirectionalAction(directionToCursor);
                    }
                }
                else
                {
                    character.inventory.ActiveItem.Action();
                }
            }
            else if (inputType == MultiuseTouchCursor.UseType.interactWithObject)
            {
                if (touchCursor.tapped)
                    character.inventory.PickupClosest(cursorWorldPos);
            }
            else if (inputType == MultiuseTouchCursor.UseType.placeObject && touchCursor.released)
            {
                character.inventory.DropActiveItem(cursorWorldPos);
            }

            if (character.inventory.ActiveItem &&
                (
                inputType == MultiuseTouchCursor.UseType.placeObject ||
                inputType == MultiuseTouchCursor.UseType.interactWithObject)
                )
            {
                touchCursor.SetUseLabel(MultiuseTouchCursor.UseType.itemAbility, character.inventory.ActiveItem.Verb);
            }

            if (character.inventory.ActiveWeapon)
            {
                character.inventory.ActiveWeapon.Aim(directionToCursor);
            }
        }

        touchCursor.ToggleButtons(character.inventory.ActiveItem);

        character.inventory.SelectClosest(cursorWorldPos);
    }

    //List<Chunk> chunks = new();

    private static bool IsAnyKeyDown()
    {
        foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(keyCode) && !IsMouseButton(keyCode))
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsMouseButton(KeyCode keyCode)
    {
        return keyCode == KeyCode.Mouse0 ||
               keyCode == KeyCode.Mouse1 ||
               keyCode == KeyCode.Mouse2 ||
               keyCode == KeyCode.Mouse3 ||
               keyCode == KeyCode.Mouse4 ||
               keyCode == KeyCode.Mouse5 ||
               keyCode == KeyCode.Mouse6;
    }
}

public enum ControllerType
{
    keyboard,
    touchscreen,
    remote
}
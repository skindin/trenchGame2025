using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchController : MonoBehaviour
{
    static TouchController main;
    static public TouchController Main
    {
        get
        {
            if (!main)
                main = FindObjectOfType<TouchController>();
            return main;
        }
    }

    public Joystick movementJoystick;
    public Vector2 MovementInput
    {
        get
        {
            return movementJoystick.Direction;
        }
    }

    public MultiuseTouchCursor multiuseJoystick;

    public Vector2 MultiuseInput
    {
        get
        {
            return multiuseJoystick.CursorPosition;
        }
    }

    //public Joystick abilityJoystick;
    //public Vector2 AbilityInput
    //{
    //    get
    //    {
    //        //abilityJoystick.
    //        return abilityJoystick.Direction;
    //    }
    //}
}

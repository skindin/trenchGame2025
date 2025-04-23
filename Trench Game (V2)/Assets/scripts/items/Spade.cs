using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;


/// <summary>
/// spade item, used for digging and filling trenches
/// </summary>
public class Spade : Item, ISecondaryAction, IDirectionalAction
{
    public float integrity, //how much area the spade can modify before it breaks
        maxIntegrity = 20; //the most integrity the spade can have/ how much it starts with
    //float elapsedRadius = 0;

    public float
        //maxIntegrity = 10, 
        maxDigRadius = 1, //the maximum distance the spade can reach
        movementModifier = .8f, //applied to character speed when using this
        radiusSpeedModifier = 2; //how fast the dig radius increases

    public float digRadius = 0; //current distance the spade will reach.
                                //starts at 0 when the character starts digging, increases by radiusSpeedModifier every second,
                                //stops at maxDigRadius, and resets to 0 when they stop digging

    public override string Verb => "dig"; //for ui purposes. honestly, not my brightest technique
    public string SecondaryVerb => "fill";

    public bool digMode = true, //which mode their in. if true, they will dig trenches when using. if false, they will fill trenches
        onlyModifySpeedIfMapWasChanged;

    Vector2 lastPos = Vector2.positiveInfinity;//the last position this spade was used at
    float lastRadius = 0; //the last dig radius the spade had

    public bool appliedMovementModifier = false; //if the movement modifer was applied. used to ensure the modifier isn't applied twice, or removed twice

    private void Awake()
    {
        integrity = maxIntegrity; //resets integrity
    }

    private void LateUpdate()
    {
        if (wielder)
        {
            lastPos = wielder.transform.position;
            lastRadius = digRadius;
        }
    }

    Coroutine usedThisFrame;

    public override void Action()
    {
        if (digRadius < maxDigRadius)
        {
            digRadius = Mathf.MoveTowards(digRadius, maxDigRadius,
                wielder.moveSpeed * radiusSpeedModifier* Time.deltaTime);
            //digRadius += (maxDigRadius - digRadius) * movementModifier * wielder.baseMoveSpeed * Time.deltaTime;
        }

        if (lastPos.x == Mathf.Infinity)
        {
            lastPos = wielder.transform.position;
        }

        if (lastRadius > digRadius)
        {
            lastRadius = 0;
        }

        TrenchManager.Manager.SetTaperedCapsule(lastPos, lastRadius, wielder.transform.position, digRadius, digMode, integrity, out var changed); 
        //applies digMode value to tapered capsule from lastPos and lastRadius to curront position and current dig radius, provides integrity as max area changed, and returns how much area was changed
        integrity -= changed; //applies changed area

        if (integrity <= 0) //if there is no more integrity, destroy this spade
        {
            DestroyItem();
            return;
        }

        //if (!changed)
        //    return;

        //lastRadius = digRadius;

        //WHY THE COROUTINES?: this function will be run directly by either the bots or character controllers.
        //however, i need to unapply the speed modifier when they stop using digging/filling, but do not want the character scripts to have anything to do with that
        //so, this script waits for a frame to see if this function was called again. if it wasn't called, it unapplies the speed modifier

        if (usedThisFrame != null) //if it wasn't used this frame, start a new coroutine
        {
            StopCoroutine(usedThisFrame);


        }
        else
        {
            OnStartDigging();
        }

        if (onlyModifySpeedIfMapWasChanged)
            ToggleSpeedModifier(changed > 0);

        usedThisFrame = StartCoroutine(WaitForNextFrame());

        IEnumerator WaitForNextFrame ()
        {
            yield return null;

            OnStopDigging();

            usedThisFrame = null;
        }
    }

    void OnStartDigging ()
    {
        ToggleSpeedModifier(true);
    }

    void ToggleSpeedModifier (bool value)
    {
        if (value == appliedMovementModifier) //prevents modifier from being applied or removed more than once
            return;

        wielder.moveSpeed *= value ? movementModifier : 1 / movementModifier;

        appliedMovementModifier = value;
    }

    void OnStopDigging ()
    {
        digRadius = lastRadius = 0;

        usedThisFrame = null;

        lastPos = Vector2.positiveInfinity; //resets lastPos so that it doesn't fill from the last position they dug at if they've moved since then

        ToggleSpeedModifier(false);
    }

    public void SecondaryAction() //toggles dig mode
    {
        digMode = !digMode;

        digRadius = lastRadius = 0;
    }

    public void DirectionalAction(Vector2 direction)
    {
        //throw new System.NotImplementedException();
        //probably gonna be a melee attack
    }

    public void Aim(Vector2 direction) //no functionality, just makes the spade rotate
    {
        //throw new System.NotImplementedException();
        transform.rotation = Quaternion.FromToRotation(Vector2.up, direction);
    }

    public override void ResetItem() //for item pooling
    {
        base.ResetItem();

        OnStopDigging();

        appliedMovementModifier = false;

        integrity = maxIntegrity;

        //integrity = ShovelModel.maxIntegrity;
    }

    public override void ToggleActive(bool active) //used when characters swap between items in their inventory
    {
        base.ToggleActive(active);

        //StopAllCoroutines();
        OnStopDigging();
    }

    public override void DropLogic(Vector2 pos, out bool destroyedSelf)
    {
        base.DropLogic(pos, out destroyedSelf);

        OnStopDigging();

        digMode = true;
    }

    public override string InfoString(string separator = " ")
    {
        var itemInfo = base.InfoString(separator);
        //itemInfo += $"{separator}{integrity:F1}/{ShovelModel.maxIntegrity:F1} sqr m"
        if (wielder)
            itemInfo += separator + (digMode ? "dig" : "fill");

        itemInfo += $"{separator}{integrity/maxIntegrity*100:F0}% integrity";
        //itemInfo += $"{separator}{maxDigRadius:F1} m";
        //itemInfo += $"{separator}speed x{movementModifier:F1}";

        return itemInfo;
    }
}

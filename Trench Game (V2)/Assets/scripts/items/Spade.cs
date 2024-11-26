using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

public class Spade : Weapon, ISecondaryAction
{
    public float integrity;
    //float elapsedRadius = 0;

    public float
        //maxIntegrity = 10, 
        maxDigRadius = 1, movementModifier = .8f, radiusSpeedModifier = 2;

    float digRadius = 0;

    public override string Verb => "dig";
    public string SecondaryVerb => "fill";

    public bool digMode = true, onlyModifySpeedIfMapWasChanged;

    Vector2 lastPos = Vector2.positiveInfinity;
    float lastRadius = 0;

    bool appliedMovementModifier = false;

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
                movementModifier * wielder.baseMoveSpeed * radiusSpeedModifier* Time.deltaTime);
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

        TrenchManager.Manager.SetTaperedCapsule(lastPos, lastRadius, wielder.transform.position, digRadius, digMode, out var changed);

        //if (!changed)
        //    return;

        //lastRadius = digRadius;

        if (usedThisFrame != null)
        {
            StopCoroutine(usedThisFrame);


        }
        else
        {
            OnStartDigging();
        }

        if (onlyModifySpeedIfMapWasChanged)
            ToggleSpeedModifier(changed);

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
        if (value == appliedMovementModifier)
            return;

        wielder.moveSpeed *= value ? movementModifier : 1 / movementModifier;

        appliedMovementModifier = value;
    }

    void OnStopDigging ()
    {
        digRadius = lastRadius = 0;

        ToggleSpeedModifier(false);
    }

    public void SecondaryAction()
    {
        digMode = !digMode;

        digRadius = lastRadius = 0;
    }

    public override void DirectionalAction(Vector2 direction)
    {
        //throw new System.NotImplementedException();

    }

    public override void Aim(Vector2 direction)
    {
        //throw new System.NotImplementedException();
        transform.rotation = Quaternion.FromToRotation(Vector2.up, direction);
    }

    public override void ResetItem()
    {
        base.ResetItem();

        OnStopDigging();

        appliedMovementModifier = false;

        //integrity = ShovelModel.maxIntegrity;
    }

    public override void ToggleActive(bool active)
    {
        base.ToggleActive(active);

        OnStopDigging();
    }

    public override void DropLogic(Vector2 pos, out bool destroyedSelf)
    {
        base.DropLogic(pos, out destroyedSelf);

        digMode = true;
    }

    public override string InfoString(string separator = " ")
    {
        var itemInfo = base.InfoString(separator);
        //itemInfo += $"{separator}{integrity:F1}/{ShovelModel.maxIntegrity:F1} sqr m"
        if (wielder)
            itemInfo += separator + (digMode ? "dig" : "fill");

        itemInfo += $"{separator} {maxDigRadius:F1} m";
        itemInfo += $"{separator} speed x{movementModifier:F1}";

        return itemInfo;
    }
}

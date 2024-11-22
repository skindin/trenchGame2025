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
        maxDigRadius = 1, movementModifier = .8f;

    float digRadius = 0;

    public override string Verb => "dig";
    public string SecondaryVerb => "fill";

    public bool digMode = true, debugLines = false;

    Vector2 lastPos = Vector2.positiveInfinity;
    float lastRadius = 0;

    private void LateUpdate()
    {
        lastPos = transform.position;
        lastRadius = digRadius;
    }

    Coroutine usedThisFrame;

    public override void Action()
    {
        if (digRadius < maxDigRadius)
        {
            digRadius = Mathf.MoveTowards(digRadius, maxDigRadius, movementModifier * wielder.baseMoveSpeed * Time.deltaTime);
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

        TrenchManager.Manager.SetTaperedCapsule(lastPos, lastRadius, wielder.transform.position, digRadius, digMode, debugLines);

        //lastRadius = digRadius;

        if (usedThisFrame != null)
        {
            StopCoroutine(usedThisFrame);
        }
        else
        {
            wielder.moveSpeed *= movementModifier;
        }

        usedThisFrame = StartCoroutine(WaitForNextFrame());

        IEnumerator WaitForNextFrame ()
        {
            yield return null;
            OnStopDigging();
            usedThisFrame = null;
        }
    }

    void OnStopDigging ()
    {
        digRadius = lastRadius = 0;

        wielder.moveSpeed /= movementModifier;
    }

    public void SecondaryAction()
    {
        digMode = !digMode;

        OnStopDigging();
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
        itemInfo += $"{separator} x{movementModifier:F1} m/s";

        return itemInfo;
    }
}

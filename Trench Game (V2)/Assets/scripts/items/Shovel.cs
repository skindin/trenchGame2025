using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

public class Shovel : Weapon, ISecondaryAction
{
    public float integrity;
    float elapsedRadius = 0;

    ShovelModel cachedModel;

    public ShovelModel ShovelModel
    {
        get
        {
            if (!cachedModel && itemModel is ShovelModel model)
            {
                cachedModel = model;
            }

            return cachedModel;
        }
    }

    public override string Verb => "dig";
    public string SecondaryVerb => "fill";

    public override void Action()
    {

    }

    public void SecondaryAction()
    {

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

        //integrity = ShovelModel.maxIntegrity;
    }

    public override string InfoString(string separator = " ")
    {
        var itemInfo = base.InfoString(separator);
        //itemInfo += $"{separator}{integrity:F1}/{ShovelModel.maxIntegrity:F1} sqr m"
        itemInfo += $"{separator} {ShovelModel.digRadius:F1} m";
        itemInfo += $"{separator} x{ShovelModel.digSpeedFactor:F1} m/s";

        return itemInfo;
    }
}

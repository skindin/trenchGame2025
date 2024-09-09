using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MedPack : Item
{
    public float animRots = 3;
    bool healing = false;
    Coroutine healRoutine;

    MedPackModel cachedModel;

    public MedPackModel MedPackModel
    {
        get
        {
            if (!cachedModel && itemModel is MedPackModel model)
                cachedModel = model;

            return cachedModel;
        }
    }

    public override void Pickup(Character character, out bool wasPickedUp, out bool wasDestroyed)
    {
        base.Pickup(character, out wasPickedUp, out wasDestroyed);

        //Action();
    }

    //public override void ItemUpdate()
    //{
    //    base.ItemUpdate();

    //    if (wielder && !healing && wielder.hp < wielder.maxHp)
    //    {
    //        Action();
    //    }
    //}

    public override void Action()
    {
        base.Action();

        healRoutine ??= StartCoroutine(Heal());
    }

    IEnumerator Heal()
    {
        if (!wielder || wielder.hp >= wielder.maxHp) yield break;

        float healingTimer = 0;

        healing = true;

        while (healingTimer < MedPackModel.healingTime)
        {
            yield return null;
            healingTimer += Time.deltaTime;
            var angle = ((MedPackModel.healingTime - healingTimer) / MedPackModel.healingTime) * animRots * 360;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        wielder?.Heal(MedPackModel.hp);
        wielder?.inventory?.RemoveItem(this);
        DestroyItem();
    }

    public override void DropLogic(Vector2 pos, out bool wasDestroyed)
    {
        base.DropLogic(pos, out wasDestroyed);

        healing = false;

        if (healRoutine != null)
        {
            StopCoroutine(healRoutine);
            healRoutine = null;
        }
    }

    public override void ResetItem()
    {
        base.ResetItem();

        healing = false;

        if (healRoutine != null)
        {
            StopCoroutine(healRoutine);
            healRoutine = null;
        }
    }

    public override string InfoString(string separator = " ")
    {
        var text = "";

        if (healing)
            text = $"(healing...)" + separator;

        text += base.InfoString(separator);
        text += separator + $"{MedPackModel.hp:F1} hp";
        text += separator + $"{MedPackModel.healingTime:F1} s";

        return text;
    }
}

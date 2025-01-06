using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Consumable : Item
{
    public float animRots = 3;
    public bool healing = false;
    Coroutine healRoutine;

    public float hp = 5, healingTime = 2;

    public override void Pickup(Character character, out bool wasPickedUp, out bool wasDestroyed, bool sync)
    {
        base.Pickup(character, out wasPickedUp, out wasDestroyed, sync);

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

        if (healRoutine == null)
        {
            healRoutine = StartCoroutine(Heal());
            NetworkManager.Manager.StartConsume(this);
        }

        //healRoutine ??= 
    }

    IEnumerator Heal(float progress = 0)
    {
        if (!wielder || wielder.hp >= wielder.maxHp) yield break;

        //float progress = 0;

        healing = true;

        while (progress < healingTime)
        {
            yield return null;
            progress += Time.deltaTime;
            var angle = ((healingTime - progress) / healingTime) * animRots * 360;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        if (NetworkManager.IsServer)
        {
            wielder?.Heal(hp);
            //wielder?.inventory?.RemoveItem(this);
            DestroyItem();
        }
    }

    public void StartHeal (float startTime)
    {
        var progress = NetworkManager.NetTime - startTime;

        healRoutine ??= StartCoroutine(Heal(progress));
    }

    public override void DropLogic(Vector2 pos, out bool wasDestroyed)
    {
        base.DropLogic(pos, out wasDestroyed);
        
        CancelHeal();
    }

    public override void ResetItem()
    {
        base.ResetItem();

        CancelHeal();
    }

    public override void ToggleActive(bool active)
    {
        base.ToggleActive(active);

        if (active)
            CancelHeal();
    }

    void CancelHeal ()
    {
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
        text += separator + $"{hp:F1} hp";
        text += separator + $"{healingTime:F1} s";

        return text;
    }
}

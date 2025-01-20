using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoringManager : ManagerBase<ScoringManager>
{
    public bool drawSceneItemScores = false;
    public Vector2 scoreLabelOffset;
    public float itemDistanceWeight = 1;
    public Gun baseGun;
    public float gunRoundsWeight, gunFireRateWeight, gunDamageRateWeight, gunRangeWeight;
    public Consumable baseConsumable;
    public float consumableHPWeight, consumableDurationWeight;

    public float GetItemDistanceScore(Vector2 pos, Item item)
    {
        var distance = Vector2.Distance(pos, item.transform.position);

        return 1 / distance * itemDistanceWeight;
    }

    public float RankGun(Gun gun, int availableAmmo = 0)
    {
        return (Mathf.Min((float)(gun.rounds + availableAmmo) / gun.maxRounds) * gunRoundsWeight) +
            (gun.firingRate / baseGun.firingRate * gunFireRateWeight) +
            (gun.damageRate / baseGun.damageRate * gunDamageRateWeight) +
            (gun.range / baseGun.range * gunRangeWeight);
    }

    public float RankConsumable(Consumable consumable)
    {
        return (consumable.hp / baseConsumable.hp * consumableHPWeight) +
            (baseConsumable.healingTime / consumable.healingTime * consumableDurationWeight); //inverse division, bc lower duration is better
    }

    public float RankItem<T>(T item)
    {
        if (item is Gun gun)
        {
            return RankGun(gun);
        }
        else if (item is Consumable consumable)
        {
            return RankConsumable(consumable);
        }

        return 0;
    }
}

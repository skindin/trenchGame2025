using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemRanking : MonoBehaviour
{
    public Gun baseGun;
    public float gunRoundsRatioWeight, gunFireRateWeight, gunDamageRateWeight, gunRangeWeight;
    public Consumable baseConsumable;
    public float consumableHPWeight, consumableDurationWeight;

    public float RankGun (Gun gun, int availableAmmo = 0)
    {
        return (Mathf.Min( (gun.rounds + availableAmmo)/ gun.maxRounds )* gunRoundsRatioWeight) +
            (gun.firingRate / baseGun.firingRate * gunFireRateWeight) + 
            (gun.damageRate / baseGun.damageRate * gunDamageRateWeight) +
            (gun.range / baseGun.range * gunRangeWeight);
    }

    public float RankConsumable (Consumable consumable)
    {
        return (consumable.hp / baseConsumable.hp * consumableHPWeight) +
            (baseConsumable.healingTime / consumable.healingTime * consumableDurationWeight); //inverse division, bc lower duration is better
    }

    public float RankItem<T> (T item)
    {
        if (typeof(T) == typeof(Gun))
        {
            return RankGun(item as Gun);
        }
        else if (typeof(T) == typeof(Consumable))
        {
            return RankConsumable(item as Consumable);
        }

        return 0;
    }
}

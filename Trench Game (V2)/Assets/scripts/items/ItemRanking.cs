using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemRanking : MonoBehaviour
{
    public float gunRoundsRatioWeight, gunFireRateWeight, gunDamageRateWeight, gunRangeWeight;
    public float consumableHPWeight, consumableDurationWeight;

    public float RankGun (Gun gun)
    {
        return (gun.rounds / gun.maxRounds * gunRoundsRatioWeight) +
            gun.firingRate * gunFireRateWeight + 
            gun.damageRate * gunDamageRateWeight +
            gun.range * gunRangeWeight;
    }

    public float RankItem<T> (T item)
    {
        if (typeof(T) == typeof(Gun))
        {
            return RankGun(item as Gun);
        }

        return 0;
    }
}

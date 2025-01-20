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
    public float characterDistanceWeight = 1;
    public Character baseCharacter;
    public float characterHPWeight, characterMoveSpeedWeight;

    public float GetItemDistanceScore(Vector2 pos, Item item)
    {
        var distance = Vector2.Distance(pos, item.transform.position);

        return 1 / distance * itemDistanceWeight;
    }

    public float GetCharacterDistanceScore (Vector2 pos, Character character)
    {
        var distance = Vector2.Distance (pos, character.transform.position);

        return 1 / distance * characterDistanceWeight;
    }

    public float GetGunScore(Gun gun, int availableAmmo = 0)
    {
        return (Mathf.Min((float)(gun.rounds + availableAmmo) / gun.maxRounds) * gunRoundsWeight) +
            (gun.firingRate / baseGun.firingRate * gunFireRateWeight) +
            (gun.damageRate / baseGun.damageRate * gunDamageRateWeight) +
            (gun.range / baseGun.range * gunRangeWeight);
    }

    public float GetConsumableScore(Consumable consumable)
    {
        return (consumable.hp / baseConsumable.hp * consumableHPWeight) +
            (baseConsumable.healingTime / consumable.healingTime * consumableDurationWeight); //inverse division, bc lower duration is better
    }

    public float GetItemScore<T>(T item)
    {
        if (item is Gun gun)
        {
            return GetGunScore(gun);
        }
        else if (item is Consumable consumable)
        {
            return GetConsumableScore(consumable);
        }

        return 0;
    }

    public float GetCharacterScore<T> (T character) where T : Character
    {
        var score = (character.hp / character.maxHp * characterHPWeight) +
            (character.moveSpeed / baseCharacter.moveSpeed * characterMoveSpeedWeight);

        if (character.inventory.ActiveWeapon)
            score += GetItemScore(character.inventory.ActiveWeapon);

        return score;
    }
}

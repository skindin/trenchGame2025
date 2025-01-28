using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BotBrains
{
    class ProfileManager : ManagerBase<ProfileManager>
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

        public float GetItemDistanceScore(Vector2 posA, Vector2 posB)
        {
            var distance = Vector2.Distance(posA, posB);

            return 1 / distance * itemDistanceWeight;
        }

        public float GetCharacterDistanceScore(Vector2 posA, Vector2 posB)
        {
            var distance = Vector2.Distance(posA, posB);

            return 1 / distance * characterDistanceWeight;
        }

        public float GetGunScore(GunProfile gun, int availableAmmo = 0)
        {
            var prefab = ItemManager.Manager.itemPools[gun.prefabId].prefab as Gun;

            return (Mathf.Min((float)(gun.rounds + availableAmmo) / prefab.maxRounds) * gunRoundsWeight) +
                (prefab.firingRate / baseGun.firingRate * gunFireRateWeight) +
                (prefab.damageRate / baseGun.damageRate * gunDamageRateWeight) +
                (prefab.range / baseGun.range * gunRangeWeight);
        }

        public float GetConsumableScore(ConsumableProfile consumable)
        {
            var prefab = ItemManager.Manager.itemPools[consumable.prefabId].prefab as Consumable;

            return (prefab.hp / baseConsumable.hp * consumableHPWeight) +
                (baseConsumable.healingTime / prefab.healingTime * consumableDurationWeight); //inverse division, bc lower duration is better
        }

        public float GetItemScore<T>(T item) where T : ItemProfile
        {
            if (item is GunProfile gun)
            {
                return GetGunScore(gun);
            }
            else if (item is ConsumableProfile consumable)
            {
                return GetConsumableScore(consumable);
            }
            else return 0; //temporary
        }

        public float GetCharacterScore<T>(T character) where T : CharacterProfile
        {
            var prefab = CharacterManager.Manager.prefab;

            var score = (character.hp.HasValue ?
                character.hp.Value /prefab.maxHp * characterHPWeight:
                characterHPWeight) +
                (character.velocity.HasValue ?
                character.velocity.Value.magnitude / baseCharacter.moveSpeed * characterMoveSpeedWeight :
                characterMoveSpeedWeight);

            foreach (var item in character.items)
            {
                score += GetItemScore(item);
            }

            return score;
        }
    }
}

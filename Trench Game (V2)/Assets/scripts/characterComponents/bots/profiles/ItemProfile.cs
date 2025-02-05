using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BotBrains;

namespace BotBrains //only need to make one of these if something about the item can permenantly change
{
    public class ItemProfile
    {
        public int prefabId;//, localId;
        public CharacterProfile wielder;
        public float lastTimeSeen;
        public Vector2? pos;
        public bool isVisible;
        //public virtual bool IsDestructable => false;

        public virtual void ResetPrivateProperties ()
        {
        }

        public virtual void UpdateWithItem<T> (T item, CharacterProfile character = null) where T : Item
        {
            lastTimeSeen = Time.time;
            pos = item.transform.position;
            isVisible = true;

            if (character != null)
                wielder = character;
        }

        public virtual string Print => $"prefabId: {prefabId}";
    }

    public class WeaponProfile : ItemProfile
    {

    }

    public class GunProfile : WeaponProfile
    {
        public int? rounds;

        public override void ResetPrivateProperties()
        {
            base.ResetPrivateProperties();

            rounds = null;
        }

        public override void UpdateWithItem<T>(T item, CharacterProfile character = null)
        {
            base.UpdateWithItem(item, character);

            if (item is Gun gun)
            {
                rounds = gun.rounds;
            }
        }

        public override string Print => base.Print + (rounds.HasValue ? $", rounds: {rounds}" : "");
    }

    public class StackProfile : ItemProfile
    {
        public int? amount;

        public override void ResetPrivateProperties()
        {
            base.ResetPrivateProperties();

            amount = null;
        }

        public override void UpdateWithItem<T>(T item, CharacterProfile character = null)
        {
            base.UpdateWithItem(item, character);

            if (item is StackableItem stack)
            {
                amount = stack.amount;
            }
        }

        public override string Print => base.Print + (amount.HasValue ? $", amount: {amount}" : "");
    }

    public class AmmoProfile : StackProfile
    {

        //public override bool IsDestructable => true;
    }

    public class ConsumableProfile : ItemProfile
    {
    
    }
}

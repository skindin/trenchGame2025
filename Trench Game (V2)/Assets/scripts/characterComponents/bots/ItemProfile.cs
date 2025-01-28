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

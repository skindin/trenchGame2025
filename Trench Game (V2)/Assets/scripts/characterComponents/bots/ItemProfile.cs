using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BotBrains;

namespace BotBrains //only need to make one of these if something about the item can permenantly change
{
    public class ItemProfile
    {
        public int prefabId;
        public int? lastKnownWielder;
        public float lastTimeSeen;
        public float? lastKnownPower;
        public Vector2? lastKnownPos;
        public bool isVisible;

        public virtual void ResetPrivateProperties ()
        {
            lastKnownPower = null;
        }
    }

    public class WeaponProfile : ItemProfile
    {

    }

    public class GunProfile : WeaponProfile
    {
        public int? lastKnownRounds;

        public override void ResetPrivateProperties()
        {
            base.ResetPrivateProperties();

            lastKnownRounds = null;
        }
    }

    public class StackProfile : ItemProfile
    {
        public int? amount;

        public override void ResetPrivateProperties()
        {
            base.ResetPrivateProperties();

            amount = null;
        }
    }

    public class AmmoProfile : StackProfile
    {
    
    }
}

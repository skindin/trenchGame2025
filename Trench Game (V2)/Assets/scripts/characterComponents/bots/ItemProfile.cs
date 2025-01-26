using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TrenchBot;

namespace TrenchBot
{
    public abstract class ItemProfile
    {
        public int prefabId;
        public Vector2? lastKnownPos;
        public bool currentlyVisible;
    }

    public abstract class WeaponProfile : ItemProfile
    {

    }

    public class GunProfile : WeaponProfile
    {
        public int lastKnownRounds;
    }
}

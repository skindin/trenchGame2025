using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BotBrains
{
    public class CharacterProfile
    {
        public int id;
        public Vector2? lastKnownPos, lastKnownVelocity;
        public float lastSeenTime, lastKnownPower, lastDamagedTime, totalDamageDealt;
        public bool isVisible;
        public int?[] items;
        //public Character character;

        public Vector2? SetCurrentVelocity(Vector2 currentPos, float deltaTime)
        {
            if (lastKnownPos.HasValue)
            {
                return lastKnownVelocity = (currentPos - lastKnownPos.Value) / deltaTime;
            }
            else
            {
                return null;
            }
        }

        public Vector2? GuessCurrentPos(float time)
        {
            if (lastKnownPos.HasValue)
            {
                if (lastKnownVelocity.HasValue)
                {
                    return lastKnownPos.Value + lastKnownVelocity.Value * (time - lastSeenTime);
                }
                else
                {
                    return lastKnownPos.Value;
                }
            }
            else
            {
                return null;
            }
        }
    }
}
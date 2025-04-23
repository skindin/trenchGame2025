using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BotBrains
{
    public class CharacterProfile //REVIEWERS: ignore this script
    {
        public int id;
        public Vector2? pos, velocity;
        public float? hp, lastSeenTime, power, lastDamagedTime, totalDamageDealt;
        public bool isVisible;
        public HashSet<ItemProfile> items = new(); //int represents prefab id
        public ItemProfile activeItem;
        //public Character character;

        public void AddKnownItem (ItemProfile itemProfile)
        {
            activeItem = itemProfile;
            items.Add(itemProfile);
        }

        public Vector2? SetCurrentVelocity(Vector2 currentPos, float deltaTime)
        {
            if (pos.HasValue)
            {
                return velocity = (currentPos - pos.Value) / deltaTime;
            }
            else
            {
                return null;
            }
        }

        public Vector2? GuessCurrentPos(float time)
        {
            if (pos.HasValue)
            {
                if (velocity.HasValue)
                {
                    return pos.Value + velocity.Value * (time - lastSeenTime);
                }
                else
                {
                    return pos.Value;
                }
            }
            else
            {
                return null;
            }
        }

        public void UpdateWithCharacter (Character character)
        {
            pos = character.transform.position;
            hp = character.hp;
            lastSeenTime = Time.time;
            isVisible = true;

            //gotta update items too

            power = ProfileManager.Manager.GetCharacterScore(this);
        }

        public string Print => $"characterId: {id}{(activeItem != null ? $", activeItem: {activeItem.Print}":"")}";
    }
}
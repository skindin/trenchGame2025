using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Clan
{
    public string Name;
    public Color color;
    public int id;
    public Dictionary<int, Character> characters = new();

    public void AddCharacter (Character character)
    {
        characters.Add(character.id, character);
    }

    public void RemoveCharacter (Character character)
    {
        characters.Remove(character.id);
    }

    public bool DoesInclude (Character character)
    {
        return characters.TryGetValue(character.id, out var found) && character == found; //this is probably better than a list...
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClanManager : ManagerBase<ClanManager>
{
    public List<Clan> clans = new();
    List<int> indexList = new ();
    int nextClanIndex = 0;

    public int GetRandomIndexByPopulation ()
    {
        var ratio = Random.value;

        return CollectionUtils.GetRandomIndexFromListValues(ratio, clans,
            clan => clan.characters.Count > 0 ? 1 / clan.characters.Count : 10 * clans.Count);
        //no idea how clean this will work lol
    }

    public int GetNextClanIndex()
    {
        if (nextClanIndex == 0)
        {
            indexList = CollectionUtils.GetRandomizedIntList(clans.Count, (min, max) => Random.Range(min, max));
        }

        var nextIndex = indexList[nextClanIndex];
        nextClanIndex = (int)Mathf.Repeat(nextClanIndex+1,clans.Count);
        return nextIndex;
    }

    public Clan AssignToClanByIndex (Character character, int index)
    {
        var clan = clans[index];
        character.AssignClan(clan);
        clan.AddCharacter(character);

        return clan;
    }

    private void Awake()
    {
        CollectionUtils.AssignIntPropToIndex(clans, (clan, id) => clan.id = id);
        //nextClanIndex = Random.Range(0,clans.Count);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClanManager : ManagerBase<ClanManager>
{
    public List<Clan> clans;

    public Clan GetRandomClan ()
    {
        return clans[Random.Range(0, clans.Count)];
    }

    public Clan GetRandomClanByPopulation ()
    {
        return clans[GetRandomClanIndexByPopulation()];

    }

    public int GetRandomClanIndexByPopulation ()
    {
        var ratio = Random.value;

        return CollectionUtils.GetRandomIndexFromListValues(ratio, clans,
            clan => clan.characters.Count > 0 ? 1 / clan.characters.Count : 10 * clans.Count);
        //no idea how clean this will work lol
    }

    private void Awake()
    {
        CollectionUtils.AssignIntPropToIndex(clans, (clan, id) => clan.id = id);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotManager : ManagerBase<BotManager>
{
    public float distanceWeight = 1f;

    public float GetDistanceScore(BotControllerV2 bot, Item item)
    {
        var distance = Vector2.Distance(bot.transform.position,item.transform.position);

        return distance * distanceWeight;
    }
}

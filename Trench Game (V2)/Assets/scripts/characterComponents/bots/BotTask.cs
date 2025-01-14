using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct BotTask<Type>
{
    public Type target;
    public int priority;

    public enum TaskType
    {
        Destroy, //for characters and other destroyable objects
        Retrieve, //for picking up items items
        Prepare, //reloading guns
        Repair, //healing characters
        Consume, //using consumables
        Flee, //when moving towards another target
    }
}
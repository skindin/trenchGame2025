using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BotControllerV2 : MonoBehaviour
{
    public Character character;
    public Vector2 visionBox;
    public bool debugLines = false;
    public Transform targetObj;
    public Vector2 targetPos;

    public Chunk[,] chunks;

    private void Update()
    {
        UpdateChunks();
    }

    public void UpdateChunks ()
    {
        chunks = ChunkManager.Manager.ChunksFromBoxPosSize(transform.position,visionBox);
    }

    public Item PickupClosestItem<T>(Func<T, bool> condition = null) where T : Item
    {
        var closestItem = LogicAndMath.GetClosest(
            transform.position,
            character.inventory.withinRadius.OfType<T>().ToArray(),
            item => item.transform.position,
            out _,
            condition
        );

        if (closestItem)
        {
            var dropPos = UnityEngine.Random.insideUnitCircle * character.inventory.selectionRad + (Vector2)closestItem.transform.position;
            character.inventory.PickupItem(closestItem,dropPos,true);
        }

        return closestItem;
    }

    public T FindClosestCharacter<T>(Func<T,bool> condition = null) where T : Character
    {
        return ChunkManager.Manager.FindClosestCharacterWithinBoxPosSize(transform.position, visionBox, condition, chunks, debugLines);
    }

    public T FindClosestItem<T>(Func<T, bool> condition = null) where T : Item
    {
        return ChunkManager.Manager.FindClosestItemWithinBoxPosSize(transform.position, visionBox, condition, chunks, debugLines);
    }

    public List<T> GetItems<T> (Func<T, bool> condition = null) where T : Item
    {
        return ChunkManager.Manager.GetItemsWithinChunkArray(chunks, condition);
    }

    public List<T> GetCharacters<T>(Func<T, bool> condition = null) where T : Character
    {
        return ChunkManager.Manager.GetCharactersWithinChunkArray(chunks, condition);
    }

    private void OnDrawGizmos()
    {
        if (debugLines)
        {
            GeoUtils.DrawBoxPosSize(transform.position, visionBox, UnityEngine.Color.magenta);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System;

public class AIController : MonoBehaviour
{
    public Character character;
    public Vector2 visionBox;
    public bool wandering = false, debugLines = false;
    public float dangerRadius = 10, changeDirRadius = .1f;
    public Vector2 targetPos;
    public Item targetItem;
    public Collider targetCollider;
    public Character closestEnemy;

    private void Update()
    {
        //targetPos += Random.insideUnitCircle;
        //targetPos = ChunkManager.Manager.ClampPosToNearestChunk(targetPos);
        //var delta = targetPos - (Vector2)transform.position;
        //character.Move(delta);

        SoldierLogic();
    }

    public void SoldierLogic ()
    {
        var chunks = ChunkManager.Manager.ChunksFromBoxPosSize(transform.position, visionBox);

        closestEnemy = FindClosestCharacterWithinChunks<Character>(chunks);

        if (targetCollider && !targetCollider.gameObject.activeInHierarchy) 
            targetCollider = null;

        if (targetItem && (!targetItem.gameObject.activeInHierarchy || targetItem.wielder))
            targetItem = null;

        if (closestEnemy && !closestEnemy.gameObject.activeInHierarchy)
        {
            closestEnemy = null;
        }

        if (!character.gun) //pickup a gun if you don't have one
        {
            PickupClosestItem<Gun>();
        }

        if (character.gun) //if you picked up a gun...
        {
            if (!targetCollider && closestEnemy) //and you have no target collider, but you do have a close enemy...
            {
                targetCollider = closestEnemy.collider; //target the closest enemy
            }

            if (character.gun.rounds <= 0) //and the gun is out of amo...
            {
                if (!character.gun.reloading && character.reserve.GetAmoAmount(character.gun.GunModel.amoType) > 0) //and you didn't arlready start reloading and you have amo in the reserve...
                    character.gun.StartReload(); //start reloading the gun
            }
            else //if the gun has amo...
            {
                if (targetCollider) //and you have a collider to target...
                {
                    var direction = targetCollider.transform.position - transform.position;
                    character.gun.Trigger(direction); //shoot at the collider

                    targetPos = targetCollider.transform.position;
                }
            }
        }
        else //if you still don't have a gun...
        {
            var closestGun = FindClosestItemWithinChunks<Gun>(chunks); //find the closest gun you can see

            if (closestGun) //if you find a gun...
            {
                targetItem = closestGun; //target the item
                targetPos = targetItem.transform.position;
            }
            else //if you couldn't find a gun...
            {
                if (debugLines)
                    GeoFuncs.DrawCircle(transform.position, dangerRadius, Color.red);

                if (closestEnemy && Vector2.Distance(closestEnemy.transform.position,transform.position) <= dangerRadius) //and you are too close to an enemy...
                {
                    targetPos = (transform.position - closestEnemy.transform.position).normalized * dangerRadius + transform.position;
                    targetPos = ChunkManager.Manager.ClampPosToNearestChunk(targetPos);

                    //Wander();
                }
            }
        }

        if (!closestEnemy)
        {
            var closestAmo = FindClosestItemWithinChunks<Amo>(chunks);

            bool pickupAmo = false;

            if (!targetItem && closestAmo)
            {
                var pool = character.reserve.GetPool(closestAmo.AmoModel.type);
                if (pool.rounds < pool.maxRounds) //if there's room...
                    pickupAmo = true;
            }

            if (pickupAmo)
            {
                targetPos = closestAmo.transform.position;
            }
            else
            {
                Wander();
            }
        }

        if (debugLines)
            GeoFuncs.MarkPoint(targetPos, .5f, Color.magenta);

        var diretion = targetPos - (Vector2)transform.position;

        if (wandering && character.gun)
        {
            character.gun.Aim(diretion);
        }

        character.Move(diretion);
    }

    public void Wander ()
    {
        if (!wandering || Vector2.Distance((Vector2)transform.position,targetPos) <= changeDirRadius)
        {
            targetPos = ChunkManager.Manager.GetRandomPos();
            wandering = true;
        }
    }

    //public void Move ()
    //{
    //}

    public void PickupClosestItem<T> () where T : Item
    {
        var closestItem = LogicAndMath.GetClosestWithCondition(transform.position, character.inventory.withinRadius, item => item.transform.position, item => item is T);
        //not super familier with 'is', might not work

        character.inventory.PickupItem(closestItem);

        //return closestItem;

        character.inventory.PickupItem(closestItem);
    }

    public T FindClosestItemWithinChunks<T>(Chunk[,] chunks) where T : Item
    {
        T[] array = new T[0];

        foreach (var chunk in chunks)
        {
            if (chunk == null) continue;

            if (debugLines)
                ChunkManager.Manager.DrawChunk(chunk,Color.green);

            array = chunk.GetItems(array, false);
        }

        float closestDist = Mathf.Infinity;
        T closestItem = null;

        foreach (var item in array)
        {
            if (!GeoFuncs.TestBoxPosSize(transform.position, visionBox, item.transform.position, debugLines))
                continue;

            var dist = Vector2.Distance(item.transform.position, transform.position);

            if (dist < closestDist)
            {
                closestDist = dist;
                closestItem = item;
            }
        }

        return closestItem;
    }

    public T FindClosestCharacterWithinChunks<T>(Chunk[,] chunks) where T : Character
    {
        T[] array = new T[0];

        foreach (var chunk in chunks)
        {
            if (chunk == null) continue;

            array = chunk.GetCharacters(array, false);
        }

        float closestDist = Mathf.Infinity;
        T closestCharacter = null;

        foreach (var character in array)
        {
            if (character == this.character || //almost forgot to disclude this character lol
                !GeoFuncs.TestBoxPosSize(transform.position, visionBox, character.transform.position, debugLines))
                continue;

            var dist = Vector2.Distance(character.transform.position, transform.position);

            if (dist < closestDist)
            {
                closestDist = dist;
                closestCharacter = character;
            }
        }

        return closestCharacter;
    }

    private void OnDrawGizmos()
    {
        if (debugLines)
        {
            GeoFuncs.DrawBoxPosSize(transform.position, visionBox, Color.magenta);
        }
    }
}

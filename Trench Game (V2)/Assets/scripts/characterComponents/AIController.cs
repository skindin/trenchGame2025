using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System;

public class AIController : MonoBehaviour
{
    public Character character;
    public Vector2 visionBox;
    public bool wandering = false, dodging = false, debugLines = false;
    public float dangerRadius = 10, dodgeRadius = .1f, pointerSpeed = 50, minPointerMag = 1, maxPointerMag = 1, maxPointerOffset = .5f;
    Vector2 targetPos, pointerPos, targetPointerPos; //pointer pos and target pointer pos are in LOCAL space

    Vector2 TargetPointerPos
    {
        get
        {
            var magnitude = Mathf.Clamp(targetPointerPos.magnitude, minPointerMag, maxPointerMag);

            return targetPointerPos.normalized * magnitude; //aim towards collider
        }

        set
        {
            var magnitude = Mathf.Clamp(value.magnitude, minPointerMag, maxPointerMag);

            targetPointerPos = value.normalized * magnitude; //aim towards collider
        }
    }

    public Vector2 TargetPos
    {
        get
        {
            return targetPos;
        }

        set
        {
            value = ChunkManager.Manager.ClampPosToNearestChunk(value);
            targetPos = value;
            path.Add(targetPos);
        }
    }

    public Item targetItem;
    public Collider targetCollider;
    public Character closestEnemy;
    List<Vector2> path = new();

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
            if (!targetCollider && closestEnemy && GeoFuncs.TestBoxPosSize(transform.position, visionBox, closestEnemy.transform.position)) //and you have no target collider, but you do have a close enemy...
            {
                targetCollider = closestEnemy.collider; //target the closest enemy
            }

            if (character.gun.rounds <= 0) //and your gun is out of amo...
            {
                if (!character.gun.reloading && character.reserve.GetAmoAmount(character.gun.GunModel.amoType) > 0) //and you didn't arlready start reloading and you have amo in the reserve...
                    character.gun.StartReload(); //start reloading the gun
            }
            else //and your gun has amo...
            {
                if (targetCollider && GeoFuncs.TestBoxPosSize(transform.position, visionBox, targetCollider.transform.position)) //and collider target is within view...
                {
                    var direction = pointerPos;

                    var dist = Vector2.Distance(character.gun.BarrelPos, targetCollider.transform.position) + targetCollider.WorldSize/2;

                    var range = character.gun.GunModel.range;

                    if (debugLines)
                        GeoFuncs.DrawCircle(character.gun.BarrelPos, range, Color.red, 8);

                    if (dist <= range) //if within range..
                    {
                        if (GeoFuncs.DoesLineIntersectCircle(
                            targetCollider.transform.position, 
                            targetCollider.WorldSize / 2, 
                            transform.position, 
                            (Vector2)transform.position + range * direction.normalized,
                            //debugLines
                            false
                            )) //and within trajectory
                        {
                            character.gun.Trigger(direction); //shoot at the collider
                        }


                    }
                    else
                    {
                        TargetPos = targetCollider.transform.position;
                        dodging = false;
                    }
                }
                else
                {
                    targetCollider = null;
                }
            }
        }
        else //if you still don't have a gun...
        {
            var closestGun = FindClosestItemWithinChunks<Gun>(chunks); //find the closest gun you can see

            if (closestGun) //if you find a gun...
            {
                targetItem = closestGun; //target the item
                TargetPos = targetItem.transform.position;
            }
            else //if you couldn't find a gun...
            {
                if (debugLines)
                    GeoFuncs.DrawCircle(transform.position, dangerRadius, Color.red, 8);

                if (closestEnemy && Vector2.Distance(closestEnemy.transform.position,transform.position) <= dangerRadius) //and you are too close to an enemy...
                {

                    Evade();
                    //Wander();
                }
            }
        }

        if (character.gun && character.gun.rounds > 0 && targetCollider)
        {
            //var colliderDelta = targetCollider.transform.position - transform.position;
            TargetPointerPos = targetCollider.transform.position - transform.position; //aim towards collider

            Dodge();
        }
        else
        {
            dodging = false;

            if (closestEnemy)
            {
                Evade();
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
                TargetPos = closestAmo.transform.position;

                wandering = false;
            }
            else
            {
                Wander();
            }

            //var colliderDelta = targetPointerPos - (Vector2)transform.position;
            //TargetPointerPos = TargetPos - (Vector2)transform.position; //aim towards collider

            //var closestItem = FindClosestItemWithinChunks<Item>(chunks);

            //if (closestItem)
            //    TargetPointerPos = closestItem.transform.position - transform.position;


            //else
            //{
            //    TargetPointerPos += UnityEngine.Random.insideUnitCircle * .01f;
            //}

            //targetPointerPos = TargetPos - (Vector2)transform.position;
            //GeoFuncs.RandomPosInBoxPosSize(transform.position, visionBox);
        }


        //else //theres gotta be a better way to do this
        //{
        //    wandering = false;
        //}

        if (debugLines)
            GeoFuncs.MarkPoint(TargetPos, .5f, Color.magenta);

        //var moveDirection = TargetPos - (Vector2)transform.position;

        //var moveDirection = Vector2.MoveTowards(transform.position, TargetPos, 10) - (Vector2)transform.position;

        character.MoveToPos(TargetPos);

        pointerPos = Vector2.MoveTowards(pointerPos,TargetPointerPos,Time.deltaTime * pointerSpeed);

        if (debugLines)
        {
            GeoFuncs.MarkPoint(targetPointerPos + (Vector2)transform.position, 1, Color.red);
            GeoFuncs.MarkPoint(pointerPos + (Vector2)transform.position, 1, Color.blue);
        }

        if (character.gun)
            character.gun.Aim(pointerPos);
    }

    public void Wander ()
    {
        if (!wandering || 
            (Vector2)transform.position == TargetPos
            //Vector2.Distance((Vector2)transform.position,TargetPos) <= changeDirRadius
            )
        {
            TargetPos = ChunkManager.Manager.GetRandomPos();
            wandering = true;
        }
    }

    public void Dodge()
    {
        if (!dodging ||
        (Vector2)transform.position == TargetPos
        //Vector2.Distance((Vector2)transform.position,TargetPos) <= changeDirRadius
        )
        {
            
            TargetPos = UnityEngine.Random.insideUnitCircle * dodgeRadius + (Vector2)transform.position;
            dodging = true;
        }
    }

    public void Evade()
    {
        TargetPos = (transform.position - closestEnemy.transform.position).normalized * dangerRadius + transform.position;
    }

    //public void Move ()
    //{
    //}

    public void PickupClosestItemWithCondition<T>(Func<Item, bool> condition) where T : Item
    {

        var closestItem = LogicAndMath.GetClosestWithCondition(
            transform.position,
            character.inventory.withinRadius,
            item => item.transform.position,
            condition
        );

        if (closestItem)
        {
            character.inventory.PickupItem(closestItem);
        }
    }

    public void PickupClosestItem<T>() where T : Item
    {
        PickupClosestItemWithCondition<T>(item => item is T);
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

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
    public float dangerRadius = 10, dodgeRadius = .1f, pointerSpeed = 50, minPointerMag = 1, maxPointerMag = 1, maxPointerOffset = .5f, maxTargetOffset = .5f;
    Vector2 targetPos, targetPosOffset, pointerPos, targetPointerPos; //pointer pos and target pointer pos are in LOCAL space
    Chunk[,] chunks = default;

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
            targetPosOffset = (Vector2)UnityEngine.Random.insideUnitSphere * maxTargetOffset;
            return targetPos;
        }

        set
        {
            value = ChunkManager.Manager.ClampToWorld(value);
            targetPos = value;
            path.Add(transform.position);
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

        chunks = ChunkManager.Manager.ChunksFromBoxPosSize(transform.position, visionBox);

        SoldierLogic();
    }

    public void SoldierLogic ()
    {
        //closestEnemy = FindClosestCharacterWithinChunks<Character>(chunks);
        closestEnemy = FindClosestCharacter<Character>(character => character != this.character && (character.gun || this.character.gun));

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
            var closestGun = FindClosestItem<Gun>(); //find the closest gun you can see

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
            //var closestAmo = FindClosestItem<StackableItem>();
            //bool pickupAmo = closestAmo;

            var closestAmo = FindClosestItem<Amo>();

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
        else
        {
            wandering = false;
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
        path[^1] = transform.position;

        pointerPos = Vector2.MoveTowards(pointerPos,TargetPointerPos,Time.deltaTime * pointerSpeed);

        if (debugLines)
        {
            GeoFuncs.MarkPoint(targetPointerPos + (Vector2)transform.position, 1, Color.red);
            GeoFuncs.MarkPoint(pointerPos + (Vector2)transform.position, 1, Color.blue);

            GeoFuncs.DrawLine(path, Color.black);
        }

        if (character.gun)
            character.gun.Aim(pointerPos);
    }

    List<Vector2> wanderPoints = new();
    int wanderIndex = -1;

    public void Wander()
    {
        if (!wandering)
        {
            if (wanderIndex < 0)
            {
                wanderPoints = ChunkManager.Manager.DistributePoints(visionBox, wanderPoints);

                for (int i = 0; i < wanderPoints.Count; i++)
                {
                    wanderPoints[i] += (Vector2)UnityEngine.Random.insideUnitSphere * maxTargetOffset;
                }
            }

            wandering = true;
            TargetPos = transform.position;

            //wanderIndex = -1;
        }
        else
        {
            bool foundPoint = false;

            if (wanderIndex > -1 && (Vector2)transform.position == TargetPos)
            {
                wanderPoints.RemoveAt(wanderIndex);
                foundPoint = true;
                //wanderIndex = -1;
            }

            if (wanderIndex < 0 || foundPoint)
            {
                wanderIndex = LogicAndMath.GetClosestIndex(transform.position, wanderPoints, point => point, null);

                if (wanderIndex > -1) // Ensure valid index
                {
                    var min = (Vector2)transform.position - visionBox / 2;
                    var max = (Vector2)transform.position + visionBox / 2;

                    var wanderPoint = wanderPoints[wanderIndex];

                    var closestVisiblePoint = Vector2.Max(wanderPoint, min);
                    closestVisiblePoint = Vector2.Min(closestVisiblePoint, max);

                    var delta = wanderPoint - closestVisiblePoint;

                    //if (debugLines)
                    //    GeoFuncs.MarkPoint(closestVisiblePoint, 1, Color.yellow);

                    TargetPos = (Vector2)transform.position + delta;

                    if (debugLines)
                        GeoFuncs.MarkPoint(closestVisiblePoint, 1, Color.yellow);
                }

            }

            if (wanderIndex < 0) // if there are no points left, redistribute points
            {
                wandering = false;
                Wander(); //IT KEEPS RUNNING THIS EVERY TIME 
                return;
            }
        }

        if (debugLines)
        {
            for (int i = 0; i < wanderPoints.Count; i++)
            {
                var point = wanderPoints[i];
                Color color = (i == wanderIndex) ? Color.green : Color.blue;
                GeoFuncs.MarkPoint(point, 1, color);
            }
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

        var closestItem = LogicAndMath.GetClosest(
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

    public T FindClosestCharacter<T>(Func<T,bool> condition = null) where T : Character
    {
        return ChunkManager.Manager.FindClosestCharacterWithinBoxPosSize(transform.position, visionBox, condition, chunks, debugLines);
    }

    public T FindClosestItem<T>(Func<T, bool> condition = null) where T : Item
    {
        return ChunkManager.Manager.FindClosestItemWithinBoxPosSize(transform.position, visionBox, condition, chunks, debugLines);
    }

    private void OnDrawGizmos()
    {
        if (debugLines)
        {
            GeoFuncs.DrawBoxPosSize(transform.position, visionBox, Color.magenta);
        }
    }
}

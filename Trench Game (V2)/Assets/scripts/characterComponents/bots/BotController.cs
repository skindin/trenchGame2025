using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BotController : MonoBehaviour
{
    public Character character;
    public Vector2 visionBox;
    public bool wandering = false, dodging = false, debugLines = false;
    public float dangerRadius = 10,
        dodgeRadius = .1f,
        pointerSpeed = 50,
        minPointerMag = 1,
        maxPointerMag = 1,
        maxPointerOffset = .5f,
        maxTargetOffset = .5f,
        maxWanderOffset = 5;
        //wanderDistMemory = 5;
    Vector2 targetPos, targetPosOffset, pointerPos, targetPointerPos; //pointer pos and target pointer pos are in LOCAL space
    Chunk[,] chunks = default;

    //CURRENTLY NOT RESSETING ANY OF THIS WHEN BOTS DIE

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
            value = ChunkManager.Manager.ClampToWorld(value);
            targetPosOffset = (Vector2)UnityEngine.Random.insideUnitSphere * maxTargetOffset;
            targetPos = value;
            //path.Add(transform.position);
        }
    }

    public Vector2 OffsetTargetPos
    {
        get
        {
            return ChunkManager.Manager.ClampToWorld(TargetPos + targetPosOffset);
        }
    }

    public Item targetItem;
    public TrenchCollider targetCollider;
    public Character closestEnemy;
    //List<Vector2> path = new();

    private void Update()
    {

        //targetPos += Random.insideUnitCircle;
        //targetPos = ChunkManager.Manager.ClampPosToNearestChunk(targetPos);
        //var delta = targetPos - (Vector2)transform.position;
        //character.Move(delta);

        chunks = ChunkManager.Manager.ChunksFromBoxPosSize(transform.position, visionBox);

        SoldierLogic();

        //if (exploredPoints.Count > 0 && Time.deltaTime - prevMemLossStamp >= wanderMemoryDur)
        //{
        //    var arrayIndex = exploredPoints[0];
        //    exploredPoints.RemoveAt(0);

        //    unexploredPoints.Add(arrayIndex);
        //}
    }

    public void SoldierLogic ()
    {
        //closestEnemy = FindClosestCharacterWithinChunks<Character>(chunks);
        closestEnemy = FindClosestCharacter<Character>(character => character != this.character && 
        (character.inventory.ActiveWeapon || this.character.inventory.ActiveWeapon));

        if (targetCollider && !targetCollider.gameObject.activeInHierarchy) 
            targetCollider = null;

        if (targetItem && (!targetItem.gameObject.activeInHierarchy || targetItem.wielder))
            targetItem = null;

        if (closestEnemy && !closestEnemy.gameObject.activeInHierarchy)
        {
            closestEnemy = null;
        }

        if (!character.inventory.ActiveWeapon) //pickup a gun if you don't have one
        {
            var item = PickupClosestItem<Weapon>();
        }

        Gun gun = (character.inventory.ActiveWeapon is Gun a ? a : null);

        if (gun) //if you picked up a gun...
        {
            if (!targetCollider && closestEnemy && GeoUtils.TestBoxPosSize(transform.position, visionBox, closestEnemy.transform.position)) //and you have no target collider, but you do have a close enemy...
            {
                targetCollider = closestEnemy.collider; //target the closest enemy
            }

            if (gun.rounds <= 0) //and your gun is out of amo...
            {
                if (!gun.reloading && character.reserve.GetAmoAmount(gun.amoType) > 0) //and you didn't arlready start reloading and you have amo in the reserve...
                    gun.Action(); //start reloading the gun
            }
            else //and your gun has amo...
            {
                if (targetCollider && GeoUtils.TestBoxPosSize(transform.position, visionBox, targetCollider.transform.position)) //and collider target is within view...
                {
                    var direction = pointerPos;

                    var dist = Vector2.Distance(gun.BarrelPos, targetCollider.transform.position) + targetCollider.WorldSize/2;

                    var range = gun.range;

                    if (debugLines)
                        GeoUtils.DrawCircle(gun.BarrelPos, range, UnityEngine.Color.red, 8);

                    if (dist <= range) //if within range..
                    {
                        if (GeoUtils.DoesLineIntersectCircle(
                            targetCollider.transform.position, 
                            targetCollider.WorldSize / 2, 
                            transform.position, 
                            (Vector2)transform.position + range * direction.normalized,
                            //debugLines
                            false
                            )) //and within trajectory
                        {
                            gun.DirectionalAction(direction); //shoot at the collider
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
                    GeoUtils.DrawCircle(transform.position, dangerRadius, UnityEngine.Color.red, 8);

                if (closestEnemy && Vector2.Distance(closestEnemy.transform.position,transform.position) <= dangerRadius) //and you are too close to an enemy...
                {

                    Evade();
                    //Wander();
                }
            }
        }

        if (gun && gun.rounds > 0 && targetCollider)
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

            TargetPointerPos = TargetPos - (Vector2)transform.position;
        }

        if (!closestEnemy)
        {
            //var closestAmo = FindClosestItem<StackableItem>();
            //bool pickupAmo = closestAmo;

            var closestAmo = FindClosestItem<Ammo>();

            bool pickupAmo = false;

            if (!targetItem && closestAmo)
            {
                var pool = character.reserve.GetPool(closestAmo.type);
                if (pool.rounds < pool.maxRounds) //if there's room...
                    pickupAmo = true;
            }

            if (gun && character.reserve)
            {
                var deficit = gun.maxRounds - gun.rounds;

                if (deficit > 0 && character.reserve.GetAmoAmount(gun.amoType) > 0)
                {
                    gun.Action();
                }
            }

            if (pickupAmo)
            {
                TargetPos = closestAmo.transform.position;

                Wander(false);
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
            Wander(false);
        }


        //else //theres gotta be a better way to do this
        //{
        //    wandering = false;
        //}

        if (debugLines)
            GeoUtils.MarkPoint(TargetPos, .5f, UnityEngine.Color.magenta);

        //var moveDirection = TargetPos - (Vector2)transform.position;

        //var moveDirection = Vector2.MoveTowards(transform.position, TargetPos, 10) - (Vector2)transform.position;

        character.MoveToPos(TargetPos);
        //path[^1] = transform.position;

        pointerPos = Vector2.MoveTowards(pointerPos,TargetPointerPos,Time.deltaTime * pointerSpeed);

        if (debugLines)
        {
            GeoUtils.MarkPoint(targetPointerPos + (Vector2)transform.position, 1, UnityEngine.Color.red);
            GeoUtils.MarkPoint(pointerPos + (Vector2)transform.position, 1, UnityEngine.Color.blue);

            //GeoUtils.DrawLine(path, UnityEngine.Color.black);
        }

        for (int i = 0; i < unexploredPoints.Count; i++)
        {
            if (i == wanderIndex)
                continue;

            var arrayIndex = unexploredPoints[i];
            var point = wanderPoints[arrayIndex.x, arrayIndex.y];

            if (GeoUtils.TestBoxPosSize(transform.position, visionBox, point, debugLines))
            {
                unexploredPoints.RemoveAt(i);
                exploredPoints.Add(arrayIndex);
                if (wanderIndex > i)
                    wanderIndex--;
                i--;
            }
        }

        //if (gun)
            character.LookInDirection(pointerPos);
    }

    Vector2[,] wanderPoints = new Vector2[0,0];
    readonly List<Vector2Int> unexploredPoints = new(), exploredPoints = new();
    int wanderIndex = -1;

    public void Wander(bool wander = true)
    {
        if (!wander)
        {
            wandering = false;
            return;
        }

        if (!wandering)
        {
            if (unexploredPoints.Count < 1)
            {
                wanderPoints = ChunkManager.Manager.DistributePoints(visionBox,character.collider.WorldSize/2);

                unexploredPoints.Clear();
                exploredPoints.Clear();

                var height = wanderPoints.GetLength(1);

                for (int y = 0; y < height; y++)
                {
                    var width = wanderPoints.GetLength(0);

                    for (int x = 0; x < width; x++)
                    {
                        //wanderPoints[x, y] += UnityEngine.Random.insideUnitCircle * maxWanderOffset;
                        unexploredPoints.Add(new(x, y));
                    }
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
                exploredPoints.Add(unexploredPoints[wanderIndex]);
                unexploredPoints.RemoveAt(wanderIndex);
                foundPoint = true;
                //wanderIndex = -1;
            }

            if (wanderIndex < 0 || foundPoint)
            {
                LogicAndMath.GetClosest(transform.position, unexploredPoints.ToArray(), arrayPos => wanderPoints[arrayPos.x,arrayPos.y], out wanderIndex, null);

                if (wanderIndex > -1) // Ensure valid index
                {
                    var min = (Vector2)transform.position - visionBox / 2;
                    var max = (Vector2)transform.position + visionBox / 2;

                    var arrayPos = unexploredPoints[wanderIndex];
                    var wanderPoint = wanderPoints[arrayPos.x,arrayPos.y];

                    var closestVisiblePoint = Vector2.Max(wanderPoint, min);
                    closestVisiblePoint = Vector2.Min(closestVisiblePoint, max);

                    var delta = wanderPoint - closestVisiblePoint;

                    //if (debugLines)
                    //    GeoFuncs.MarkPoint(closestVisiblePoint, 1, Color.yellow);

                    TargetPos = (Vector2)transform.position + delta + UnityEngine.Random.insideUnitCircle * maxWanderOffset;

                    if (debugLines)
                        GeoUtils.MarkPoint(closestVisiblePoint, 1, UnityEngine.Color.yellow);
                }

            }

            if (wanderIndex < 0) // if there are no points left, redistribute points
            {
                wandering = false;
                Wander();
                return;
            }
        }

        if (debugLines)
        {
            var height = wanderPoints.GetLength(1);

            for (int y = 0; y < height; y++)
            {
                var width = wanderPoints.GetLength(0);

                for (int x = 0; x < width; x++)
                {
                    var arrayIndex = new Vector2Int(x, y);

                    UnityEngine.Color color;

                    var arrayIndexIndex = unexploredPoints.IndexOf(arrayIndex);

                    if (arrayIndexIndex != -1)
                    {
                        if (arrayIndexIndex == wanderIndex)
                            color = UnityEngine.Color.green;
                        else
                            color = UnityEngine.Color.red;
                    }
                    else
                        color = UnityEngine.Color.blue;

                    GeoUtils.MarkPoint(wanderPoints[x,y], 1, color);
                }
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

    private void OnDrawGizmos()
    {
        if (debugLines)
        {
            GeoUtils.DrawBoxPosSize(transform.position, visionBox, UnityEngine.Color.magenta);
        }
    }
}

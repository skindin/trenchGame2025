using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.VisualScripting;

public class Gun : Weapon
{
    public AmoReserve reserve;
    public int rounds = 10, reloadAnimRots = 3;
    public Coroutine reloadRoutine, fireRoutine;
    public Vector2 direction = Vector2.right, barrelPos;
    public bool safetyOff = true,
        holdingTrigger = false,
        reloading = false,
        startFull = true, //temporary, until i make the actual spawning script
        fired = false,
        drawBerrelPos = false;
    bool isFiring = false;

    public override string Verb { get; } = "shoot";

    public Vector2 BarrelPos
    {
        get 
        {
            return transform.rotation * barrelPos + transform.position; //doesn't account for scale!
        }
    }

    private GunModel cachedGunModel;
    public GunModel GunModel
    {
        get
        {
            if (cachedGunModel == null || cachedGunModel != itemModel)
            {
                cachedGunModel = (GunModel)itemModel;
            }
            return cachedGunModel;
        }
        set
        {
            itemModel = value;
            cachedGunModel = value; // Update the cache when the model changes
        }
    }

    Vector2 lastPos, velocity;

    public override void ResetItem()
    {
        base.ResetItem();

        safetyOff = true;
        holdingTrigger = reloading = fired = false;

        direction = Vector2.right;

        reloadRoutine = fireRoutine = null;

        reserve = null;
    }

    public override void ItemAwake()
    {
        base.ItemAwake();

        if (startFull)
            rounds = GunModel.maxRounds;
        else
            rounds = 0;
    }

    //bool GunLogic(Vector2 velocity = default)
    //{
    //    if (!safetyOff || reloading || (!GunModel.autoFire) && fired) return false;

    //    if (rounds <= 0)
    //    {
    //        if (GunModel.autoReload)
    //        {
    //            StartReload();
    //        }
    //        return false;
    //    }

    //    Aim((velocity == default) ? Vector2.up : velocity);

    //    var fireDeltaTime = Time.time - lastFireStamp;

    //    var secsPerBullet = 1 / GunModel.firingRate;

    //    if ((lastFireStamp == 0 && Time.time <= secsPerBullet) || fireDeltaTime > secsPerBullet)
    //    {
    //        int bulletCount;

    //        if (GunModel.autoFire)
    //        {
    //            bulletCount = Mathf.FloorToInt(GunModel.firingRate * Time.deltaTime);

    //            bulletCount = Mathf.Max(bulletCount, 1);

    //            bulletCount = Mathf.Min(bulletCount, rounds);
    //        }
    //        else
    //        {
    //            bulletCount = 1;
    //            fired = true;
    //        }

    //        for (int i = 0; i < bulletCount; i++)
    //        {
    //            Fire();
    //        }

    //        rounds -= bulletCount;

    //        //lastFireStamp = Time.time;

    //        if (rounds <= 0 && GunModel.autoReload)
    //        {
    //            StartReload();
    //        }
    //    }

    //    return true;
    //}

    public override void Attack (Vector2 direction = default)
    {
        //holdingTrigger = true;//not sure how this gonna work on server?
        //Aim(direction);

        Aim(direction);

        holdingTrigger = true;

        fireRoutine ??= StartCoroutine(Shoot());


        //GunLogic(direction);
    }

    IEnumerator Shoot ()
    {
        var secsPerBullet = 1 / GunModel.firingRate;

        while (true)
        {
            if (!holdingTrigger)
            {
                fireRoutine = null;
                yield break;
            }

            if (!reloading)
            {
                if (rounds > 0)
                {

                    var bulletCount = Mathf.FloorToInt(GunModel.firingRate * Time.deltaTime);

                    bulletCount = Mathf.Max(bulletCount, 1);

                    bulletCount = Mathf.Min(bulletCount, rounds);

                    for (int i = 0; i < bulletCount; i++)
                    {
                        Fire();
                    }

                    rounds -= bulletCount;
                }
                else
                {
                    if (!reloading && GunModel.autoReload)
                        Action();

                    fireRoutine = null;
                    yield break;
                }
            }

            if (!GunModel.autoFire) break;

            holdingTrigger = false; //makes sure that something else says it's pulling the trigger before it repeats

            yield return new WaitForSeconds(secsPerBullet);
        }

    }

    public override void Action ()
    {
        if (reloadRoutine == null)
        {
            reloadRoutine = StartCoroutine(Reload());
        }
    }

    IEnumerator Reload()
    {
        if (reloading || reserve == null) yield break;

        if (rounds < GunModel.maxRounds && reserve.GetAmoAmount(GunModel.amoType) > 0)
        {
            float elapsedTime = 0;

            reloading = true;

            while (elapsedTime < GunModel.reloadTime)
            {
                yield return null;

                elapsedTime += Time.deltaTime;
                var angle = ((GunModel.reloadTime - elapsedTime) / GunModel.reloadTime) * reloadAnimRots * 360;
                transform.rotation = Quaternion.FromToRotation(Vector2.up, direction) * Quaternion.AngleAxis(angle, Vector3.forward);
            }

            reloading = false;
            rounds += reserve.RemoveAmo(GunModel.amoType, GunModel.maxRounds - rounds);
        }

        reloadRoutine = null;
    }

    public override void Aim (Vector2 direction)
    {
        this.direction = direction;

        if (reloading) return;

        var angle = Vector2.SignedAngle(Vector2.up, direction);
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    public Bullet Fire ()
    {
        return ProjectileManager.Manager.NewBullet(transform.position + transform.rotation * barrelPos,
            (direction.normalized * GunModel.bulletSpeed) + velocity,
            GunModel.range,
            GunModel.DamagePerBullet,
            wielder);
    }

    public override void ItemUpdate()
    {
        base.ItemUpdate();

        if (!holdingTrigger)
        {
            fired = false;
        }
        holdingTrigger = false;

        //if (reloading)
        //{           

        //}

        velocity = ((Vector2)transform.position - lastPos) / Time.deltaTime;
        lastPos = transform.position;
    }

    private void OnDrawGizmos()
    {
        if (drawBerrelPos)    
            GeoUtils.MarkPoint(transform.position + transform.rotation * barrelPos,.2f,Color.blue);
    }

    public override string InfoString(string separator = " ")
    {
        var itemInfo = $"{(reloading ? "reloading..." + separator : "")}" + base.InfoString();

        var roundRatio = $"{rounds}/{GunModel.maxRounds}";
        var range = $"{GunModel.range:F1} m range";
        var bulletSpeed = $"{GunModel.bulletSpeed:F1} m/s";
        var fireRate = $"{GunModel.firingRate:F1} rounds/s";
        var damageRate = $"{GunModel.damageRate:F1} hp/s";
        var reload = $"{ GunModel.reloadTime:F1} s reload";
        var amoType = GunModel.amoType.name;

        var array = new string[] {itemInfo, roundRatio, fireRate, damageRate, range, bulletSpeed, reload, amoType};

        //var result = itemInfo.Concat(gunInfo).ToArray();

        return string.Join(separator, array);
    }

    //public override DataDict<object> PrivateData
    //{
    //    get 
    //    {
    //        var data = base.PrivateData;
    //            DataDict<object>.Combine(ref data,
    //            (Naming.rounds, rounds),
    //            (Naming.maxRounds, GunModel.maxRounds),
    //            (Naming.amoType, GunModel.amoType.name)
    //            );

    //        return data;
    //    }
    //}

    public override void Pickup(Character character, out bool wasPickedUp, out bool wasDestroyed)
    {
        base.Pickup(character,out wasPickedUp, out wasDestroyed);

        reserve = character.reserve;
    }

    public override void DropLogic(Vector2 pos, out bool wasDestroyed)
    {
        base.DropLogic(pos, out wasDestroyed);

        reserve = null;
        reloading = false;

        if (fireRoutine != null)
        {
            StopCoroutine(fireRoutine);
            fireRoutine = null;
        }

        if (reloadRoutine != null)
        {
            StopCoroutine(reloadRoutine);
            reloadRoutine = null; //idk if setting reload routine to null is neccessary shrugging emoji
        }
    }
}

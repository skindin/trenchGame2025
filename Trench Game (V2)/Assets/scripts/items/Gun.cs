using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Gun : Item
{
    public AmoReserve reserve;
    public int rounds = 10;
    float lastFireStamp = 0, reloadStartStamp = 0;
    public Vector2 direction = Vector2.right, barrelPos;
    public bool safetyOff = true,
        holdingTrigger = false,
        reloading = false,
        startFull = true, //temporary, until i make the actual spawning script
        fired = false,
        drawBerrelPos = false;

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
            if (cachedGunModel == null || cachedGunModel != model)
            {
                cachedGunModel = (GunModel)model;
            }
            return cachedGunModel;
        }
        set
        {
            model = value;
            cachedGunModel = value; // Update the cache when the model changes
        }
    }

    public override void ResetItem()
    {
        base.ResetItem();

        safetyOff = true;
        holdingTrigger = reloading = fired = false;

        direction = Vector2.right;

        lastFireStamp = reloadStartStamp = 0;

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

    bool GunLogic(Vector2 direction = default)
    {
        if (!safetyOff || reloading || (!GunModel.autoFire) && fired) return false;

        if (rounds <= 0)
        {
            if (GunModel.autoReload)
            {
                StartReload();
            }
            return false;
        }

        Aim((direction == default) ? Vector2.up : direction);

        var fireDeltaTime = Time.time - lastFireStamp;

        var secsPerBullet = 1 / GunModel.firingRate;

        if ((lastFireStamp == 0 && Time.time <= secsPerBullet) || fireDeltaTime > secsPerBullet)
        {
            int bulletCount;

            if (GunModel.autoFire)
            {
                bulletCount = Mathf.FloorToInt(GunModel.firingRate * Time.deltaTime);

                bulletCount = Mathf.Max(bulletCount, 1);

                bulletCount = Mathf.Min(bulletCount, GunModel.maxPerFrame, rounds);
            }
            else
            {
                bulletCount = 1;
                fired = true;
            }

            for (int i = 0; i < bulletCount; i++)
            {
                Fire();
            }

            rounds -= bulletCount;

            lastFireStamp = Time.time;

            if (rounds <= 0 && GunModel.autoReload)
            {
                StartReload();
            }
        }

        return true;
    }

    public bool Trigger (Vector2 direction = default) //direction parameter to ensure the bullet fires in the right direction across all connections
    {
        holdingTrigger = true;//not sure how this gonna work on server?

        return GunLogic(direction);
    }

    //networking procedure
    //1. client runs fire logic, which can only spawn ghost bullets (bullets that can't actually damage anything, though they are destroyed when they hit something)
    //2. client sends rpc to server containing direction and time stamp
    //3. server runs trigger logic, spawning real bullets
    //4. server sends rpc to all other clients containing direction and time stamp
    //5. clients run trigger logic and spawn ghost bullets

    public override void ItemUpdate()
    {
        base.ItemUpdate();

        if (!holdingTrigger)
        {
            fired = false;
        }
        holdingTrigger = false;

        if (reloading)
        {
            var reloadClock = Mathf.Min(Time.time - reloadStartStamp, GunModel.reloadTime);

            if (reloadClock >= GunModel.reloadTime)
            {
                Reload();
                reloadStartStamp = Time.time;
                reloading = false;
            }
                
            var angle = ((GunModel.reloadTime - reloadClock) / GunModel.reloadTime) * GunModel.reloadAnimRots * 360;
            transform.rotation = Quaternion.FromToRotation(Vector2.up, direction) * Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    void Reload ()
    {
        rounds += reserve.RemoveAmo(GunModel.amoType, GunModel.maxRounds - rounds);
    }

    //void TakeAllAmoFromThis (AmoReserve recievingReserve)
    //{
    //    var amoPool = recievingReserve.GetPool(GunModel.amoType);
    //    var space = amoPool.maxRounds - amoPool.rounds;
    //    var addend = Mathf.Min(space, rounds);
    //    rounds -= addend;
    //    amoPool.AddAmo(addend); //need to make this TakeAmoFromThis, and put amo in reserve
    //}

    public void StartReload()
    {
        if (reloading || reserve == null) return;

        if (rounds < GunModel.maxRounds && reserve.GetAmoAmount(GunModel.amoType) > 0)
        {
            reloading = true;
            reloadStartStamp = Time.time;
        }
    }

    public void Aim (Vector2 direction)
    {
        if (reloading) return;

        this.direction = direction;

        var angle = Vector2.SignedAngle(Vector2.up, direction);
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    public Bullet Fire ()
    {
        return ProjectileManager.Manager.NewBullet(transform.position + transform.rotation * barrelPos, direction.normalized * GunModel.bulletSpeed, GunModel.range, GunModel.DamagePerBullet, wielder);
    }

    private void OnDrawGizmos()
    {
        if (drawBerrelPos)    
            GeoFuncs.MarkPoint(transform.position + transform.rotation * barrelPos,.2f,Color.blue);
    }

    public override string InfoString(string separator = " ")
    {
        var itemInfo = base.InfoString();

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
    }
}

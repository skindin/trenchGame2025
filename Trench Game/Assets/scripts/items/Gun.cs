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
    public bool enabled = true,
        holdingTrigger = false,
        reloading = false,
        maxRounds = false, //temporary, until i make the actual spawning script
        fired = false,
        drawBerrelPos = false;

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

    public override void ItemAwake()
    {
        base.ItemAwake();

        if (maxRounds)
            rounds = GunModel.maxRounds;
        else
            rounds = Mathf.Clamp(rounds, 0, GunModel.maxRounds);
    }

    public bool GunLogic(Vector2 direction = default)
    {
        if (!enabled || reloading || (!GunModel.autoFire) && fired) return false;

        if (rounds <= 0)
        {
            if (GunModel.autoReload)
            {
                StartReload();
            }
            return false;
        }

        if (direction != Vector2.zero) this.direction = direction;

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
        rounds = reserve.RemoveAmo(GunModel.amoType, GunModel.maxRounds - rounds);
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

        if (reserve.GetAmoAmount(GunModel.amoType) > 0)
        {
            reloading = true;
            reloadStartStamp = Time.time;
        }
    }

    public void Aim (Vector2 direction)
    {
        this.direction = direction;

        if (reloading) return;

        var angle = Vector2.SignedAngle(Vector2.up, direction);
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    public Bullet Fire ()
    {
        return ProjectileManager.Manager.NewBullet(transform.position + transform.rotation * barrelPos, direction.normalized * GunModel.bulletSpeed, GunModel.range, wielder);
    }

    private void OnDrawGizmos()
    {
        if (drawBerrelPos)    
            GeoFuncs.MarkPoint(transform.position + transform.rotation * barrelPos,.2f,Color.blue);
    }

    public override string GetInfo(string separator = " ")
    {
        var itemInfo = base.GetInfo();

        var roundRatio = $"{rounds}/{GunModel.maxRounds}";
        var range = $"{GunModel.range} m range";
        var bulletSpeed = $"{GunModel.bulletSpeed} m/s";
        var fireRate = $"{GunModel.firingRate} rounds/s";
        var reload = $"{GunModel.reloadTime} s reload";
        var amoType = GunModel.amoType.name;

        var array = new string[] {itemInfo, roundRatio, fireRate, range, bulletSpeed, reload, amoType};

        //var result = itemInfo.Concat(gunInfo).ToArray();

        return string.Join(separator, array);
    }

    public override bool Pickup(Character character)
    {
        base.Pickup(character);

        reserve = character.reserve;
        return true;
    }

    public override void Drop()
    {
        base.Drop();

        reserve = null;
        reloading = false;
    }
}

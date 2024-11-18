using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.VisualScripting;

public class Gun : Weapon
{
    public AmmoReserve reserve;
    public int rounds = 10, reloadAnimRots = 3;
    public Coroutine reloadRoutine, fireRoutine;
    public Vector2 direction = Vector2.right, barrelPos;
    public bool safetyOff = true,
        holdingTrigger = false,
        reloading = false,
        startFull = true, //temporary, until i make the actual spawning script
        fired = false,
        drawBerrelPos = false;
    //bool isFiring = false;

    public override string Verb { get; } = "shoot";

    public Vector2 BarrelPos
    {
        get 
        {
            return transform.TransformPoint(barrelPos); //doesn't account for scale!
        }
    }

    public float bulletSpeed, range, firingRate, reloadTime = 2, damageRate = 5, swapDelay = .2f;
    public int maxPerFrame = 5, maxRounds = 10;//, reloadAnimRots = 3;
    public AmmoType amoType;
    public bool autoFire = true, autoReload = false;
    //public bool released = true;

    public float DamagePerBullet
    {
        get
        {
            return damageRate / firingRate;
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

    float timeDeactivated;

    public override void ToggleActive(bool active)
    {
        base.ToggleActive(active);

        if (!active)
        {
            //StopAllCoroutines(); //should suffice for now
                                 //fireRoutine = 
            if (reloadRoutine != null)
            {
                StopCoroutine(reloadRoutine);
                reloadRoutine = null;
            }
            reloading = false; //maaan idk how to design the delay. using the cooldown is way to long, requiring click is too hard

            if (fireRoutine != null)
            {
                StopCoroutine(fireRoutine);
                fireRoutine = null;
            }

            timeDeactivated = Time.time;
        }
        else
        {
            var secsPerBullet = 1 / firingRate;
            var rateDelay = Mathf.Max(secsPerBullet - Time.time - timeDeactivated, 0);
            StartShootRoutine(swapDelay + rateDelay);
        }
        //else
        //{
        //    released = false;
        //}
        //else //requiring mouse lift makes a lot more sense
        //{
        //    StartShootRoutine(true);
        //}
    }

    public override void ItemAwake()
    {
        base.ItemAwake();

        if (startFull)
            rounds = maxRounds;
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

    public override void DirectionalAction(Vector2 direction = default)
    {
        //holdingTrigger = true;//not sure how this gonna work on server?
        //Aim(direction);

        Aim(direction);

        holdingTrigger = true;

        StartShootRoutine();
    }

    void StartShootRoutine (float delay = 0)
    {
        //if (released)
        fireRoutine ??= StartCoroutine(Shoot(delay));

        IEnumerator Shoot(float delay = 0)
        {
            if (delay > 0)
            {
                yield return new WaitForSeconds(delay);
            }

            var secsPerBullet = 1 / firingRate;

            while (true)
            {
                if (!holdingTrigger)
                {
                    //released = true;
                    fireRoutine = null;
                    yield break;
                }

                if (!reloading)
                {
                    if (rounds > 0)
                    {

                        var bulletCount = Mathf.FloorToInt(firingRate * Time.deltaTime);

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
                        if (!reloading && autoReload) //inconsistant af to only test reloading property here
                            Action();

                        fireRoutine = null;
                        yield break;
                    }
                }

                if (!autoFire) break;

                holdingTrigger = false; //makes sure that something else says it's pulling the trigger before it repeats

                float startTime = Time.time;

                do
                {
                    yield return null;
                }
                while (Time.time - startTime < secsPerBullet); //accounts for being paused

                //yield return new WaitForSeconds(secsPerBullet);
            }

        }
    }

    public override void Aim(Vector2 direction)
    {
        //direction = (Vector2)(transform.position - wielder.transform.position);

        //Debug.DrawRay(transform.position, direction.normalized * 1.5f, Color.blue);

        this.direction = direction;

        if (reloading) return;

        onAim.Invoke(direction);

        NetworkManager.Manager.SyncDirection(wielder,direction);
    }

    public override void Action()
    {
        reloadRoutine ??= StartCoroutine(Reload(0, true));
    }

    IEnumerator Reload(float progress = 0, bool sync = false)
    {
        if (reloading || reserve == null) yield break;

        if (!sync || (rounds < maxRounds && reserve.GetAmoAmount(amoType) > 0))
        {
            reloading = true;

            if (sync)
            {
                NetworkManager.Manager.StartReload(this);
            }

            var initialRotation = transform.localRotation.eulerAngles.z;

            while (progress < reloadTime)
            {
                yield return null;

                progress += Time.deltaTime;
                var angle = ((reloadTime - Mathf.Min(progress, reloadTime)) / reloadTime) * reloadAnimRots * 360;
                transform.localRotation = Quaternion.AngleAxis(angle + initialRotation, Vector3.forward);
            }

            //transform.localRotation = Quaternion.Euler(new(0, 0, initialRotation));

            reloading = false;
            rounds += reserve.RemoveAmo(amoType, maxRounds - rounds);

            if (NetworkManager.IsServer)
            {
                var gunData = new GunData { Amo = rounds};

                var itemData = new ItemData { ItemId = id, Gun = gunData };

                NetworkManager.Manager.server.UpdateItemData(itemData);
            }
        }

        reloadRoutine = null;
    }

    public void StartReload (float startTime)
    {
        var progress = NetworkManager.NetTime - startTime;

        reloadRoutine ??= StartCoroutine(Reload(progress, false));
    }

    public Bullet Fire ()
    {
        var bullet = ProjectileManager.Manager.NewBullet(BarrelPos,
            (direction.normalized * bulletSpeed) + velocity,
            range,
            DamagePerBullet,
            wielder);

        NetworkManager.Manager.SpawnBullet(this,bullet);

        return bullet;
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
        var itemInfo = $"{(reloading ? "(reloading...)" + separator : "")}" + base.InfoString();

        var roundRatio = $"{rounds}/{maxRounds}";
        var range = $"{this.range:F1} m range";
        var bulletSpeed = $"{this.bulletSpeed:F1} m/s";
        var fireRate = $"{firingRate:F1} rounds/s";
        var damageRate = $"{this.damageRate:F1} hp/s";
        var reload = $"{ reloadTime:F1} s reload";
        var amoType = this.amoType.name;

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

    public override void Pickup(Character character, out bool wasPickedUp, out bool wasDestroyed, bool sync)
    {
        base.Pickup(character,out wasPickedUp, out wasDestroyed, sync);

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

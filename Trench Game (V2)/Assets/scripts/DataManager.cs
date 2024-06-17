using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static DataManager;
using static UnityEditor.Progress;
using Newtonsoft.Json;

public static class DataManager //might be better for all these data types to be structs...? but idk
{
    [System.Serializable]
    public class BaseItemData : JsonAble<BaseItemData>
    {
        public int id, wielderId;
        public string modelName;
        public List<string> tags = new List<string>();
        Vector2 pos;

        public BaseItemData(Item item)
        {
            id = item.id;
            modelName = item.model.name;
            wielderId = item.wielder.id;

            tags = LogicAndMath.GetValuesList(item.model.Tags, tag => tag.ToString());

            pos = item.transform.position;
            //modelData = new ItemModelData(item.model);
        }
    }

    [System.Serializable]
    public abstract class StackData : BaseItemData
    {
        public int amount;

        public StackData(StackableItem stack) : base(stack)
        {
            amount = stack.amount;
        }
    }

    [System.Serializable]
    public class AmoData : StackData
    {

        public string type;

        public AmoData(Amo amo) : base(amo)
        {
            type = amo.AmoModel.type.name;
        }
    }

    [System.Serializable]
    public class GunData : BaseItemData
    {
        public int rounds;
        public Vector2 direction;
        public float bulletSpeed, range, firingRate, reloadTime = 2, damageRate = 5;
        public int maxPerFrame = 5, maxRounds = 10, reloadAnimRots = 3;
        public string amoType;
        public bool autoFire = true, autoReload = false;

        public GunData (Gun gun) : base(gun)
        {
            rounds = gun.rounds;
            direction = gun.direction;

            var model = gun.GunModel;

            bulletSpeed = model.bulletSpeed;
            range = model.range;
            firingRate = model.firingRate;
            reloadTime = model.reloadTime;
            damageRate = model.damageRate;
            maxRounds = model.maxRounds;

            amoType = model.amoType.name;

            autoFire = model.autoFire;
            autoReload = model.autoReload;
        }
    }

    public static TData GetItemData<TItem, TData>(TItem item) where TItem : Item where TData : BaseItemData
    {
        //return new TData(item);

        var constructor = typeof(TData).GetConstructor(new Type[] { typeof(TItem) });
        if (constructor == null)
        {
            throw new ArgumentException($"Type {typeof(TData).Name} does not have a constructor that accepts a {typeof(TItem).Name} parameter.");
        }

        return (TData)constructor.Invoke(new object[] { item });
    }

    //public static TData GetData<TItem, TData>(TItem item, Type dataType) where TItem : Item where TData : ItemData
    //{
    //    return (TData)Activator.CreateInstance(dataType, item);
    //}

    public abstract class BaseCharacterData<TGunData> : JsonAble<BaseCharacterData<TGunData>> where TGunData : BaseItemData
    {
        public int id;
        public string name;
        public float hp, maxHp;
        public TGunData gun = null;

        public BaseCharacterData(Character character)
        {
            id = character.id;
            name = character.Name;
            hp = character.hp;
            maxHp = character.maxHp;
        }
    }

    public class PrivateCharacterData : BaseCharacterData<GunData>
    {
        public PrivateCharacterData (Character character) : base(character)
        {
            if (character.gun)
                gun = GetItemData<Gun, GunData>(character.gun);
        }
    }

    public static PrivateCharacterData GetPrivateCharacterData(Character character)
    {
        return new PrivateCharacterData(character);
    }

    public class PublicCharacterData : BaseCharacterData<BaseItemData>
    {
        public PublicCharacterData(Character character) : base(character)
        {
            if (character.gun)
                gun = GetItemData<Gun, BaseItemData>(character.gun);
        }
    }

    public static PublicCharacterData GetPublicCharacterData(Character character)
    {
        return new PublicCharacterData(character);
    }

    public class TestScript
    {
        Amo amo;

        void Test ()
        {
            var amoItemData = GetItemData<Amo,BaseItemData>(amo);
        }
    }
}

[System.Serializable]
public class JsonAble<T> where T : class
{
    //public bool HasValue = false;

    ////public JsonAble()
    ////{
    ////    HasValue = false;
    ////}

    //public JsonAble(bool wasSet)
    //{
    //    HasValue = wasSet;
    //}

    public string ToJson()
    {
        //if (!HasValue)
        //{
        //    return "{}";
        //}
        return JsonConvert.SerializeObject(this);
    }

    public static T FromJson(string json)
    {
        if (string.IsNullOrEmpty(json) || json == "{}")
        {
            return null;
        }
        else
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
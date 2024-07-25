using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Google.Protobuf;
//using Newtonsoft.Json;
////using static DataManager;
////using static UnityEditor.Progress;
//using Newtonsoft.Json.Linq;

public static class DataManager
{
    public static byte[] MessageToBinary (IMessage message)
    {
        byte[] binaryData;
        using (var stream = new MemoryStream())
        {
            message.WriteTo(stream);
            binaryData = stream.ToArray();
        }

        return binaryData;
    }

    public static byte[] VectorToBinary(Vector2 pos)
    {
        Vector2Data posData = new() { X = pos.x, Y = pos.y };

        return MessageToBinary(posData);
    }

    public static bool IfGetVector(byte[] bytes, out Vector2 pos)
    {
        if (IfGet<Vector2Data>(bytes,out var vectorData))
        {
            pos = new Vector2(vectorData.X,vectorData.Y);
            return true;
        }

        pos = Vector2.zero;
        return false;
    }

    public static bool IfGet<T>(byte[] binaryData, out T result) where T : IMessage, new()
    {
        result = default;
        try
        {
            // Create an instance of the protobuf message type
            result = new T();

            // Deserialize the binary data into the message
            result = (T)result.Descriptor.Parser.ParseFrom(binaryData);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return false;
        }
    }
}

//public static class DataManager //might be better for all these data types to be structs...? but idk
//{
//    [System.Serializable]
//    public class BaseItemData : JsonAble<BaseItemData>
//    {
//        public int id, wielderId;
//        public string modelName;
//        public List<string> tags = new List<string>();
//        Vector2Data pos;

//        public BaseItemData(Item item)
//        {
//            id = item.id;
//            modelName = item.itemModel.name;
//            wielderId = item.wielder.id;

//            tags = LogicAndMath.GetValuesList(item.itemModel.Tags, tags, tag => tag.ToString());

//            pos = new(item.transform.position);
//            //modelData = new ItemModelData(item.model);
//        }
//    }

//    [System.Serializable]
//    public abstract class StackData : BaseItemData
//    {
//        public int amount;

//        public StackData(StackableItem stack) : base(stack)
//        {
//            amount = stack.amount;
//        }
//    }

//    [System.Serializable]
//    public class AmoData : StackData
//    {

//        public string type;

//        public AmoData(Ammo amo) : base(amo)
//        {
//            type = amo.AmoModel.type.name;
//        }
//    }

//    [System.Serializable]
//    public class WeaponData : BaseItemData
//    {
//        public WeaponData (Weapon weapon) : base(weapon)
//        {

//        }
//    }

//    [System.Serializable]
//    public class GunData : BaseItemData
//    {
//        public int rounds;
//        public Vector2Data direction;
//        public float bulletSpeed, range, firingRate, reloadTime = 2, damageRate = 5;
//        public int maxPerFrame = 5, maxRounds = 10, reloadAnimRots = 3;
//        public string amoType;
//        public bool autoFire = true, autoReload = false;

//        public GunData (Gun gun) : base(gun)
//        {
//            rounds = gun.rounds;
//            direction = new(gun.direction);

//            var model = gun.GunModel;

//            bulletSpeed = model.bulletSpeed;
//            range = model.range;
//            firingRate = model.firingRate;
//            reloadTime = model.reloadTime;
//            damageRate = model.damageRate;
//            maxRounds = model.maxRounds;

//            amoType = model.amoType.name;

//            autoFire = model.autoFire;
//            autoReload = model.autoReload;
//        }
//    }

//    public static TData GetItemData<TItem, TData>(TItem item) where TItem : Item where TData : BaseItemData
//    {
//        //return new TData(item);

//        var constructor = typeof(TData).GetConstructor(new Type[] { typeof(TItem) });
//        if (constructor == null)
//        {
//            throw new ArgumentException($"Type {typeof(TData).Name} does not have a constructor that accepts a {typeof(TItem).Name} parameter.");
//        }

//        return (TData)constructor.Invoke(new object[] { item });
//    }

//    public static BaseItemData GetItemData(Item item)
//    {
//        if (item is Gun gun)
//        {
//            return new GunData(gun);
//        }
//        else if (item is Ammo amo)
//        {
//            return new AmoData(amo);
//        }
//        else
//        {
//            return GetItemData<Item, BaseItemData>(item);
//        }
//    }


//    //public static TData GetData<TItem, TData>(TItem item, Type dataType) where TItem : Item where TData : ItemData
//    //{
//    //    return (TData)Activator.CreateInstance(dataType, item);
//    //}

//    public abstract class BaseCharacterData<TItemData> : JsonAble<BaseCharacterData<TItemData>> where TItemData : BaseItemData
//    {
//        public int id;
//        public string name;
//        public float hp, maxHp;
//        public Vector2Data pos;
//        public TItemData gun = null;
//        //public bool reloading;

//        public BaseCharacterData(Character character)
//        {
//            id = character.id;
//            name = character.Name;
//            hp = character.hp;
//            maxHp = character.maxHp;
//            pos = new(character.transform.position);
//            //reloading = character.gun && character.gun.reloading;
//        }
//    }

//    public class PrivateCharacterData : BaseCharacterData<WeaponData>
//    {
//        public Dictionary<string,AmoReserveData> amoReserve = new();

//        public PrivateCharacterData (Character character) : base(character)
//        {
//            if (character.inventory.ActiveWeapon)
//                gun = GetItemData<Weapon, WeaponData>(character.inventory.ActiveWeapon);

//            if (character.reserve)
//            {
//                foreach (var pool in character.reserve.ammoPools)
//                {
//                    amoReserve.Add(pool.type.name, new(pool));
//                }
//            }
//        }

//        [System.Serializable]
//        public class AmoReserveData
//        {
//            public int rounds, maxRounds;

//            public AmoReserveData(AmoPool pool)
//            {
//                rounds = pool.rounds;
//                maxRounds = pool.maxRounds;
//            }
//        }

//    }

//    public static PrivateCharacterData GetPrivateCharacterData(Character character)
//    {
//        return new PrivateCharacterData(character);
//    }

//    public class PublicCharacterData : BaseCharacterData<BaseItemData>
//    {
//        public PublicCharacterData(Character character) : base(character)
//        {
//            if (character.inventory.ActiveWeapon)
//                gun = GetItemData<Item, BaseItemData>(character.inventory.ActiveItem);
//        }
//    }

//    public static PublicCharacterData GetPublicCharacterData(Character character)
//    {
//        return new PublicCharacterData(character);
//    }

//    public class TestScript
//    {
//        Ammo amo;

//        void Test ()
//        {
//            var amoItemData = GetItemData<Ammo,BaseItemData>(amo);
//        }
//    }
//}

//[System.Serializable]
//public class JsonAble<T> where T : class
//{
//    //public bool HasValue = false;

//    ////public JsonAble()
//    ////{
//    ////    HasValue = false;
//    ////}

//    //public JsonAble(bool wasSet)
//    //{
//    //    HasValue = wasSet;
//    //}

//    public string ToJson()
//    {
//        //if (!HasValue)
//        //{
//        //    return "{}";
//        //}
//        return JsonConvert.SerializeObject(this);

//        //return "";
//    }

//    public static T FromJson(string json)
//    {
//        if (string.IsNullOrEmpty(json) || json == "{}")
//        {
//            return null;
//        }
//        else
//        {
//            return JsonConvert.DeserializeObject<T>(json);
//        }

//        //return null;
//    }
//}

//[System.Serializable]
//public struct Vector2Data
//{
//    public float x, y;

//    public Vector2Data(Vector2 vector)
//    {
//        x = vector.x;
//        y = vector.y;
//    }
//}
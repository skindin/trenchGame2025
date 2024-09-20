using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Google.Protobuf;
using Google.Protobuf.Collections;
//using Newtonsoft.Json;
////using static DataManager;
////using static UnityEditor.Progress;
//using Newtonsoft.Json.Linq;

public static class DataManager
{
    //public static byte[] MessageToBinary (IMessage message)
    //{
    //    return message.ToByteArray();

    //    //byte[] binaryData;
    //    //using (var stream = new MemoryStream())
    //    //{
    //    //    message.WriteTo(stream);
    //    //    binaryData = stream.ToArray();
    //    //}

    //    //return binaryData;
    //}


    //public static byte[] VectorToBinary(Vector2 pos)
    //{
    //    return MessageToBinary(VectorToData(pos));
    //}

    public static Vector2Data VectorToData (Vector2 pos)
    {
        return new() { X = pos.x, Y = pos.y };
    }

    public static Vector2 DataToVector(Vector2Data data)
    {
        return new Vector2(data.X,data.Y);
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
            Debug.Log(ex.Message);
            return false;
        }
    }
    

    /// <summary>
    /// Modifies a to have any new values from b
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    public static void CombineCharData (CharacterData a, CharacterData b)
    {
        if (b.HasName)
        {
            a.Name = b.Name;
        }

        if (b.Pos != null)
        {
            a.Pos = b.Pos;
        }

        if (b.HasAngle)
        {
            a.Angle = b.Angle;
        }

        if (b.HasHp) //i shouldn't be sending the hp when it's at the default value but idc rn
        {
            a.Hp = b.Hp; 
        }

        if (b.HasItemId)
        {
            a.ItemId = b.ItemId;
        }

        if (b.HasLimbo)
        {
            a.Limbo = b.Limbo;
        }

        //if (b.Reserve != null) nvm this too complicated rn
        //{
        //    if (a.Reserve != null)
        //    {

        //    }
        //    else
        //    {
        //        foreach (var)
        //    }
        //}
    }

    public static void CombineCharDataList(CharacterData charData, CharDataList list)
    {
        //bool found = false;
        foreach (var listChar in list.List)
        {
            if (charData.CharacterID == listChar.CharacterID)
            {
                CombineCharData(listChar, charData);
                //found = true;
                return;
            }
        }

        //if (!found)
        //{
            list.List.Add(charData);
        //}
    }

    /// <summary>
    /// Modifies a to have any new values from b
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    public static void CombineItemData (ItemData a, ItemData b) //this is great, but should really remove data when they reach the default value
    {
        if (a == null)
        {
            Debug.Log("a is null");
        }

        if (b == null)
        {
            Debug.Log("b is null");
        }

        if (a == null || b == null)
        {
            return;
        }

        switch (b.TypeCase)
        {
            case ItemData.TypeOneofCase.Gun:
                {
                    if (a.Gun == null)
                    {
                        a.Gun = b.Gun;
                    }
                    else
                    {
                        if (b.Gun.HasAmo)
                        {
                            a.Gun.Amo = b.Gun.Amo;
                        }

                        if (b.Gun.HasReloadStart)
                        {
                            a.Gun.ReloadStart = b.Gun.ReloadStart;
                        }
                    }
                }
                break;

            case ItemData.TypeOneofCase.Stack:
                {
                    if (a.Stack == null)
                    {
                        a.Stack = b.Stack;
                    }
                    else //this will work as long as the amount isn't optional
                    {
                       a.Stack.Amount = b.Stack.Amount;
                    }
                }
                break;

            case ItemData.TypeOneofCase.Consumable:
                {
                    if (a.Consumable == null)
                    {
                        a.Consumable = b.Consumable;
                    }
                    else
                    {
                        a.Consumable.ConsumeStart = b.Consumable.ConsumeStart;
                    }
                }
                break;
        }
    }

    public static void CombineItemDataList (ItemData data, RepeatedField<ItemData> list)
    {
        foreach (var otherData in list)
        {
            if (otherData.ItemId == data.ItemId)
            {
                CombineItemData(otherData, data);
                return;
            }
        }

        list.Add(data);
    }

    public static Item CreateItemWithData(ItemData data)
    {
        var newItem = ItemManager.Manager.NewItem(data.PrefabId, data.ItemId);
        if (data.Pos != null)
        {
            var pos = DataToVector(data.Pos);
            newItem.Drop(pos);
        }

        ModifyItemWithData(newItem, data);

        return newItem;
    }

    public static Item UpdateItemWithData (ItemData data)
    {
        var item = ItemManager.Manager.active[data.ItemId];

        if (data.Pos != null)
        {
            var pos = DataToVector(data.Pos);

            if (item.wielder)
            {
                item.wielder.inventory.DropItem(item, pos);
            }
            else
            {
                item.Drop(pos);
            }
        }

        ModifyItemWithData(item, data);

        return item;
    }

    static void ModifyItemWithData (Item item, ItemData data)
    {
        switch (data.TypeCase)
        {
            case ItemData.TypeOneofCase.Gun:
                {
                    if (item is Gun gun)
                    {
                        if (data.Gun.HasAmo)
                        {
                            gun.rounds = data.Gun.Amo;
                        }

                        if (data.Gun.HasReloadStart)
                        {
                            if (NetworkManager.NetTime - data.Gun.ReloadStart > gun.GunModel.reloadTime)
                                data.Gun.ClearReloadStart();
                            else
                                gun.StartReload(data.Gun.ReloadStart);
                        }
                    }
                    else
                    {
                        Debug.LogError($"item {item.id} was given gun data, but it is not a gun");
                    }
                }
                break;

            case ItemData.TypeOneofCase.Stack:
                {
                    if (item is StackableItem stack)
                    {
                        stack.amount = data.Stack.Amount;
                    }
                    else
                    {
                        Debug.LogError($"item {item.id} was given stack data, but it is not a stack");
                    }
                }
                break;

            case ItemData.TypeOneofCase.Consumable:
                {
                    if (item is MedPack medPack)
                    {
                        medPack.StartHeal(data.Consumable.ConsumeStart);
                    }
                    else
                    {
                        Debug.LogError($"item {item.id} was given medpack/consumable data, but it is neither");
                    }
                }
                break;
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
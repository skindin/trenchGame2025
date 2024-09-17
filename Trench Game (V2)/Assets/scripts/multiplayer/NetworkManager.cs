//using System.Collections;
//using System.Collections.Generic;
//using System.IO;
//using System;
using Google.Protobuf;
using System;
using UnityEngine;
using UnityEngine.TextCore.Text;
//using System;

public class NetworkManager : ManagerBase<NetworkManager>
{
    //string id;
    public static bool IsServer{ get {
#if UNITY_EDITOR
        return Manager.editorIsServer;
#elif UNITY_SERVER
        return true;
#else
        return false;
#endif
        }}
    public bool editorIsServer;

    public GameClient client;
    public GameServer server;

    //float time;
    public float NetTime;

    public void SetPos(Vector2 pos, int id)
    {
        var posData = DataManager.VectorToData(pos);


        if (!IsServer)
        {

            var input = new PlayerInput { Pos = posData };

            var baseMessage = new MessageForServer { Input = input };

            //var binary = DataManager.MessageToBinary(baseMessage);

            client.SendData(baseMessage.ToByteArray());

            //Debug.Log($"sent pos {pos} to server");
        }
        else {

            var charData = new CharacterData { Pos = posData, CharacterID = id };

            //server.updateCharData.List.Add(charData);

            server.UpdateCharData(charData);

            //server.SendDataDisclude(message.ToByteArray());

            //PosSyncsPerFrame++;

            //Debug.Log($"server character {id} moved to {pos}");
            }
        }

    public void SetName(string newName) //this hasn't been changed to combine with movement data but it probably should eventually
    {
        if (IsServer)
            return;

        var baseMessage = new MessageForServer() { Input = new PlayerInput { Name = newName } };

        client.SendData(baseMessage.ToByteArray());

        Debug.Log($"sent characterName {newName} to server");

        //CharacterManager.Manager.localPlayerCharacter.name = newName;

    }

    public void PickupItem(Item item, Vector2 dropPos)
    {
        if (IsServer)
            return;

        var posData = DataManager.VectorToData(dropPos);

        var input = new PlayerInput { PickupItem = item.id , LookPos = posData};

        var message = new MessageForServer {Input = input};

        client.SendData(message.ToByteArray());

        Debug.Log($"told server it picked up item {item.id}");
    }

    //public void DropItemServer(Item item, Vector2 dropPos)
    //{
    //    var posData = DataManager.VectorToData(dropPos);

    //    var itemData = new ItemData { ItemId = item.id , Pos = posData};
    //}

    public void DropItemClient (Vector2 pos)
    {
        if (IsServer)
            return;

        var posData = DataManager.VectorToData (pos);

        var input = new PlayerInput { DropItem = true , LookPos = posData };

        var message = new MessageForServer { Input = input };

        client.SendData(message.ToByteArray());

        Debug.Log($"told server it dropped current item");
    }

    public void ServerRemoveItem (Item item)
    {
        if (!IsServer)
            return;

        server.removeItemList.Add(item.id);

        for (int i = 0; i < server.updateItems.Count; i++)
        {
            var updateItem = server.updateItems[i];

            if (updateItem.ItemId == item.id)
            {
                server.updateItems.RemoveAt(i);
                i--;
                break;
            }
        }

        for (int i = 0; i < server.currentItems.Count; i++)
        {
            var currentItem = server.currentItems[i];

            if (currentItem.ItemId == item.id)
            {
                server.currentItems.RemoveAt(i);
                Debug.Log($"removed item data {item.id}");
                i--;
                break;
            }
        }
    }

    public void SetHealth(Character character, float health)
    {
        if (!IsServer)
            return;

        var charData = new CharacterData { CharacterID = character.id, Hp = health };

        server.UpdateCharData(charData);
    }

    public void SpawnBullet(Bullet bullet)
    {
        if (IsServer)
            return;

        var bunch = new BulletBunch { StartTime = NetTime};

        var startPosData = DataManager.VectorToData(bullet.startPos);
        var endPosData = DataManager.VectorToData(bullet.startPos + bullet.velocity.normalized * bullet.range);

        bunch.Bullets.Add(new BulletData { Startpos = startPosData, Endpos = endPosData });

        var input = new PlayerInput { Bullets = bunch };

        var message = new MessageForServer { Input = input, Time = NetTime };

        client.SendData(message.ToByteArray());

        //XPlatformLog($"told server to spawn a bullet");
    }

    //private void Update()
    //{
    //    if (PosSyncsPerFrame > 0)
    //    {
    //        Debug.Log($"positions were synced {PosSyncsPerFrame} times this frame");
    //        PosSyncsPerFrame = 0;
    //    }
    //}

    public Bullet DataToBullet (BulletData data, Character source, float time)
    {
        if (source.inventory.ActiveItem == null || source.inventory.ActiveItem is not Gun gun)
        {
            Debug.Log($"source {source.id} is not holding a gun");
            return null;
        }    

        var gunModel = gun.GunModel;

        var startpos = DataManager.ConvertDataToVector(data.Startpos);

        var endPos = DataManager.ConvertDataToVector(data.Endpos);

        var velocity = (endPos - startpos).normalized * gunModel.bulletSpeed;

        var delta = NetTime - time;

        //var progress = bullet.Progress + (delta * velocity.magnitude / gunModel.range);

        var bullet = ProjectileManager.Manager.NewBullet(startpos, velocity, gunModel.range, gunModel.DamagePerBullet, source);

        ProjectileManager.Manager.UpdateBullet(bullet,delta, out _);

        return bullet;
    }

    public void SyncDirection (Vector2 direction)
    {
        if (IsServer)
            return;

        float angle = Quaternion.LookRotation(Vector3.forward, direction).eulerAngles.z;

        var input = new PlayerInput { Angle = angle };

        var message = new MessageForServer { Input = input };

        client.SendData(message.ToByteArray());
    }

//    public static void XPlatformLog(string log)
//    {
//#if UNITY_EDITOR// && false
//        Debug.Log(log);
//#else
//        System.Console.WriteLine(log);
//#endif
//    }

//    public static void XPlatformLogError (string error)
//    {
//#if UNITY_EDITOR// && false
//        Debug.LogError(error);
//#else
//        System.Console.WriteLine(error);
//#endif
//    }
}

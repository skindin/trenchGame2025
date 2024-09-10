//using System.Collections;
//using System.Collections.Generic;
//using System.IO;
//using System;
using Google.Protobuf;
using UnityEngine;
using UnityEngine.TextCore.Text;
//using System;

public class NetworkManager : ManagerBase<NetworkManager>
{
    //string id;

    public GameClient client;
    public GameServer server;

    float time;
    public float NetTime
    //{
    //    get { return time; }

    //    set
    //    {
    //        //var floored = Mathf.Floor(value);

    //        //if (floored > Mathf.Floor(time))
    //        //{
    //        //    XPlatformLog($"T = {floored}");
    //        //}

    //        time = value;
    //    }
    //}
    ;

    public void SetPos(Vector2 pos, int id)
    {
        var posData = DataManager.VectorToData(pos);

#if (!UNITY_SERVER || UNITY_EDITOR)// && false

        var input = new PlayerInput {Pos = posData};

        var baseMessage = new MessageForServer {Input = input};

        //var binary = DataManager.MessageToBinary(baseMessage);

        client.SendData(baseMessage.ToByteArray());

        //Debug.Log($"sent pos {pos} to server");
#else

        var charData = new CharacterData { Pos = posData , CharacterID = id};

        server.updateList.List.Add(charData);

        //server.SendDataDisclude(message.ToByteArray());

        //PosSyncsPerFrame++;

        //Console.WriteLine($"server character {id} moved to {pos}");
#endif
    }

    public void SetName(string newName) //this hasn't been changed to combine with movement data but it probably should eventually
    {

#if !UNITY_SERVER || UNITY_EDITOR
        var baseMessage = new MessageForServer() { Input = new PlayerInput { Name = newName } };

        client.SendData(baseMessage.ToByteArray());

        Debug.Log($"sent characterName {newName} to server");

        //CharacterManager.Manager.localPlayerCharacter.name = newName;
#endif

    }

    public void PickupItem(Item item, Vector2 dropPos)
    {
#if (!UNITY_SERVER || UNITY_EDITOR)// && false
        var posData = DataManager.VectorToData(dropPos);

        var input = new PlayerInput { PickupItem = item.id , LookPos = posData};

        var message = new MessageForServer {Input = input};

        client.SendData(message.ToByteArray());

        Debug.Log($"told server it picked up item {item.id}");
#endif
    }

    public void DropItem (Vector2 pos)
    {
#if (!UNITY_SERVER || UNITY_EDITOR)// && false
        var posData = DataManager.VectorToData (pos);

        var input = new PlayerInput { DropItem = true , LookPos = posData };

        var message = new MessageForServer { Input = input };

        client.SendData(message.ToByteArray());

        Debug.Log($"told server it dropped current item");
#endif
    }



    public void SpawnBullet(Bullet bullet)
    {
#if (!UNITY_SERVER || UNITY_EDITOR)// && false

        var bunch = new BulletBunch { StartTime = NetTime};

        var startPosData = DataManager.VectorToData(bullet.startPos);
        var endPosData = DataManager.VectorToData(bullet.startPos + bullet.velocity.normalized * bullet.range);

        bunch.Bullets.Add(new BulletData { Startpos = startPosData, Endpos = endPosData });

        var input = new PlayerInput { Bullets = bunch };

        var message = new MessageForServer { Input = input, Time = NetTime };

        client.SendData(message.ToByteArray());

        //XPlatformLog($"told server to spawn a bullet");
#endif
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
            XPlatformLog($"source {source.id} is not holding a gun");
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
#if UNITY_EDITOR || !UNITY_SERVER// && false
        float angle = Quaternion.LookRotation(Vector3.forward, direction).eulerAngles.z;

        var input = new PlayerInput { Angle = angle };

        var message = new MessageForServer { Input = input };

        client.SendData(message.ToByteArray());
#endif
    }

    public static void XPlatformLog(string log)
    {
#if UNITY_EDITOR// && false
        Debug.Log(log);
#else
        System.Console.WriteLine(log);
#endif
    }
}

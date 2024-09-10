//using System.Collections;
//using System.Collections.Generic;
//using System.IO;
//using System;
using UnityEngine;
using Google.Protobuf;
//using System;

public class NetworkManager : ManagerBase<NetworkManager>
{
    //string id;

    public GameClient client;
    public GameServer server;

    public float time = 0; //might be good to add unscaled time later...
    //int PosSyncsPerFrame = 0;

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

    public void SyncBullet (Bullet bullet)
    {
#if (!UNITY_SERVER || UNITY_EDITOR) //for now, just assuming the client's framerate is sufficient
        //BulletBunch bunch = new();

        //var startPosData = DataManager.VectorToData(bullet.startPos);
        //var endPosData = DataManager.VectorToData(bullet.startPos + bullet.velocity.normalized * bullet.range);

        //bunch.Bullets.Add(new BulletData { Startpos = startPosData, EndPos = endPosData });


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

    public void SetName (string newName) //this hasn't been changed to combine with movement data but it probably should eventually
    {

#if !UNITY_SERVER || UNITY_EDITOR
        var baseMessage = new MessageForServer() { Input = new PlayerInput{Name = newName }};

        client.SendData(baseMessage.ToByteArray());

        Debug.Log($"sent characterName {newName} to server");

        //CharacterManager.Manager.localPlayerCharacter.name = newName;
#endif

    }
}

//using System.Collections;
//using System.Collections.Generic;
//using System.IO;
//using System;
using UnityEngine;
using Google.Protobuf;
using System;
//using static UnityEditor.PlayerSettings;

public class NetworkManager : ManagerBase<NetworkManager>
{
    //string id;

    public GameClient client;
    public GameServer server;

    //int PosSyncsPerFrame = 0;

    public void SetPos(Vector2 pos, int id)
    {
        var posData = DataManager.VectorToData(pos);

#if (!UNITY_SERVER || UNITY_EDITOR)// && false

        var input = new PlayerInput {Pos = posData};

        var baseMessage = new BaseMessage {Input = input};

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

    //private void Update()
    //{
    //    if (PosSyncsPerFrame > 0)
    //    {
    //        Debug.Log($"positions were synced {PosSyncsPerFrame} times this frame");
    //        PosSyncsPerFrame = 0;
    //    }
    //}

    public void SetName (string newName)
    {

#if !UNITY_SERVER || UNITY_EDITOR
        var baseMessage = new BaseMessage() { Input = new PlayerInput{Name = newName }};

        client.SendData(baseMessage.ToByteArray());

        Debug.Log($"sent characterName {newName} to server");

        //CharacterManager.Manager.localPlayerCharacter.name = newName;
#endif

    }
}

//using System.Collections;
//using System.Collections.Generic;
//using System.IO;
//using System;
using UnityEngine;
using Google.Protobuf;
//using static UnityEditor.PlayerSettings;

public class NetworkManager : ManagerBase<NetworkManager>
{
    //string id;

    public GameClient client;
    public GameServer server;

    public void SetPos(Vector2 pos, int id)
    {
        var posData = DataManager.VectorToData(pos);

#if !UNITY_SERVER || UNITY_EDITOR //&& false

        var input = new PlayerInput {Pos = posData};

        var baseMessage = new BaseMessage {Input = input};

        //var binary = DataManager.MessageToBinary(baseMessage);

        client.SendData(baseMessage.ToByteArray());

        //Debug.Log($"sent pos {pos} to server");
#else

        var charData = new CharacterData { Pos = posData , CharacterID = id};

        var message = new BaseMessage { UpdateCharData = charData };

        server.Broadcast(message.ToByteArray());
#endif
    }

    public void SetName (string newName)
    {

#if !UNITY_SERVER || UNITY_EDITOR
        var baseMessage = new BaseMessage() { Name = newName };

        client.SendData(baseMessage.ToByteArray());

        Debug.Log($"sent characterName {newName} to server");

        //CharacterManager.Manager.localPlayerCharacter.name = newName;
#endif

    }
}

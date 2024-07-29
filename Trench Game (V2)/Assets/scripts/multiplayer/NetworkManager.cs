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

    public void SetPos(Vector2 pos)
    {
#if !UNITY_SERVER || UNITY_EDITOR

        SetPosClientToServer(pos);

        void SetPosClientToServer(Vector2 pos) //sets local pos, then sends data to clients
        {
            var posData = DataManager.VectorToData(pos);

            var baseMessage = new BaseMessage() { Pos = posData};

            //var binary = DataManager.MessageToBinary(baseMessage);

            client.SendData(baseMessage.ToByteArray());

            Debug.Log($"sent pos {pos} to server");

        }

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

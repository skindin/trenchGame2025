using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using Google.Protobuf;

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
            var posBinary = DataManager.VectorToBinary(pos);

            client.SendDataToServer(posBinary);
        }
#endif
    }
}

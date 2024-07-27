using Google.Protobuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
//using UnityEngine.Android;
using WebSocketSharp;
using WebSocketSharp.Server;

public class GameServer : MonoBehaviour
{
    private WebSocketServer wssv;
    public Dictionary<string, ClientBehavior> clients = new();
    public Dictionary<string, Character> playerCharacters = new();

    public Queue<Action> actionQueue = new();

    private void Awake()
    {
#if UNITY_SERVER && !UNITY_EDITOR// || true
        //|| true
        ClientBehavior.server = this;

        try
        {
            // Initialize the WebSocket server
            wssv = new WebSocketServer("ws://localhost:8080");
            wssv.AddWebSocketService<ClientBehavior>("/ClientBehavior");
            wssv.Start();

            if (wssv.IsListening)
            {
                Console.WriteLine("WebSocket Server listening on port " + wssv.Port + ", and providing WebSocket services:");
                foreach (var path in wssv.WebSocketServices.Paths)
                {
                    Console.WriteLine("- " + path);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error starting WebSocket server: " + ex.Message);
        }
#else
        Debug.Log("This is either not a server or is the Unity editor...");
#endif
    }

    private void Update()
    {
        while (actionQueue.Count > 0)
        {
            Action action;
            lock (actionQueue)
            {
                action = actionQueue.Dequeue();
            }
            action?.Invoke();
        }
    }

    void BroadCast (string message)
    {
        //foreach (var session in wssv.WebSocketServices["/Echo"].Sessions) ;

        var sessions = wssv.WebSocketServices["/ClientBehavior"].Sessions;

        sessions.Broadcast(message);

        //for (var i = 0; i < sessions.Count; i++)
        //{

        //}
    }

    public void SendDataDisclude(byte[] binary, params string[] discludeIDs)
    {
        //Console.WriteLine($"began send data disclude. clients: {clients} discludedIds: {discludeIDs}");
        foreach (var clientBehavior in clients)
        {
            if (!discludeIDs.Contains(clientBehavior.Key))
            {
                clientBehavior.Value.SendData(binary);
            }
        }
    }

    public void SendDataToClient(byte[] binary, string id)
    {
        if (clients.TryGetValue(id, out var client))
        {
            client.SendData(binary);
        }
    }

//    private void Update()
//    {
//#if UNITY_SERVER && !UNITY_EDITOR 
////||true
//        BroadCast("server spamming client");
//#endif
//    }



    private void OnDestroy()
    {
        if (wssv != null)
        {
            wssv.Stop();
            wssv = null;
        }
    }
}

public class ClientBehavior : WebSocketBehavior
{
    public static GameServer server;

    //public ClientBehavior (GameServer server)
    //{
    //    ClientBehavior.server = server;
    //}

    protected override void OnOpen()
    {
        //Console.WriteLine("New clientBehavior connected.");
        server.clients.Add(ID, this);
    }

    protected override void OnMessage(MessageEventArgs e)
    {


        // Use Coroutine on the main thread
        //Console.WriteLine("onmessage started on server");
        server.actionQueue.Enqueue(() => UseData(e.RawData));
        //Console.WriteLine("started coroutine on server");

        void UseData(byte[] rawData)
        {
            if (e.IsText)
            {
                Console.WriteLine("Server received message: " + e.Data);
                //return;
            }
            else if (DataManager.IfGet<BaseMessage>(rawData, out var baseMessage))
            {
                switch (baseMessage.TypeCase)
                {
                    case BaseMessage.TypeOneofCase.NewPlayerRequest:
                        {
                            if (!server.playerCharacters.ContainsKey(ID))
                            {

                                var id = CharacterManager.Manager.NewId;
                                var pos = ChunkManager.Manager.GetRandomPos();

                                var character = CharacterManager.Manager.NewRemoteCharacter(pos, id);

                                Console.WriteLine($"spawned remote character {id} at {pos}");

                                server.playerCharacters.Add(ID, character);

                                var posData = DataManager.VectorToData(pos);

                                var charData = new CharacterData() { Pos = posData, CharacterID = id };

                                //var permission = new SpawnLocalPlayerPermission() { CharacterData = charData };

                                var grantMessage = new BaseMessage() { NewPlayerGrant = charData };

                                //var grantData = DataManager.MessageToBinary(grantMessage);

                                SendData(grantMessage.ToByteArray());

                                var remoteChar = new BaseMessage() { NewRemoteChar = charData };

                                //var remoteCharData = DataManager.MessageToBinary(remoteChar);

                                server.SendDataDisclude(remoteChar.ToByteArray(), ID);
                            }
                        }
                        break;

                    case BaseMessage.TypeOneofCase.Pos:
                        {
                            var posData = baseMessage.Pos;

                            if (server.playerCharacters.TryGetValue(ID, out var character))
                            {
                                //var id = characterData.CharacterID;

                                var pos = DataManager.ConvertDataToVector(posData);

                                character.SetPos(pos, false);
                                Console.WriteLine($"updated pos of character {character.id} to {pos}");

                                var characterData = new CharacterData() { Pos = posData, CharacterID = character.id };

                                var updateMessage = new BaseMessage() { UpdateCharData = characterData };

                                //var updateData = DataManager.MessageToBinary(updateMessage);

                                server.SendDataDisclude(updateMessage.ToByteArray(), ID);
                            }
                            else
                            {
                                Console.WriteLine($"server doesn't have a character associated with client {ID}");
                            }
                        }
                        break;
                }
            }
        }
    }


    protected override void OnClose(CloseEventArgs e)
    {
        Console.WriteLine("Client disconnected. Reason: " + e.Reason);

        server.actionQueue.Enqueue(() => RemoveCharacter(ID));

        void RemoveCharacter (string ID)
        {
            server.clients.Remove(ID);

            if (server.playerCharacters.TryGetValue(ID, out var character))
            {
                server.playerCharacters.Remove(ID);

                CharacterManager.Manager.RemoveCharacter(character);
            }

            var removeMessage = new BaseMessage() { RemoveCharOfID = character.id };

            server.SendDataDisclude(removeMessage.ToByteArray(), ID);

            Console.WriteLine($"Removed character {character.id}");
        }
    }

    protected override void OnError(ErrorEventArgs e)
    {
        Console.WriteLine("penis WebSocket error: " + e.Message);
    }

    public void SendData(byte[] binary)
    {
        Send(binary);
    }
}

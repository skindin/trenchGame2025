using Google.Protobuf;
using Google.Protobuf.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

//using UnityEngine.Android;
using WebSocketSharp;
using WebSocketSharp.Server;
using static UnityEngine.EventSystems.EventTrigger;

public class GameServer : MonoBehaviour
{
    private WebSocketServer wssv;
    public Dictionary<string, ClientBehavior> clients = new();
    public Dictionary<string, Character> playerCharacters = new();

    public Queue<Action> actionQueue = new();

    public bool logBitRate = false;
    public int averageBitRateFrames = 20, averageBitRate = 0;

    public int bytesThisFrame { get; set; }
    List<int> pastByteRecords = new();

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
        catch (TypeInitializationException ex)
        {
            Console.WriteLine("Type Initialization Exception: " + ex.Message);
            if (ex.InnerException != null)
            {
                Console.WriteLine("Inner Exception: " + ex.InnerException.Message);
                Console.WriteLine("Stack Trace: " + ex.InnerException.StackTrace);
            }
            else
            {
                Console.WriteLine("No Inner Exception.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("General Exception: " + ex.Message);
            Console.WriteLine("Stack Trace: " + ex.StackTrace);
        }
#else
        //Debug.Log("This is either not a server or is the Unity editor...");
#endif
    }

    public CharDataList newPlayerList = new(), updateList = new(), removeList = new(), currentCharData = new();

    private void LateUpdate()
    {
        //bool sentSomeData = actionQueue.Count > 0;

        while (actionQueue.Count > 0)
        {
            Action action;
            lock (actionQueue)
            {
                action = actionQueue.Dequeue();
            }
            action?.Invoke();
        }



        foreach (var pair in clients)
        {
            var client = pair.Value;

            if (client.newPlayer != null)
            {
                newPlayerList.List.Add(client.newPlayer);

                var pos = ChunkManager.Manager.GetRandomPos();
                var id = CharacterManager.Manager.NewId;

                var newCharacter = SpawnManager.Manager.SpawnRemoteCharacter(pos, id);

                playerCharacters.Add(client.ID, newCharacter);
                currentCharData.List.Add(client.newPlayer);

                var posData = DataManager.VectorToData(pos);

                client.newPlayer.Pos = posData;
                client.newPlayer.CharacterID = id;
            }
            else if (client.update != null)
            {
                updateList.List.Add(client.update);

                var character = playerCharacters[client.ID];

                var currentData = currentCharData.List.FirstOrDefault(charData => charData.CharacterID == client.update.CharacterID);

                if (currentData != null)
                    DataManager.CombineCharData(currentData, client.update); //i hope this part works properly

                if (client.update.HasName)
                {
                    character.characterName = client.update.Name;
                }

                if (client.update.Pos != null)
                {
                    var pos = DataManager.ConvertDataToVector(client.update.Pos);
                    character.SetPos(pos,false);
                }

            }
            else if (client.remove != null)
            {
                removeList.List.Add(client.remove);

                var character = playerCharacters[client.ID];

                SpawnManager.Manager.RemoveCharacter(character);
                for (int i = 0; i < currentCharData.List.Count; i++)
                {
                    var currentChar = removeList.List[i];

                    if (currentChar.CharacterID == client.remove.CharacterID)
                    {
                        currentCharData.List.RemoveAt(i);
                        i--;
                    }
                }

                currentCharData.List.Remove(client.remove);
            }
        }

        foreach (var pair in clients)
        {
            var client = pair.Value;

            if (client.newPlayer == null && client.update == null && client.remove == null)
                continue; //doesn't need to reset, because they're all already null

            if (client.newPlayer != null) //if this client just joined, inform them of current characters, then continue to other clients
            {
                var currentChars = (currentCharData.List.Count > 0) ? currentCharData : null;

                var currentChar = currentChars.List.FirstOrDefault(charData => charData.CharacterID == client.newPlayer.CharacterID);

                if (currentChar != null) //sloppy af ik
                {
                    currentChars.List.Remove(currentChar);
                }

                var grant = new NewPlayerGrant { NewPlayer = client.newPlayer , CurrentChars = currentChars};
                var message = new BaseMessage { NewPlayerGrant = grant };
                client.SendData(message.ToByteArray());

                currentChars.List.Add(currentChar);

                LogLists();
                client.update = client.remove = client.newPlayer = null;
                continue;
            }

            {
                var gameState = new GameState();
                var message = new BaseMessage { GameState = gameState };

                if (client.update != null)
                {
                    updateList.List.Remove(client.update);
                }

                gameState.UpdateChars = updateList;

                gameState.NewRemoteChars = newPlayerList;

                gameState.RemoveChars = removeList;

                client.SendData(message.ToByteArray());

                if (client.update != null)
                {
                    updateList.List.Add(client.update);
                }
            }

            LogLists();

            void LogLists ()
            {
                //Console.WriteLine($"{newPlayerList.List.Count} new players, {updateList.List.Count} character updates, and {removeList.List.Count} removals");
            }
        }

        //if (logBitRate)
        //{
        //    pastByteRecords.Add(bytesThisFrame);

        //    if (pastByteRecords.Count > averageBitRateFrames)
        //        pastByteRecords.RemoveAt(0);

        //    averageBitRate = Mathf.RoundToInt(LogicAndMath.GetListValueTotal(pastByteRecords.ToArray(), byteCount => byteCount) / pastByteRecords.Count / Time.deltaTime);

        //    if (bytesThisFrame > 0)
        //        Console.WriteLine($"average bit rate: {averageBitRate}");

        //    bytesThisFrame = 0;
        //}

        newPlayerList.List.Clear();
        updateList.List.Clear();
        removeList.List.Clear();
    }

    public void Broadcast (byte[] message)
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

    public CharacterData remove, update, newPlayer;

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
            else if (DataManager.IfGet<BaseMessage>(rawData, out var message))
            {
                switch (message.TypeCase)
                {
                    case BaseMessage.TypeOneofCase.NewPlayerRequest:
                        {
                            if (!server.playerCharacters.ContainsKey(ID) && newPlayer == null)
                            {
                                //var pos = ChunkManager.Manager.GetRandomPos();

                                var name = message.NewPlayerRequest;

                                newPlayer = new CharacterData { CharacterID = CharacterManager.Manager.NewId, Name = name };

                                Console.WriteLine($"recieved new player request");

                                //foreach (var otherCharacter in CharacterManager.Manager.active)
                                //{
                                //    var otherCharPosData = DataManager.VectorToData(otherCharacter.transform.position);

                                //    var spawnData = new CharacterData() {
                                //        Pos = otherCharPosData, 
                                //        CharacterID = otherCharacter.id , 
                                //        Name = otherCharacter.characterName
                                //    };

                                //    var newRemote = new BaseMessage() { NewRemoteChar = spawnData };

                                //    SendData(newRemote.ToByteArray());

                                //    Console.WriteLine(
                                //        $"told new client to spawn character {otherCharacter.id} named {otherCharacter.characterName} at {(Vector2)otherCharacter.transform.position}");
                                //}

                                //var id = CharacterManager.Manager.NewId;
                                //var pos = ChunkManager.Manager.GetRandomPos();
                                //var name = baseMessage.NewPlayerRequest;

                                //var character = SpawnManager.Manager.SpawnRemoteCharacter(pos, id);
                                //character.characterName = name;

                                //Console.WriteLine($"spawned remote character {id} named {name} at {pos}");

                                //server.playerCharacters.Add(ID, character);

                                //var posData = DataManager.VectorToData(pos);

                                //var charData = new CharacterData() { Pos = posData, CharacterID = id , Name = character.characterName};

                                ////var permission = new SpawnLocalPlayerPermission() { CharacterData = charData };

                                //var grantMessage = new BaseMessage() { NewPlayerGrant = charData };

                                ////var grantData = DataManager.MessageToBinary(grantMessage);

                                //SendData(grantMessage.ToByteArray());

                                //var remoteChar = new BaseMessage() { NewRemoteChar = charData };

                                ////var remoteCharData = DataManager.MessageToBinary(remoteChar);

                                //server.SendDataDisclude(remoteChar.ToByteArray(), ID);
                            }
                        }
                        break;

                    case BaseMessage.TypeOneofCase.Input:
                        {
                            if (server.playerCharacters.TryGetValue(ID, out var character))
                            {
                                var charData = new CharacterData { };

                                if (message.Input.Pos != null)
                                {
                                    charData.Pos = message.Input.Pos;
                                }

                                if (message.Input.HasName)
                                {
                                    charData.Name = message.Input.Name;
                                }

                                //Console.WriteLine($"server recieved input data")

                                //var posData = message.Input.Pos;


                                //var id = characterData.CharacterID;

                                //var pos = DataManager.ConvertDataToVector(posData);

                                //character.SetPos(pos, false);
                                //Console.WriteLine($"updated pos of character {character.id} to {pos}");

                                //var characterData = new CharacterData() { Pos = posData, CharacterID = character.id };

                                if (update != null)
                                {
                                    DataManager.CombineCharData(update, charData);
                                }
                                else
                                    update = charData;

                                //var updateMessage = new BaseMessage() { UpdateCharData = characterData };

                                ////var updateData = DataManager.MessageToBinary(updateMessage);

                                //server.SendDataDisclude(updateMessage.ToByteArray(), ID);

                                Console.WriteLine($"server recieved input");
                            }
                            else
                            {
                                Console.WriteLine($"server doesn't have a charData associated with client {ID}");
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
            if (server.playerCharacters.TryGetValue(ID, out var character))
            {
                remove = new CharacterData { CharacterID = character.id };

                newPlayer = update = null;
            }

            //server.clients.Remove(ID);

            //if (server.playerCharacters.TryGetValue(ID, out var character))
            //{
            //    server.playerCharacters.Remove(ID);

            //    CharacterManager.Manager.RemoveCharacter(character);
            //}

            //var removeMessage = new BaseMessage() { RemoveCharOfID = character.id };

            //server.SendDataDisclude(removeMessage.ToByteArray(), ID);

            //Console.WriteLine($"Removed character {character.id}");
        }
    }

    protected override void OnError(ErrorEventArgs e)
    {
        Console.WriteLine("WebSocket error: " + e.Message);
    }

    public void SendData(byte[] binary)
    {
        Send(binary);

        server.bytesThisFrame += binary.Length;

        if (server.logBitRate)
            Console.WriteLine($"sent {binary.Length} bytes to client");
    }
}

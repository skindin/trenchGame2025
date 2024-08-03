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
    public Queue<string> disconnected = new();

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

                Console.WriteLine($"spawned remote character {id} named {client.newPlayer.Name}");

                playerCharacters.Add(client.ID, newCharacter);
                client.character = newCharacter;
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

                //Console.WriteLine($"updated character {character.id}");
                LogLists();
            }
            else if (client.remove != null)
            {
                removeList.List.Add(client.remove);

                var character = playerCharacters[client.ID];

                SpawnManager.Manager.RemoveCharacter(character);
                foreach (var currentChar in currentCharData.List)
                {
                    //var currentChar = removeList.List[i];

                    if (currentChar.CharacterID == client.remove.CharacterID)
                    {
                        currentCharData.List.Remove(currentChar);
                        break;
                    }
                }

                Console.WriteLine($"removed character {character.id}");
            }
        }

        foreach (var pair in clients)
        {
            var client = pair.Value;

            if (client.State != WebSocketState.Open)
                continue;

            //if (client.newPlayer == null && client.update == null && client.remove == null) //bruh im such an idiot
            //    continue; //doesn't need to reset, because they're all already null

            if (client.newPlayer != null) //if this client just joined, inform them of current characters, then continue to other clients
            {
                var currentChars = (currentCharData.List.Count > 0) ? currentCharData : null;

                var currentChar = currentChars.List.FirstOrDefault(charData => charData.CharacterID == client.newPlayer.CharacterID);

                if (true || client.character != null) //sloppy af ik, not sure why i was testing if the client already had a character, it should by this point
                {
                    currentChars.List.Remove(currentChar);
                }

                //client.newPlayer.Name = null;

                var grant = new NewPlayerGrant { NewPlayer = client.newPlayer , CurrentChars = currentChars};
                var message = new BaseMessage { NewPlayerGrant = grant };
                client.SendData(message.ToByteArray());

                currentChars.List.Add(currentChar);

                Console.WriteLine($"told client to spawn their player, character {client.newPlayer.CharacterID}");

                LogLists();
                client.update = client.remove = client.newPlayer = null;
                continue;
            }

            {
                var gameState = new GameState();


                if (client.update != null)
                {
                    updateList.List.Remove(client.update);
                }

                gameState.UpdateChars = updateList.List.Count > 0 ? updateList : null;

                gameState.NewRemoteChars = newPlayerList.List.Count > 0 ? newPlayerList : null;

                gameState.RemoveChars = removeList.List.Count > 0 ? removeList : null;

                if (gameState.UpdateChars != null || gameState.NewRemoteChars != null || gameState.RemoveChars != null)
                {
                    var message = new BaseMessage { GameState = gameState };

                    client.SendData(message.ToByteArray());
                    LogLists();
                }
                //else
                //    Console.WriteLine("apparently all the lists are empty shruggin emoji");

                if (client.update != null) //readd this character back
                {
                    updateList.List.Add(client.update);
                }

                client.update = client.remove = client.newPlayer = null;
            }
            //LogLists();
        }

        newPlayerList.List.Clear();
        updateList.List.Clear();
        removeList.List.Clear();

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

        while (disconnected.Count > 0)
        {
            clients.Remove(disconnected.Dequeue());
        }

        //LogLists();

        void LogLists()
        {
            //Console.WriteLine($"{newPlayerList.List.Count} new players, {updateList.List.Count} character updates, {removeList.List.Count} removals, and {currentCharData.List.Count} current characters");
        }
    }

    public void AddCharacter (Character character)
    {
        var pos = DataManager.VectorToData(character.transform.position);
        var data = new CharacterData{CharacterID = character.id, Pos = pos, Name = character.characterName};
        currentCharData.List.Add(data);
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

    public Character character;

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
                            if (!character && newPlayer == null)
                            {
                                //var pos = ChunkManager.Manager.GetRandomPos();

                                var name = message.NewPlayerRequest;

                                newPlayer = new CharacterData {Name = name};

                                Console.WriteLine($"recieved new player request");
                            }
                        }
                        break;

                    case BaseMessage.TypeOneofCase.Input:
                        {
                            if (server.playerCharacters.TryGetValue(ID, out var character))
                            {
                                var charData = new CharacterData {CharacterID = this.character.id};

                                if (message.Input.Pos != null)
                                {
                                    charData.Pos = message.Input.Pos;
                                }

                                if (message.Input.HasName)
                                {
                                    charData.Name = message.Input.Name;
                                }

                                if (update != null)
                                {
                                    DataManager.CombineCharData(update, charData);
                                }
                                else
                                    update = charData;

                                //Console.WriteLine($"server recieved input");
                            }
                            else
                            {
                                Console.WriteLine($"server doesn't have a currentChar associated with client {ID}");
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
            if (character)
            {
                remove = new CharacterData { CharacterID = character.id };

                newPlayer = update = null;

                server.disconnected.Enqueue(ID);
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

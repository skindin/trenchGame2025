using Google.Protobuf;
using Google.Protobuf.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using UnityEngine.Assertions.Must;
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
    public int averageBitRateFrames = 20, averageBitRate = 0, targetFramerate = 200;

    public int bytesThisFrame { get; set; }
    List<int> pastByteRecords = new();

    long startTick = 0;

    private void Awake()
    {
#if !UNITY_SERVER || UNITY_EDITOR// || true
        return;
#endif
        startTick = DateTime.UtcNow.Ticks;

        //|| true
        ClientBehavior.server = this;

        try
        {
            // Initialize the WebSocket server
            wssv = new WebSocketServer("ws://0.0.0.0:8080");
            // or for dual-stack (both IPv4 and IPv6)
            // wssv = new WebSocketServer("ws://[::]:8080");

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

                Application.targetFrameRate = targetFramerate;
                Console.WriteLine($"Target framerate set to {targetFramerate}");
    }

    public CharDataList newPlayerList = new(), updateList = new(), currentCharData = new();
    public RepeatedField<int> removeCharList = new(), removeItemList = new();

    public RepeatedField<ItemData> newItems = new(), updateItems = new(), currentItems = new();

    public RepeatedField<BulletBunch> newBullets = new();

    private void LateUpdate()
    {
#if !UNITY_SERVER || UNITY_EDITOR// || true
        return;
#endif

        NetworkManager.Manager.NetTime = Time.time;

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
                var id = SpawnManager.Manager.NewCharId;

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

                foreach (var droppedItem in client.droppedItems)
                {
                    updateItems.Add(droppedItem);
                    var item = ItemManager.Manager.active[droppedItem.ItemId];
                    var pos = DataManager.ConvertDataToVector(droppedItem.Pos);
                    client.character.inventory.DropItem(item, pos);

                    foreach (var currentItem in currentItems)
                    {
                        if (currentItem.ItemId == droppedItem.ItemId)
                        {
                            currentItem.Pos = droppedItem.Pos;
                        }
                    }
                }

                if (client.droppedItems.Count > 0)
                    currentData.ClearItemId();

                if (client.update.HasItemId)
                {
                    var item = ItemManager.Manager.active[client.update.ItemId];
                    character.inventory.PickupItem(item, item.transform.position);

                    foreach (var itemData in currentItems)
                    {
                        if (itemData.ItemId == item.id)
                        {
                            itemData.Pos = null;
                        }
                    }

                    currentData.ItemId = client.update.ItemId;
                    //hopefully adding the previous item to the dropped list makes the drop pos work when switching items...
                }

                if (character.inventory.ActiveWeapon != null
                    && character.inventory.ActiveWeapon is Gun gun
                    && client.bullets != null && client.bullets.Bullets.Count > 0)
                {
                    var gunModel = gun.GunModel;

                    foreach (var bullet in client.bullets.Bullets)
                    {
                        NetworkManager.Manager.DataToBullet(bullet, character, client.bullets.StartTime);
                    }

                    //NetworkManager.XPlatformLog($"spawned {client.bullets.Bullets.Count} bullet(s)");

                    newBullets.Add(client.bullets);
                }

                //client.droppedItems.Clear();

                //if (!character.inventory.ActiveItem)
                //{
                //    currentData.ClearItemId();
                //}
                //else
                //{
                //    currentData.ItemId = character.inventory.ActiveItem.id;

                //}

                //Console.WriteLine($"updated character {character.id}");
                LogLists();
            }
            else if (client.remove != null)
            {

                var character = playerCharacters[client.ID];

                removeCharList.Add(character.id);


                SpawnManager.Manager.RemoveCharacter(character);
                foreach (var currentChar in currentCharData.List)
                {
                    //var currentChar = removeList.List[i];

                    if (currentChar.CharacterID == client.remove.CharacterID)
                    {
                        currentCharData.List.Remove(currentChar);

                        //character.inventory.DropActiveItem();

                        ItemData activeItem = null;

                        foreach (var itemData in currentItems)
                        {
                            if (itemData.ItemId == currentChar.ItemId)
                            {
                                activeItem = itemData;
                                break;
                            }
                        }
                        
                        if (activeItem == null)
                        {
                            activeItem = new ItemData { ItemId = currentChar.ItemId};
                        }

                        activeItem.Pos = currentChar.Pos;

                        //currentChar.ClearItemId();
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

                //var startTime = LogicAndMath.SecondsToTicks(Time.time);

                var grant = new NewPlayerGrant { NewPlayer = client.newPlayer , CurrentChars = currentChars, StartTime = startTick};

                foreach (var item in currentItems) //might be better to make this it's own message, this readonly thing getting annoying
                {
                    grant.CurrentItems.Add(item);
                }

                var message = new MessageForClient { NewPlayerGrant = grant, Time = Time.deltaTime };
                client.SendData(message.ToByteArray());

                currentChars.List.Add(currentChar);

                Console.WriteLine($"told client to spawn their player, character {client.newPlayer.CharacterID}");

                LogLists();
                client.update = client.remove = client.newPlayer = null;
                continue;
            }
            //if this is an established player...
            {
                var gameState = new GameState();


                if (client.update != null)
                {
                    updateList.List.Remove(client.update);



                    //if (client.bull)
                }

                if (client.bullets != null)
                {
                    newBullets.Remove(client.bullets);
                }

                foreach (var dropItem in client.droppedItems)
                {
                    bool found = false;
                    for (int i = 0; i < updateItems.Count; i++)
                    {
                        var updateItem = updateItems[i];

                        if (dropItem.ItemId == updateItem.ItemId)
                        {
                            updateItems.RemoveAt(i);
                            i--;
                            found = true;
                            break;
                        }
                    }

                    if (found)
                        break;
                }

                gameState.UpdateChars = updateList.List.Count > 0 ? updateList : null;

                gameState.NewRemoteChars = newPlayerList.List.Count > 0 ? newPlayerList : null;

                foreach (var removeChar in removeCharList)
                {
                    gameState.RemoveChars.Add(removeChar);
                }

                foreach (var newItem in newItems)
                {
                    gameState.NewItems.Add(newItem);
                }

                foreach (var updateItem in updateItems)
                {
                    gameState.UpdateItems.Add(updateItem);
                }

                foreach (var bunch in newBullets)
                {
                    gameState.NewBullets.Add(bunch);
                }

                //if (newBullets.Count > 0)
                //{
                //    NetworkManager.XPlatformLog($"told client {client.character.id} to spawn {newBullets.Count} bullet(s)");
                //}

                if ( true || 
                    gameState.UpdateChars != null ||
                    gameState.NewRemoteChars != null || 
                    gameState.RemoveChars != null ||
                    gameState.NewItems.Count > 0
                    )
                {
                    var message = new MessageForClient { GameState = gameState , Time = Time.deltaTime};

                    client.SendData(message.ToByteArray());
                    LogLists();
                }
                //else
                //    Console.WriteLine("apparently all the lists are empty shruggin emoji");

                if (client.update != null) //readd this character back
                {
                    updateList.List.Add(client.update);
                }


                foreach (var dropItem in client.droppedItems)
                {
                    updateItems.Add(dropItem);
                }

                if (client.bullets != null)
                    newBullets.Add(client.bullets);

                client.droppedItems.Clear();

                client.bullets = null;

                client.update = client.remove = client.newPlayer = null;
            }
            //LogLists();
        }

        //var message = 

        newPlayerList.List.Clear();
        updateList.List.Clear();
        removeCharList.Clear();

        newItems.Clear();
        updateItems.Clear();
        removeItemList.Clear();

        newBullets.Clear(); //bruh facepalm emoji
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

    public void AddItem(Item item)
    {
        var pos = DataManager.VectorToData(item.transform.position);
        var data = new ItemData { ItemId = item.id, PrefabId = item.prefabId, Pos = pos };
        newItems.Add(data);
        currentItems.Add(data);
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

    public List<ItemData> droppedItems = new(); //GOTTA MAKE THIS LIST THE ITEMS IT'S DROPPING

    public Character character;

    public BulletBunch bullets = new();

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
            else if (DataManager.IfGet<MessageForServer>(rawData, out var message))
            {
                switch (message.TypeCase)
                {
                    case MessageForServer.TypeOneofCase.NewPlayerRequest:
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

                    case MessageForServer.TypeOneofCase.Input:
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
                                    //Console.WriteLine($"recieved new name {message.Input.Name}");
                                }

                                if (message.Input.HasAngle)
                                {
                                    charData.Angle = message.Input.Angle;
                                    //Console.WriteLine($"recieved angle {charData.Angle} for character {character.id}");
                                }

                                if (update != null)
                                {
                                    DataManager.CombineCharData(update, charData);
                                }
                                else
                                    update = charData;

                                switch (message.Input.ItemCase)
                                {
                                    //case PlayerInput.ItemOneofCase.Action:
                                    //    break;

                                    //case PlayerInput.ItemOneofCase.SecondaryAction: break;

                                    //case PlayerInput.ItemOneofCase.DirectionalAction: break;

                                    case PlayerInput.ItemOneofCase.DropItem:
                                        droppedItems.Add(new ItemData { ItemId = character.inventory.ActiveItem.id , Pos = message.Input.LookPos});
                                        break;

                                    case PlayerInput.ItemOneofCase.PickupItem:
                                        update.ItemId = message.Input.PickupItem;
                                        if (character.inventory.ActiveItem) //if the character is holding an item,
                                        {
                                            droppedItems.Add(new ItemData { ItemId = character.inventory.ActiveItem.id, Pos = message.Input.LookPos });
                                        }
                                        break;
                                }

                                //Console.WriteLine($"server recieved input");
                            }
                            else
                            {
                                Console.WriteLine($"server doesn't have a currentChar associated with client {ID}");
                            }            
                            
                            if (message.Input.Bullets != null && message.Input.Bullets.Bullets.Count > 0)
                            {
                                bullets = message.Input.Bullets;
                                bullets.CharacterId = character.id;
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
